using System;

namespace StatusWindow.Progression
{
    /// <summary>Small repeatable goals that reuse the existing combat loop.</summary>
    public enum DailyContractType
    {
        RiftClear,
        CombatGold,
    }

    [Serializable]
    public sealed class GameSaveData
    {
        public const int CurrentVersion = 15;

        public int version = CurrentVersion;
        public int level;
        public int experience;
        public int gold;
        public int unspentStatPoints;
        public int clearedDungeonCount;
        public int rebirthCount;
        public int legacyShards;
        public int spentLegacyShards;
        public int selectedDungeonIndex;
        public int selectedProtocolIndex;
        public int selectedCombatDirectiveIndex;
        public bool autoRepeatDungeon;
        public bool soundEnabled;
        public bool vibrationEnabled;
        public long lastSavedUtcTicks;
        public long dailyContractResetUtcTicks;
        public int dailyRiftClearCount;
        public int dailyCombatGold;
        public bool dailyRiftClearClaimed;
        public bool dailyCombatGoldClaimed;
        public int[] stats;
        public string[] unlockedSkillIds;
        public string[] equippedSkillIds;
        public string[] legacyUpgradeIds;
        public string[] claimedMilestoneIds;
        public int[] legacyUpgradeRanks;
        public string[] ownedEquipmentIds;
        public string[] equipmentUpgradeIds;
        public int[] equipmentUpgradeLevels;
        public string[] dungeonMasteryIds;
        public int[] dungeonMasteryRanks;
        public DungeonRecordData[] dungeonRecords;
        public string weaponId;
        public string armorId;
        public string bootsId;
        public string ringId;
        public BuildPresetData[] buildPresets;
    }

    [Serializable]
    public sealed class BuildPresetData
    {
        public int[] stats;
        public string[] equippedSkillIds;
        public string weaponId;
        public string armorId;
        public string bootsId;
        public string ringId;
    }

    [Serializable]
    public sealed class DungeonRecordData
    {
        public string dungeonId;
        public int totalClears;
        public float bestClearSeconds;
    }
}
