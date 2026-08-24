using System;
using System.Collections.Generic;
using StatusWindow.Progression;
using UnityEngine;

namespace StatusWindow
{
    public sealed class StatusWindowGameState
    {
        private readonly PrototypeCatalog catalog;
        private readonly int[] stats = new int[Enum.GetValues(typeof(StatType)).Length];
        private readonly HashSet<SkillNodeDefinition> unlockedSkills = new HashSet<SkillNodeDefinition>();
        private readonly HashSet<SkillNodeDefinition> equippedSkills = new HashSet<SkillNodeDefinition>();
        private readonly HashSet<EquipmentDefinition> purchasedEquipment = new HashSet<EquipmentDefinition>();
        private readonly Dictionary<EquipmentSlot, EquipmentDefinition> equippedEquipment = new Dictionary<EquipmentSlot, EquipmentDefinition>();
        private readonly Dictionary<EquipmentDefinition, int> equipmentUpgradeLevels = new Dictionary<EquipmentDefinition, int>();
        private readonly Dictionary<DungeonDefinition, int> dungeonMasteryRanks = new Dictionary<DungeonDefinition, int>();
        private readonly Dictionary<DungeonDefinition, DungeonRecordData> dungeonRecords = new Dictionary<DungeonDefinition, DungeonRecordData>();
        private readonly Dictionary<LegacyUpgradeDefinition, int> legacyUpgradeRanks = new Dictionary<LegacyUpgradeDefinition, int>();
        private readonly HashSet<MilestoneDefinition> claimedMilestones = new HashSet<MilestoneDefinition>();
        private readonly BuildPresetData[] buildPresets = new BuildPresetData[3];
        private long lastSavedUtcTicks;
        private long dailyContractResetUtcTicks;
        private int dailyRiftClearCount;
        private int dailyCombatGold;
        private bool dailyRiftClearClaimed;
        private bool dailyCombatGoldClaimed;
        private bool soundEnabled = true;
        private bool vibrationEnabled = true;

        public StatusWindowGameState(PrototypeCatalog prototypeCatalog)
        {
            catalog = prototypeCatalog;
            Gold = catalog.StartingGold;
            UnspentStatPoints = catalog.StartingStatPoints;
        }

        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public int ExperienceToNextLevel => 20 + Level * 10;
        public int Gold { get; private set; }
        public int UnspentStatPoints { get; private set; }
        public int ClearedDungeonCount { get; private set; }
        public int RebirthCount { get; private set; }
        public int LegacyShards { get; private set; }
        public int SpentLegacyShards { get; private set; }
        public int AvailableLegacyShards => Mathf.Max(0, LegacyShards - SpentLegacyShards);
        public int SelectedDungeonIndex { get; private set; }
        public int SelectedProtocolIndex { get; private set; }
        public int SelectedCombatDirectiveIndex { get; private set; }
        public bool AutoRepeatDungeon { get; private set; }
        public bool SoundEnabled => soundEnabled;
        public bool VibrationEnabled => vibrationEnabled;
        public PrototypeCatalog Catalog => catalog;
        public const int DailyRiftClearTarget = 2;
        public const int DailyCombatGoldTarget = 120;
        public int DailyRiftClearCount => dailyRiftClearCount;
        public int DailyCombatGold => dailyCombatGold;

        public int GetStat(StatType statType) => stats[(int)statType];
        public bool HasSkill(SkillNodeDefinition skill) => unlockedSkills.Contains(skill);
        public bool IsSkillEquipped(SkillNodeDefinition skill) => equippedSkills.Contains(skill);
        public bool IsEquipmentSetActive(EquipmentSetDefinition equipmentSet)
        {
            if (equipmentSet == null || equipmentSet.RequiredEquipment == null || equipmentSet.RequiredEquipment.Count == 0) return false;
            foreach (var equipment in equipmentSet.RequiredEquipment)
            {
                if (equipment == null || GetEquipped(equipment.Slot) != equipment) return false;
            }
            return true;
        }

        public int GetEquipmentSetEquippedCount(EquipmentSetDefinition equipmentSet)
        {
            if (equipmentSet == null || equipmentSet.RequiredEquipment == null) return 0;
            var count = 0;
            foreach (var equipment in equipmentSet.RequiredEquipment)
            {
                if (equipment != null && GetEquipped(equipment.Slot) == equipment) count++;
            }
            return count;
        }

        /// <summary>Equips a complete owned set atomically so a player can switch a build before auto battle starts.</summary>
        public bool TryEquipSet(EquipmentSetDefinition equipmentSet)
        {
            if (equipmentSet == null || equipmentSet.RequiredEquipment == null || equipmentSet.RequiredEquipment.Count == 0) return false;
            foreach (var equipment in equipmentSet.RequiredEquipment)
            {
                if (equipment == null || !HasEquipment(equipment)) return false;
            }

            foreach (var equipment in equipmentSet.RequiredEquipment)
            {
                equippedEquipment[equipment.Slot] = equipment;
            }
            return true;
        }
        public int EquippedSkillCount => equippedSkills.Count;
        public int MaximumEquippedSkillCount => catalog.Progression.MaximumEquippedSkillCount;
        public int BuildPresetCount => buildPresets.Length;
        public int TotalDungeonMasteryRank
        {
            get
            {
                var total = 0;
                foreach (var dungeon in catalog.Dungeons) total += GetDungeonMasteryRank(dungeon);
                return total;
            }
        }
        public float TotalDungeonMasteryDamageBonus
        {
            get
            {
                var total = 0f;
                foreach (var dungeon in catalog.Dungeons) total += GetDungeonMasteryRank(dungeon) * dungeon.DamageBonusPerMasteryRank;
                return total;
            }
        }
        public bool HasEquipment(EquipmentDefinition equipment) => purchasedEquipment.Contains(equipment);
        public int GetEquipmentUpgradeLevel(EquipmentDefinition equipment) => equipment != null && equipmentUpgradeLevels.TryGetValue(equipment, out var level) ? level : 0;
        public int GetDungeonMasteryRank(DungeonDefinition dungeon) => dungeon != null && dungeonMasteryRanks.TryGetValue(dungeon, out var rank) ? rank : 0;
        public int GetDungeonTotalClearCount(DungeonDefinition dungeon) => dungeon != null && dungeonRecords.TryGetValue(dungeon, out var record) ? record.totalClears : 0;
        public float GetDungeonBestClearSeconds(DungeonDefinition dungeon) => dungeon != null && dungeonRecords.TryGetValue(dungeon, out var record) ? record.bestClearSeconds : 0f;
        public int GetLegacyUpgradeRank(LegacyUpgradeDefinition upgrade) => upgrade != null && legacyUpgradeRanks.TryGetValue(upgrade, out var rank) ? rank : 0;
        public bool HasClaimedMilestone(MilestoneDefinition milestone) => milestone != null && claimedMilestones.Contains(milestone);
        public EquipmentDefinition GetEquipped(EquipmentSlot slot) => equippedEquipment.TryGetValue(slot, out var equipment) ? equipment : null;
        public DungeonDefinition SelectedDungeon => catalog.Dungeons.Count == 0 ? catalog.Dungeon : catalog.Dungeons[SelectedDungeonIndex];
        public DungeonProtocolDefinition SelectedProtocol => catalog.DungeonProtocols.Count == 0 ? null : catalog.DungeonProtocols[SelectedProtocolIndex];
        public CombatDirectiveDefinition SelectedCombatDirective => catalog.CombatDirectives.Count == 0 ? null : catalog.CombatDirectives[SelectedCombatDirectiveIndex];

        public bool TrySelectDungeon(int index)
        {
            if (index < 0 || index >= catalog.Dungeons.Count) return false;
            if (Level < catalog.Dungeons[index].RequiredLevel) return false;
            SelectedDungeonIndex = index;
            return true;
        }

        public void SetAutoRepeatDungeon(bool enabled)
        {
            AutoRepeatDungeon = enabled;
        }

        public void SetSoundEnabled(bool enabled) => soundEnabled = enabled;
        public void SetVibrationEnabled(bool enabled) => vibrationEnabled = enabled;

        public bool TrySelectProtocol(int index)
        {
            if (index < 0 || index >= catalog.DungeonProtocols.Count) return false;
            SelectedProtocolIndex = index;
            return true;
        }

        public bool TrySelectCombatDirective(int index)
        {
            if (index < 0 || index >= catalog.CombatDirectives.Count) return false;
            SelectedCombatDirectiveIndex = index;
            return true;
        }

        public bool TrySpendStatPoint(StatType statType)
        {
            if (UnspentStatPoints <= 0) return false;
            stats[(int)statType]++;
            UnspentStatPoints--;
            return true;
        }

        public bool TryBuyStatPoint()
        {
            if (Gold < catalog.StatPointGoldCost) return false;
            Gold -= catalog.StatPointGoldCost;
            UnspentStatPoints++;
            return true;
        }

        public bool TryUnlockSkill(SkillNodeDefinition skill)
        {
            if (skill == null || HasSkill(skill) || Gold < skill.GoldCost || (skill.Prerequisite != null && !HasSkill(skill.Prerequisite))) return false;
            Gold -= skill.GoldCost;
            unlockedSkills.Add(skill);
            if (EquippedSkillCount < MaximumEquippedSkillCount) equippedSkills.Add(skill);
            return true;
        }

        public bool TryToggleSkill(SkillNodeDefinition skill)
        {
            if (skill == null || !HasSkill(skill)) return false;
            if (IsSkillEquipped(skill)) return equippedSkills.Remove(skill);
            if (EquippedSkillCount >= MaximumEquippedSkillCount) return false;
            equippedSkills.Add(skill);
            return true;
        }

        public int BuildResetGoldRefund
        {
            get
            {
                var spentGold = 0;
                foreach (var skill in unlockedSkills) spentGold += skill.GoldCost;
                return Mathf.FloorToInt(spentGold * 0.7f);
            }
        }

        public bool TryResetBuild()
        {
            var assignedStatPoints = GetAssignedStatPoints();
            if (assignedStatPoints == 0 && unlockedSkills.Count == 0) return false;

            UnspentStatPoints += assignedStatPoints;
            Gold += BuildResetGoldRefund;
            Array.Clear(stats, 0, stats.Length);
            unlockedSkills.Clear();
            equippedSkills.Clear();
            return true;
        }

        public bool HasBuildPreset(int slot)
        {
            return slot >= 0 && slot < buildPresets.Length && buildPresets[slot] != null;
        }

        public bool SaveBuildPreset(int slot)
        {
            if (slot < 0 || slot >= buildPresets.Length) return false;
            buildPresets[slot] = new BuildPresetData
            {
                stats = (int[])stats.Clone(),
                equippedSkillIds = GetEquippedSkillIds(),
                weaponId = GetEquippedId(EquipmentSlot.Weapon),
                armorId = GetEquippedId(EquipmentSlot.Armor),
                bootsId = GetEquippedId(EquipmentSlot.Boots),
                ringId = GetEquippedId(EquipmentSlot.Ring),
            };
            return true;
        }

        public bool TryApplyBuildPreset(int slot)
        {
            if (!HasBuildPreset(slot)) return false;
            var preset = buildPresets[slot];
            if (preset.stats == null || preset.stats.Length != stats.Length) return false;

            var requestedStatPoints = 0;
            foreach (var stat in preset.stats)
            {
                if (stat < 0) return false;
                requestedStatPoints += stat;
            }

            var totalAvailableStatPoints = UnspentStatPoints + GetAssignedStatPoints();
            if (requestedStatPoints > totalAvailableStatPoints) return false;
            if (!TryResolvePresetSkills(preset.equippedSkillIds, out var presetSkills)) return false;
            if (!TryResolvePresetEquipment(preset, out var presetEquipment)) return false;

            Array.Copy(preset.stats, stats, stats.Length);
            UnspentStatPoints = totalAvailableStatPoints - requestedStatPoints;
            equippedSkills.Clear();
            foreach (var skill in presetSkills) equippedSkills.Add(skill);
            equippedEquipment.Clear();
            foreach (var pair in presetEquipment) equippedEquipment[pair.Key] = pair.Value;
            return true;
        }

        public bool TryEquip(EquipmentDefinition equipment)
        {
            if (equipment == null) return false;
            if (!HasEquipment(equipment))
            {
                if (Gold < equipment.GoldCost) return false;
                Gold -= equipment.GoldCost;
                purchasedEquipment.Add(equipment);
            }

            equippedEquipment[equipment.Slot] = equipment;
            return true;
        }

        public bool TryGrantEquipment(EquipmentDefinition equipment)
        {
            if (equipment == null || HasEquipment(equipment)) return false;
            purchasedEquipment.Add(equipment);
            return true;
        }

        public void Unequip(EquipmentSlot slot)
        {
            equippedEquipment.Remove(slot);
        }

        public bool TryUpgradeEquipment(EquipmentDefinition equipment)
        {
            if (equipment == null || !HasEquipment(equipment)) return false;
            var level = GetEquipmentUpgradeLevel(equipment);
            var cost = equipment.GetUpgradeGoldCost(level);
            if (level >= equipment.MaximumUpgradeLevel || Gold < cost) return false;

            Gold -= cost;
            equipmentUpgradeLevels[equipment] = level + 1;
            return true;
        }

        public bool TryPurchaseLegacyUpgrade(LegacyUpgradeDefinition upgrade)
        {
            if (upgrade == null) return false;
            var currentRank = GetLegacyUpgradeRank(upgrade);
            if (currentRank >= upgrade.MaximumRank || AvailableLegacyShards < upgrade.ShardCostPerRank) return false;

            legacyUpgradeRanks[upgrade] = currentRank + 1;
            SpentLegacyShards += upgrade.ShardCostPerRank;
            return true;
        }

        public bool IsMilestoneComplete(MilestoneDefinition milestone)
        {
            return milestone != null && GetMilestoneProgress(milestone) >= milestone.TargetValue;
        }

        public int GetMilestoneProgress(MilestoneDefinition milestone)
        {
            if (milestone == null) return 0;
            switch (milestone.Condition)
            {
                case MilestoneCondition.Level: return Level;
                case MilestoneCondition.DungeonClears: return ClearedDungeonCount;
                case MilestoneCondition.Rebirths: return RebirthCount;
                default: return 0;
            }
        }

        public bool TryClaimMilestone(MilestoneDefinition milestone)
        {
            if (milestone == null || HasClaimedMilestone(milestone) || !IsMilestoneComplete(milestone)) return false;
            claimedMilestones.Add(milestone);
            Gold += milestone.GoldReward;
            UnspentStatPoints += milestone.StatPointReward;
            return true;
        }

        public void GainCombatReward(int gold, int experience, bool countTowardDailyContract = true)
        {
            var grantedGold = Mathf.CeilToInt(gold * (1f + LegacyShards * catalog.Progression.GoldBonusPerShard + GetLegacyGoldBonus()));
            Gold += grantedGold;
            if (countTowardDailyContract)
            {
                RefreshDailyContracts(DateTime.UtcNow);
                dailyCombatGold += Mathf.Max(0, grantedGold);
            }
            Experience += experience;
            while (Experience >= ExperienceToNextLevel)
            {
                Experience -= ExperienceToNextLevel;
                Level++;
                UnspentStatPoints += catalog.Progression.StatPointsPerLevel;
            }
        }

        public OfflineReward ClaimOfflineReward(DateTime utcNow, OfflineRewardCalculator calculator)
        {
            if (calculator == null || SelectedDungeon == null) return new OfflineReward(0, 0, 0);

            var estimatedClearSeconds = Mathf.Max(30f, SelectedDungeon.FloorCount * SelectedDungeon.FloorTimeLimit);
            var reward = calculator.Calculate(
                lastSavedUtcTicks,
                utcNow,
                SelectedDungeon.ClearGoldReward / estimatedClearSeconds,
                SelectedDungeon.ClearExperienceReward / estimatedClearSeconds);

            if (reward.HasReward) GainCombatReward(reward.Gold, reward.Experience, false);
            lastSavedUtcTicks = utcNow.Ticks;
            return reward;
        }

        public bool RecordDungeonClear(DungeonDefinition dungeon)
        {
            ClearedDungeonCount++;
            RefreshDailyContracts(DateTime.UtcNow);
            dailyRiftClearCount++;
            if (dungeon == null || GetDungeonMasteryRank(dungeon) >= dungeon.MaximumMasteryRank) return false;
            dungeonMasteryRanks[dungeon] = GetDungeonMasteryRank(dungeon) + 1;
            return true;
        }

        /// <summary>Starts a new contract day only when UTC has moved forward. Moving a device clock backward cannot reset rewards.</summary>
        public bool RefreshDailyContracts(DateTime utcNow)
        {
            var currentDay = utcNow.Date;
            if (dailyContractResetUtcTicks > 0)
            {
                try
                {
                    var savedDay = new DateTime(dailyContractResetUtcTicks, DateTimeKind.Utc).Date;
                    if (currentDay <= savedDay) return false;
                }
                catch (ArgumentOutOfRangeException)
                {
                    // A malformed legacy tick value is treated as an uninitialized daily state.
                }
            }

            dailyContractResetUtcTicks = currentDay.Ticks;
            dailyRiftClearCount = 0;
            dailyCombatGold = 0;
            dailyRiftClearClaimed = false;
            dailyCombatGoldClaimed = false;
            return true;
        }

        public int GetDailyContractProgress(DailyContractType contractType)
        {
            RefreshDailyContracts(DateTime.UtcNow);
            return contractType == DailyContractType.RiftClear ? dailyRiftClearCount : dailyCombatGold;
        }

        public int GetDailyContractTarget(DailyContractType contractType)
        {
            return contractType == DailyContractType.RiftClear ? DailyRiftClearTarget : DailyCombatGoldTarget;
        }

        public int GetDailyContractGoldReward(DailyContractType contractType)
        {
            return contractType == DailyContractType.RiftClear ? 180 : 240;
        }

        public int GetDailyContractStatPointReward(DailyContractType contractType)
        {
            return contractType == DailyContractType.RiftClear ? 1 : 2;
        }

        public bool HasClaimedDailyContract(DailyContractType contractType)
        {
            RefreshDailyContracts(DateTime.UtcNow);
            return contractType == DailyContractType.RiftClear ? dailyRiftClearClaimed : dailyCombatGoldClaimed;
        }

        public bool IsDailyContractComplete(DailyContractType contractType)
        {
            return GetDailyContractProgress(contractType) >= GetDailyContractTarget(contractType);
        }

        public bool TryClaimDailyContract(DailyContractType contractType)
        {
            RefreshDailyContracts(DateTime.UtcNow);
            if (HasClaimedDailyContract(contractType) || !IsDailyContractComplete(contractType)) return false;

            if (contractType == DailyContractType.RiftClear) dailyRiftClearClaimed = true;
            else dailyCombatGoldClaimed = true;
            Gold += GetDailyContractGoldReward(contractType);
            UnspentStatPoints += GetDailyContractStatPointReward(contractType);
            return true;
        }

        public bool TryRecordDungeonBestTime(DungeonDefinition dungeon, float clearSeconds)
        {
            if (dungeon == null || clearSeconds <= 0f) return false;
            if (!dungeonRecords.TryGetValue(dungeon, out var record))
            {
                record = new DungeonRecordData { dungeonId = dungeon.Id, bestClearSeconds = clearSeconds };
                dungeonRecords.Add(dungeon, record);
            }

            record.totalClears++;
            if (record.bestClearSeconds > 0f && clearSeconds >= record.bestClearSeconds) return false;
            record.bestClearSeconds = clearSeconds;
            return true;
        }

        public bool CanRebirth => Level >= catalog.Progression.RebirthRequiredLevel && ClearedDungeonCount >= catalog.Progression.RebirthRequiredClears;

        public bool TryRebirth()
        {
            if (!CanRebirth) return false;

            LegacyShards += catalog.Progression.RebirthShardReward;
            RebirthCount++;
            Array.Clear(stats, 0, stats.Length);
            unlockedSkills.Clear();
            equippedSkills.Clear();
            purchasedEquipment.Clear();
            equippedEquipment.Clear();
            equipmentUpgradeLevels.Clear();
            Level = 1;
            Experience = 0;
            Gold = catalog.StartingGold;
            UnspentStatPoints = catalog.StartingStatPoints;
            ClearedDungeonCount = 0;
            return true;
        }

        public GameSaveData CreateSaveData()
        {
            var data = new GameSaveData
            {
                version = GameSaveData.CurrentVersion,
                level = Level,
                experience = Experience,
                gold = Gold,
                unspentStatPoints = UnspentStatPoints,
                clearedDungeonCount = ClearedDungeonCount,
                rebirthCount = RebirthCount,
                legacyShards = LegacyShards,
                spentLegacyShards = SpentLegacyShards,
                selectedDungeonIndex = SelectedDungeonIndex,
                selectedProtocolIndex = SelectedProtocolIndex,
                selectedCombatDirectiveIndex = SelectedCombatDirectiveIndex,
                autoRepeatDungeon = AutoRepeatDungeon,
                soundEnabled = soundEnabled,
                vibrationEnabled = vibrationEnabled,
                lastSavedUtcTicks = lastSavedUtcTicks,
                dailyContractResetUtcTicks = dailyContractResetUtcTicks,
                dailyRiftClearCount = dailyRiftClearCount,
                dailyCombatGold = dailyCombatGold,
                dailyRiftClearClaimed = dailyRiftClearClaimed,
                dailyCombatGoldClaimed = dailyCombatGoldClaimed,
                stats = (int[])stats.Clone(),
                unlockedSkillIds = GetUnlockedSkillIds(),
                equippedSkillIds = GetEquippedSkillIds(),
                legacyUpgradeIds = GetLegacyUpgradeIds(),
                legacyUpgradeRanks = GetLegacyUpgradeRanks(),
                claimedMilestoneIds = GetClaimedMilestoneIds(),
                ownedEquipmentIds = GetOwnedEquipmentIds(),
                equipmentUpgradeIds = GetEquipmentUpgradeIds(),
                equipmentUpgradeLevels = GetEquipmentUpgradeLevels(),
                dungeonMasteryIds = GetDungeonMasteryIds(),
                dungeonMasteryRanks = GetDungeonMasteryRanks(),
                dungeonRecords = GetDungeonRecords(),
                weaponId = GetEquippedId(EquipmentSlot.Weapon),
                armorId = GetEquippedId(EquipmentSlot.Armor),
                bootsId = GetEquippedId(EquipmentSlot.Boots),
                ringId = GetEquippedId(EquipmentSlot.Ring),
                buildPresets = CloneBuildPresets(),
            };
            return data;
        }

        public bool TryLoad(GameSaveData data)
        {
            if (data == null || data.stats == null || data.stats.Length != stats.Length) return false;

            Level = Mathf.Max(1, data.level);
            Experience = Mathf.Max(0, data.experience);
            Gold = Mathf.Max(0, data.gold);
            UnspentStatPoints = Mathf.Max(0, data.unspentStatPoints);
            ClearedDungeonCount = Mathf.Max(0, data.clearedDungeonCount);
            RebirthCount = Mathf.Max(0, data.rebirthCount);
            LegacyShards = Mathf.Max(0, data.legacyShards);
            SpentLegacyShards = Mathf.Clamp(data.spentLegacyShards, 0, LegacyShards);
            SelectedDungeonIndex = Mathf.Clamp(data.selectedDungeonIndex, 0, Mathf.Max(0, catalog.Dungeons.Count - 1));
            SelectedProtocolIndex = Mathf.Clamp(data.selectedProtocolIndex, 0, Mathf.Max(0, catalog.DungeonProtocols.Count - 1));
            SelectedCombatDirectiveIndex = Mathf.Clamp(data.selectedCombatDirectiveIndex, 0, Mathf.Max(0, catalog.CombatDirectives.Count - 1));
            AutoRepeatDungeon = data.autoRepeatDungeon;
            soundEnabled = data.version < 15 || data.soundEnabled;
            vibrationEnabled = data.version < 15 || data.vibrationEnabled;
            lastSavedUtcTicks = data.lastSavedUtcTicks;
            dailyContractResetUtcTicks = data.dailyContractResetUtcTicks;
            dailyRiftClearCount = Mathf.Max(0, data.dailyRiftClearCount);
            dailyCombatGold = Mathf.Max(0, data.dailyCombatGold);
            dailyRiftClearClaimed = data.dailyRiftClearClaimed;
            dailyCombatGoldClaimed = data.dailyCombatGoldClaimed;
            RefreshDailyContracts(DateTime.UtcNow);
            if (catalog.Dungeons.Count > 0 && Level < catalog.Dungeons[SelectedDungeonIndex].RequiredLevel) SelectedDungeonIndex = 0;
            Array.Copy(data.stats, stats, stats.Length);
            unlockedSkills.Clear();
            equippedSkills.Clear();
            purchasedEquipment.Clear();
            equippedEquipment.Clear();
            equipmentUpgradeLevels.Clear();
            dungeonMasteryRanks.Clear();
            dungeonRecords.Clear();
            legacyUpgradeRanks.Clear();
            claimedMilestones.Clear();
            Array.Clear(buildPresets, 0, buildPresets.Length);
            LoadSkills(data.unlockedSkillIds);
            LoadEquippedSkills(data.equippedSkillIds);
            LoadLegacyUpgrades(data.legacyUpgradeIds, data.legacyUpgradeRanks);
            LoadClaimedMilestones(data.claimedMilestoneIds);
            LoadEquipment(data.ownedEquipmentIds);
            LoadEquipmentUpgrades(data.equipmentUpgradeIds, data.equipmentUpgradeLevels);
            LoadDungeonMastery(data.dungeonMasteryIds, data.dungeonMasteryRanks);
            LoadDungeonRecords(data.dungeonRecords);
            LoadBuildPresets(data.buildPresets);
            LoadEquipped(EquipmentSlot.Weapon, data.weaponId);
            LoadEquipped(EquipmentSlot.Armor, data.armorId);
            LoadEquipped(EquipmentSlot.Boots, data.bootsId);
            LoadEquipped(EquipmentSlot.Ring, data.ringId);
            return true;
        }

        private DungeonRecordData[] GetDungeonRecords()
        {
            var records = new List<DungeonRecordData>();
            foreach (var dungeon in catalog.Dungeons)
            {
                if (!dungeonRecords.TryGetValue(dungeon, out var record)) continue;
                records.Add(new DungeonRecordData
                {
                    dungeonId = dungeon.Id,
                    totalClears = record.totalClears,
                    bestClearSeconds = record.bestClearSeconds,
                });
            }
            return records.ToArray();
        }

        private void LoadDungeonRecords(DungeonRecordData[] savedRecords)
        {
            if (savedRecords == null) return;
            foreach (var savedRecord in savedRecords)
            {
                if (savedRecord == null || string.IsNullOrEmpty(savedRecord.dungeonId)) continue;
                foreach (var dungeon in catalog.Dungeons)
                {
                    if (dungeon.Id != savedRecord.dungeonId) continue;
                    dungeonRecords[dungeon] = new DungeonRecordData
                    {
                        dungeonId = dungeon.Id,
                        totalClears = Mathf.Max(0, savedRecord.totalClears),
                        bestClearSeconds = Mathf.Max(0f, savedRecord.bestClearSeconds),
                    };
                    break;
                }
            }
        }

        public CombatProfile CreateCombatProfile()
        {
            var progression = catalog.Progression;
            var damage = progression.BaseDamage + GetStat(StatType.Strength) * progression.DamagePerStrength;
            var attackInterval = Math.Max(progression.MinimumAttackInterval, progression.BaseAttackInterval - GetStat(StatType.Agility) * progression.AttackIntervalReductionPerAgility);
            var maxHealth = progression.BaseHealth + GetStat(StatType.Will) * progression.HealthPerWill + GetLegacyHealthBonus();
            var defense = GetStat(StatType.Will);
            var moveDelay = Math.Max(progression.MinimumMoveDelay, progression.BaseMoveDelay - GetStat(StatType.Agility) * progression.MoveDelayReductionPerAgility);
            var activeDamage = GetStat(StatType.Magic) * progression.ActiveDamagePerMagic;
            var criticalChance = GetStat(StatType.Sense) * progression.CriticalChancePerSense;
            var execute = false;

            foreach (var skill in catalog.SkillNodes)
            {
                if (!IsSkillEquipped(skill)) continue;
                damage += skill.DamageBonus;
                activeDamage += skill.ActiveDamageBonus;
                maxHealth += skill.MaxHealthBonus;
                moveDelay = Math.Max(0.01f, moveDelay - skill.MoveDelayReduction);
                execute |= skill.GrantsExecute;
            }

            foreach (var equipment in equippedEquipment.Values)
            {
                var multiplier = equipment.GetUpgradeMultiplier(GetEquipmentUpgradeLevel(equipment));
                damage += Mathf.RoundToInt(equipment.DamageBonus * multiplier);
                activeDamage += Mathf.RoundToInt(equipment.ActiveDamageBonus * multiplier);
                maxHealth += Mathf.RoundToInt(equipment.MaxHealthBonus * multiplier);
                defense += Mathf.RoundToInt(equipment.DefenseBonus * multiplier);
                moveDelay = Math.Max(0.01f, moveDelay - equipment.MoveDelayReduction * multiplier);
                criticalChance += equipment.CriticalChanceBonus * multiplier;
            }

            foreach (var equipmentSet in catalog.EquipmentSets)
            {
                if (!IsEquipmentSetActive(equipmentSet)) continue;
                damage += equipmentSet.DamageBonus;
                activeDamage += equipmentSet.ActiveDamageBonus;
                maxHealth += equipmentSet.MaxHealthBonus;
                defense += equipmentSet.DefenseBonus;
                moveDelay = Math.Max(0.01f, moveDelay - equipmentSet.MoveDelayReduction);
                criticalChance += equipmentSet.CriticalChanceBonus;
            }

            var directive = SelectedCombatDirective;
            if (directive != null)
            {
                damage = Mathf.CeilToInt(damage * directive.DamageMultiplier);
                maxHealth = Mathf.CeilToInt(maxHealth * directive.MaxHealthMultiplier);
                moveDelay = Math.Max(0.01f, moveDelay * directive.MoveDelayMultiplier);
                criticalChance += directive.CriticalChanceBonus;
            }

            damage = Mathf.CeilToInt(damage * (1f + LegacyShards * progression.DamageBonusPerShard + GetLegacyDamageBonus() + TotalDungeonMasteryDamageBonus));
            criticalChance = Mathf.Clamp(criticalChance, 0f, progression.MaximumCriticalChance);
            return new CombatProfile(damage, attackInterval, maxHealth, defense, moveDelay, activeDamage, criticalChance, execute);
        }

        private string[] GetUnlockedSkillIds()
        {
            var ids = new List<string>();
            foreach (var skill in catalog.SkillNodes) if (HasSkill(skill)) ids.Add(skill.Id);
            return ids.ToArray();
        }

        private BuildPresetData[] CloneBuildPresets()
        {
            var snapshots = new BuildPresetData[buildPresets.Length];
            for (var index = 0; index < buildPresets.Length; index++)
            {
                var preset = buildPresets[index];
                if (preset == null) continue;
                snapshots[index] = new BuildPresetData
                {
                    stats = preset.stats == null ? null : (int[])preset.stats.Clone(),
                    equippedSkillIds = preset.equippedSkillIds == null ? null : (string[])preset.equippedSkillIds.Clone(),
                    weaponId = preset.weaponId,
                    armorId = preset.armorId,
                    bootsId = preset.bootsId,
                    ringId = preset.ringId,
                };
            }
            return snapshots;
        }

        private string[] GetEquippedSkillIds()
        {
            var ids = new List<string>();
            foreach (var skill in catalog.SkillNodes) if (IsSkillEquipped(skill)) ids.Add(skill.Id);
            return ids.ToArray();
        }

        private string[] GetOwnedEquipmentIds()
        {
            var ids = new List<string>();
            foreach (var equipment in catalog.Equipment) if (HasEquipment(equipment)) ids.Add(equipment.Id);
            return ids.ToArray();
        }

        private string[] GetLegacyUpgradeIds()
        {
            var ids = new List<string>();
            foreach (var upgrade in catalog.LegacyUpgrades)
            {
                if (GetLegacyUpgradeRank(upgrade) > 0) ids.Add(upgrade.Id);
            }

            return ids.ToArray();
        }

        private string[] GetEquipmentUpgradeIds()
        {
            var ids = new List<string>();
            foreach (var equipment in catalog.Equipment)
            {
                if (GetEquipmentUpgradeLevel(equipment) > 0) ids.Add(equipment.Id);
            }

            return ids.ToArray();
        }

        private int[] GetEquipmentUpgradeLevels()
        {
            var levels = new List<int>();
            foreach (var equipment in catalog.Equipment)
            {
                var level = GetEquipmentUpgradeLevel(equipment);
                if (level > 0) levels.Add(level);
            }

            return levels.ToArray();
        }

        private string[] GetDungeonMasteryIds()
        {
            var ids = new List<string>();
            foreach (var dungeon in catalog.Dungeons) if (GetDungeonMasteryRank(dungeon) > 0) ids.Add(dungeon.Id);
            return ids.ToArray();
        }

        private int[] GetDungeonMasteryRanks()
        {
            var ranks = new List<int>();
            foreach (var dungeon in catalog.Dungeons)
            {
                var rank = GetDungeonMasteryRank(dungeon);
                if (rank > 0) ranks.Add(rank);
            }
            return ranks.ToArray();
        }

        private string[] GetClaimedMilestoneIds()
        {
            var ids = new List<string>();
            foreach (var milestone in catalog.Milestones) if (HasClaimedMilestone(milestone)) ids.Add(milestone.Id);
            return ids.ToArray();
        }

        private int[] GetLegacyUpgradeRanks()
        {
            var ranks = new List<int>();
            foreach (var upgrade in catalog.LegacyUpgrades)
            {
                var rank = GetLegacyUpgradeRank(upgrade);
                if (rank > 0) ranks.Add(rank);
            }

            return ranks.ToArray();
        }

        private string GetEquippedId(EquipmentSlot slot)
        {
            var equipment = GetEquipped(slot);
            return equipment == null ? string.Empty : equipment.Id;
        }

        private void LoadSkills(string[] ids)
        {
            if (ids == null) return;
            foreach (var skill in catalog.SkillNodes) if (Array.IndexOf(ids, skill.Id) >= 0) unlockedSkills.Add(skill);
        }

        private void LoadEquippedSkills(string[] ids)
        {
            if (ids == null)
            {
                foreach (var skill in catalog.SkillNodes)
                {
                    if (!HasSkill(skill) || EquippedSkillCount >= MaximumEquippedSkillCount) continue;
                    equippedSkills.Add(skill);
                }
                return;
            }

            foreach (var skill in catalog.SkillNodes)
            {
                if (!HasSkill(skill) || Array.IndexOf(ids, skill.Id) < 0 || EquippedSkillCount >= MaximumEquippedSkillCount) continue;
                equippedSkills.Add(skill);
            }
        }

        private void LoadBuildPresets(BuildPresetData[] savedPresets)
        {
            if (savedPresets == null) return;
            var count = Mathf.Min(buildPresets.Length, savedPresets.Length);
            for (var index = 0; index < count; index++)
            {
                var preset = savedPresets[index];
                if (preset == null) continue;
                buildPresets[index] = new BuildPresetData
                {
                    stats = preset.stats == null ? null : (int[])preset.stats.Clone(),
                    equippedSkillIds = preset.equippedSkillIds == null ? null : (string[])preset.equippedSkillIds.Clone(),
                    weaponId = preset.weaponId,
                    armorId = preset.armorId,
                    bootsId = preset.bootsId,
                    ringId = preset.ringId,
                };
            }
        }

        private bool TryResolvePresetSkills(string[] ids, out List<SkillNodeDefinition> presetSkills)
        {
            presetSkills = new List<SkillNodeDefinition>();
            if (ids == null) return true;
            foreach (var id in ids)
            {
                var skill = FindSkill(id);
                if (skill == null || !HasSkill(skill) || presetSkills.Contains(skill) || presetSkills.Count >= MaximumEquippedSkillCount) return false;
                presetSkills.Add(skill);
            }
            return true;
        }

        private bool TryResolvePresetEquipment(BuildPresetData preset, out Dictionary<EquipmentSlot, EquipmentDefinition> presetEquipment)
        {
            presetEquipment = new Dictionary<EquipmentSlot, EquipmentDefinition>();
            return TryAddPresetEquipment(presetEquipment, EquipmentSlot.Weapon, preset.weaponId)
                && TryAddPresetEquipment(presetEquipment, EquipmentSlot.Armor, preset.armorId)
                && TryAddPresetEquipment(presetEquipment, EquipmentSlot.Boots, preset.bootsId)
                && TryAddPresetEquipment(presetEquipment, EquipmentSlot.Ring, preset.ringId);
        }

        private bool TryAddPresetEquipment(Dictionary<EquipmentSlot, EquipmentDefinition> presetEquipment, EquipmentSlot slot, string id)
        {
            if (string.IsNullOrEmpty(id)) return true;
            var equipment = FindEquipment(id);
            if (equipment == null || equipment.Slot != slot || !HasEquipment(equipment)) return false;
            presetEquipment.Add(slot, equipment);
            return true;
        }

        private SkillNodeDefinition FindSkill(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var skill in catalog.SkillNodes) if (skill.Id == id) return skill;
            return null;
        }

        private EquipmentDefinition FindEquipment(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var equipment in catalog.Equipment) if (equipment.Id == id) return equipment;
            return null;
        }

        private int GetAssignedStatPoints()
        {
            var assigned = 0;
            foreach (var stat in stats) assigned += stat;
            return assigned;
        }

        private void LoadEquipment(string[] ids)
        {
            if (ids == null) return;
            foreach (var equipment in catalog.Equipment) if (Array.IndexOf(ids, equipment.Id) >= 0) purchasedEquipment.Add(equipment);
        }

        private void LoadLegacyUpgrades(string[] ids, int[] ranks)
        {
            if (ids == null || ranks == null) return;
            var count = Mathf.Min(ids.Length, ranks.Length);
            for (var index = 0; index < count; index++)
            {
                foreach (var upgrade in catalog.LegacyUpgrades)
                {
                    if (upgrade.Id != ids[index]) continue;
                    legacyUpgradeRanks[upgrade] = Mathf.Clamp(ranks[index], 0, upgrade.MaximumRank);
                    break;
                }
            }
        }

        private void LoadEquipmentUpgrades(string[] ids, int[] levels)
        {
            if (ids == null || levels == null) return;
            var count = Mathf.Min(ids.Length, levels.Length);
            for (var index = 0; index < count; index++)
            {
                foreach (var equipment in catalog.Equipment)
                {
                    if (equipment.Id != ids[index] || !HasEquipment(equipment)) continue;
                    equipmentUpgradeLevels[equipment] = Mathf.Clamp(levels[index], 0, equipment.MaximumUpgradeLevel);
                    break;
                }
            }
        }

        private void LoadDungeonMastery(string[] ids, int[] ranks)
        {
            if (ids == null || ranks == null) return;
            var count = Mathf.Min(ids.Length, ranks.Length);
            for (var index = 0; index < count; index++)
            {
                foreach (var dungeon in catalog.Dungeons)
                {
                    if (dungeon.Id != ids[index]) continue;
                    dungeonMasteryRanks[dungeon] = Mathf.Clamp(ranks[index], 0, dungeon.MaximumMasteryRank);
                    break;
                }
            }
        }

        private void LoadClaimedMilestones(string[] ids)
        {
            if (ids == null) return;
            foreach (var milestone in catalog.Milestones)
            {
                if (Array.IndexOf(ids, milestone.Id) >= 0) claimedMilestones.Add(milestone);
            }
        }

        private float GetLegacyDamageBonus()
        {
            var bonus = 0f;
            foreach (var upgrade in catalog.LegacyUpgrades) bonus += GetLegacyUpgradeRank(upgrade) * upgrade.DamageBonusPerRank;
            return bonus;
        }

        private float GetLegacyGoldBonus()
        {
            var bonus = 0f;
            foreach (var upgrade in catalog.LegacyUpgrades) bonus += GetLegacyUpgradeRank(upgrade) * upgrade.GoldBonusPerRank;
            return bonus;
        }

        private int GetLegacyHealthBonus()
        {
            var bonus = 0;
            foreach (var upgrade in catalog.LegacyUpgrades) bonus += GetLegacyUpgradeRank(upgrade) * upgrade.MaxHealthBonusPerRank;
            return bonus;
        }

        private void LoadEquipped(EquipmentSlot slot, string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            foreach (var equipment in catalog.Equipment)
            {
                if (equipment.Slot == slot && equipment.Id == id && HasEquipment(equipment)) equippedEquipment[slot] = equipment;
            }
        }
    }

    public readonly struct CombatProfile
    {
        public CombatProfile(int damage, float attackInterval, int maxHealth, int defense, float moveDelay, int activeDamage, float criticalChance, bool execute)
        {
            Damage = damage;
            AttackInterval = attackInterval;
            MaxHealth = maxHealth;
            Defense = defense;
            MoveDelay = moveDelay;
            ActiveDamage = activeDamage;
            CriticalChance = criticalChance;
            Execute = execute;
        }

        public int Damage { get; }
        public float AttackInterval { get; }
        public int MaxHealth { get; }
        public int Defense { get; }
        public float MoveDelay { get; }
        public int ActiveDamage { get; }
        public float CriticalChance { get; }
        public bool Execute { get; }
        public float ExpectedDamagePerSecond => Damage * (1f + CriticalChance) / AttackInterval + ActiveDamage / 5f;
    }
}

namespace StatusWindow.Progression
{
    public enum ProgressionGoalKind
    {
        SpendStatPoints,
        UnlockDungeon,
        ImproveMastery,
        Rebirth,
        ChallengeDungeon,
    }

    public readonly struct ProgressionGoal
    {
        public ProgressionGoal(ProgressionGoalKind kind, string title, string description)
        {
            Kind = kind;
            Title = title;
            Description = description;
        }

        public ProgressionGoalKind Kind { get; }
        public string Title { get; }
        public string Description { get; }
    }

    /// <summary>Chooses one actionable progression target from authoritative game state.</summary>
    public sealed class ProgressionGoalAdvisor
    {
        public ProgressionGoal Create(StatusWindow.StatusWindowGameState gameState)
        {
            if (gameState == null) return new ProgressionGoal(ProgressionGoalKind.ChallengeDungeon, "다음 목표", "진행 데이터를 불러오는 중입니다.");

            if (gameState.UnspentStatPoints > 0)
            {
                return new ProgressionGoal(
                    ProgressionGoalKind.SpendStatPoints,
                    "지금 성장 가능",
                    $"남은 스탯 포인트 {gameState.UnspentStatPoints}개를 배분해 현재 균열 공략률을 높이세요.");
            }

            var nextDungeon = FindNextLockedDungeon(gameState);
            if (nextDungeon != null)
            {
                return new ProgressionGoal(
                    ProgressionGoalKind.UnlockDungeon,
                    "다음 균열 해금",
                    $"{nextDungeon.DisplayName} · Lv. {nextDungeon.RequiredLevel} 필요 (현재 Lv. {gameState.Level})");
            }

            var selectedDungeon = gameState.SelectedDungeon;
            if (selectedDungeon != null && gameState.GetDungeonMasteryRank(selectedDungeon) < selectedDungeon.MaximumMasteryRank)
            {
                return new ProgressionGoal(
                    ProgressionGoalKind.ImproveMastery,
                    "균열 숙련도 강화",
                    $"{selectedDungeon.DisplayName} {gameState.GetDungeonMasteryRank(selectedDungeon)}/{selectedDungeon.MaximumMasteryRank} · 공략마다 영구 공격력 +{selectedDungeon.DamageBonusPerMasteryRank:P0}");
            }

            if (gameState.CanRebirth)
            {
                return new ProgressionGoal(
                    ProgressionGoalKind.Rebirth,
                    "회귀 가능",
                    "기억의 파편을 얻어 다음 빌드의 영구 성장 보너스를 선택하세요.");
            }

            return selectedDungeon == null
                ? new ProgressionGoal(ProgressionGoalKind.ChallengeDungeon, "다음 목표", "균열을 선택하고 자동전투를 시작하세요.")
                : new ProgressionGoal(ProgressionGoalKind.ChallengeDungeon, "현재 균열 도전", $"{selectedDungeon.DisplayName} 공략 결과를 보고 빌드를 조정하세요.");
        }

        private static DungeonDefinition FindNextLockedDungeon(StatusWindow.StatusWindowGameState gameState)
        {
            DungeonDefinition candidate = null;
            foreach (var dungeon in gameState.Catalog.Dungeons)
            {
                if (dungeon == null || dungeon.RequiredLevel <= gameState.Level) continue;
                if (candidate == null || dungeon.RequiredLevel < candidate.RequiredLevel) candidate = dungeon;
            }
            return candidate;
        }
    }
}
