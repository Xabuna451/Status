using System.Collections.Generic;
using StatusWindow.Progression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StatusWindow.Editor
{
    [InitializeOnLoad]
    public static class PrototypeDataAssetCreator
    {
        private const string DataRoot = "Assets/03. ScriptableObjects/00. Prototype";
        private const string CatalogPath = DataRoot + "/StatusWindowPrototypeCatalog.asset";
        private const string PrototypeScenePath = "Assets/Scenes/SampleScene.unity";

        static PrototypeDataAssetCreator()
        {
            EditorApplication.delayCall += CreateAssetsIfMissing;
        }

        [MenuItem("StatusWindow/Create Prototype Data")]
        public static void CreateAssetsIfMissing()
        {
            var existingCatalog = AssetDatabase.LoadAssetAtPath<PrototypeCatalog>(CatalogPath);
            if (existingCatalog != null)
            {
                UpdateExistingPrototypeData(existingCatalog);
                EnsureSceneBootstrap(existingCatalog);
                return;
            }

            EnsureFolder("Assets/03. ScriptableObjects", "00. Prototype");

            var sharpness = CreateSkill("Skill_Sharpness", "sharpness", "예리함", "기본 공격력 +8", 45, null, 8, 0, 0, 0f, false);
            var quickStep = CreateSkill("Skill_QuickStep", "quick_step", "신속한 발걸음", "다음 적까지 이동 시간 감소", 55, null, 0, 0, 0, 0.12f, false);
            var vitality = CreateSkill("Skill_Vitality", "vitality", "생명력", "최대 체력 +35", 55, null, 0, 0, 35, 0f, false);
            var arcaneBurst = CreateSkill("Skill_ArcaneBurst", "arcane_burst", "마력 폭발", "자동 액티브 스킬 피해 +24", 65, null, 0, 24, 0, 0f, false);
            var execution = CreateSkill("Skill_Execution", "execution", "처형", "적 체력이 25% 이하일 때 즉시 처형", 90, sharpness, 0, 0, 0, 0f, true);
            var overdrive = CreateSkill("Skill_Overdrive", "overdrive", "한계 가속", "기본 공격력 +12", 100, sharpness, 12, 0, 0, 0f, false);
            var phantomStep = CreateSkill("Skill_PhantomStep", "phantom_step", "환영 보행", "다음 적까지 이동 시간 추가 감소", 95, quickStep, 0, 0, 0, 0.12f, false);
            var ironHeart = CreateSkill("Skill_IronHeart", "iron_heart", "강철 심장", "최대 체력 +50", 95, vitality, 0, 0, 50, 0f, false);
            var manaEcho = CreateSkill("Skill_ManaEcho", "mana_echo", "마력 메아리", "자동 액티브 스킬 피해 +35", 110, arcaneBurst, 0, 35, 0, 0f, false);

            var trainingSword = CreateEquipment("Equipment_TrainingSword", "training_sword", EquipmentSlot.Weapon, "훈련용 검", "기본 공격력 +7", 60, 7, 0, 0, 0, 0f, 0f);
            var manaStaff = CreateEquipment("Equipment_ManaStaff", "mana_staff", EquipmentSlot.Weapon, "마력 지팡이", "액티브 스킬 피해 +18", 70, 0, 18, 0, 0, 0f, 0f);
            var reinforcedCoat = CreateEquipment("Equipment_ReinforcedCoat", "reinforced_coat", EquipmentSlot.Armor, "강화 코트", "최대 체력 +30, 방어 +4", 75, 0, 0, 30, 4, 0f, 0f);
            var barrierJacket = CreateEquipment("Equipment_BarrierJacket", "barrier_jacket", EquipmentSlot.Armor, "방호 재킷", "최대 체력 +10, 방어 +8", 105, 0, 0, 10, 8, 0f, 0f);
            var swiftBoots = CreateEquipment("Equipment_SwiftBoots", "swift_boots", EquipmentSlot.Boots, "질풍의 장화", "이동 지연 감소", 60, 0, 0, 0, 0, 0.18f, 0f);
            var assaultBoots = CreateEquipment("Equipment_AssaultBoots", "assault_boots", EquipmentSlot.Boots, "돌격 장화", "기본 공격력 +4, 이동 지연 감소", 95, 4, 0, 0, 0, 0.08f, 0f);
            var focusRing = CreateEquipment("Equipment_FocusRing", "focus_ring", EquipmentSlot.Ring, "집중의 반지", "치명타 확률 +12%", 70, 0, 0, 0, 0, 0f, 0.12f);
            var vitalityRing = CreateEquipment("Equipment_VitalityRing", "vitality_ring", EquipmentSlot.Ring, "생명의 반지", "최대 체력 +25, 기본 공격력 +3", 100, 3, 0, 25, 0, 0f, 0f);
            var plasmaBlade = CreateEquipment("Equipment_PlasmaBlade", "plasma_blade", EquipmentSlot.Weapon, "플라즈마 블레이드", "기본 공격력 +14, 치명타 확률 +6%", 150, 14, 0, 0, 0, 0f, 0.06f);
            var phantomCoat = CreateEquipment("Equipment_PhantomCoat", "phantom_coat", EquipmentSlot.Armor, "환영 코트", "최대 체력 +35, 방어 +7, 이동 지연 감소", 155, 0, 0, 35, 7, 0.04f, 0f);
            var severanceBoots = CreateEquipment("Equipment_SeveranceBoots", "severance_boots", EquipmentSlot.Boots, "단절의 장화", "기본 공격력 +6, 이동 지연 감소", 140, 6, 0, 0, 0, 0.12f, 0f);
            var voidRing = CreateEquipment("Equipment_VoidRing", "void_ring", EquipmentSlot.Ring, "공허의 반지", "자동 액티브 피해 +30, 치명타 확률 +8%", 160, 0, 30, 0, 0, 0f, 0.08f);
            var entrySet = CreateEquipmentSet("EquipmentSet_Entry", "entry_set", "균열 입문자", "훈련용 검과 질풍의 장화: 공격력 +4, 이동 지연 -0.05초", new List<EquipmentDefinition> { trainingSword, swiftBoots }, 4, 0, 0, 0, 0.05f, 0f);
            var survivalSet = CreateEquipmentSet("EquipmentSet_Survival", "survival_set", "생존 전술", "강화 코트와 생명의 반지: 최대 체력 +25, 방어 +3", new List<EquipmentDefinition> { reinforcedCoat, vitalityRing }, 0, 0, 25, 3, 0f, 0f);
            var arcaneSet = CreateEquipmentSet("EquipmentSet_Arcane", "arcane_set", "마력 동조", "마력 지팡이와 집중의 반지: 액티브 피해 +15, 치명타 +6%", new List<EquipmentDefinition> { manaStaff, focusRing }, 0, 15, 0, 0, 0f, 0.06f);
            var voidSet = CreateEquipmentSet("EquipmentSet_Void", "void_set", "공허 절단", "플라즈마 블레이드와 공허의 반지: 공격력 +10, 액티브 피해 +20", new List<EquipmentDefinition> { plasmaBlade, voidRing }, 10, 20, 0, 0, 0f, 0f);

            var progression = ScriptableObject.CreateInstance<ProgressionDefinition>();
            progression.ConfigurePrototype();
            AssetDatabase.CreateAsset(progression, DataRoot + "/Progression_Prototype.asset");

            var inheritedStrength = CreateLegacyUpgrade("Legacy_InheritedStrength", "inherited_strength", "전생의 근력", "랭크당 공격력 +8%", 3, 1, 0.08f, 0f, 0);
            var gildedMemory = CreateLegacyUpgrade("Legacy_GildedMemory", "gilded_memory", "황금의 기억", "랭크당 골드 획득량 +12%", 3, 1, 0f, 0.12f, 0);
            var unbrokenSoul = CreateLegacyUpgrade("Legacy_UnbrokenSoul", "unbroken_soul", "불굴의 혼", "랭크당 최대 체력 +25", 3, 1, 0f, 0f, 25);
            var standardProtocol = CreateProtocol("Protocol_Standard", "standard", "표준 프로토콜", "기본 난이도와 보상으로 공략합니다.", 1f, 1f, 1f, 1f);
            var overloadProtocol = CreateProtocol("Protocol_Overload", "overload", "과부하 프로토콜", "적 체력 +50%, 보상 +60%", 1.5f, 1f, 1f, 1.6f);
            var pressureProtocol = CreateProtocol("Protocol_Pressure", "pressure", "압박 프로토콜", "제한시간 -25%, 보상 +70%", 1f, 1f, 0.75f, 1.7f);
            var assaultProtocol = CreateProtocol("Protocol_Assault", "assault", "습격 프로토콜", "적 피해 +50%, 보상 +65%", 1f, 1.5f, 1f, 1.65f);
            var anomalyProtocol = CreateProtocol("Protocol_Anomaly", "anomaly", "변칙 프로토콜", "적 체력 +30%, 피해 +25%, 제한시간 -10%, 보상 +125%", 1.3f, 1.25f, 0.9f, 2.25f);
            var assaultDirective = CreateDirective("Directive_Assault", "assault", "공세 지침", "기본 공격력 +15%, 최대 체력 -10%", 1.15f, 0.9f, 1f, 0f);
            var pursuitDirective = CreateDirective("Directive_Pursuit", "pursuit", "추적 지침", "다음 적까지 이동 시간 -20%, 치명타 +5%", 1f, 1f, 0.8f, 0.05f);
            var fortifyDirective = CreateDirective("Directive_Fortify", "fortify", "방호 지침", "최대 체력 +20%, 기본 공격력 -8%", 0.92f, 1.2f, 1f, 0f);
            var firstClear = CreateMilestone("Milestone_FirstClear", "first_clear", "첫 균열 돌파", "던전을 1회 클리어하세요.", MilestoneCondition.DungeonClears, 1, 40, 1);
            var risingHunter = CreateMilestone("Milestone_Level4", "level_4", "성장하는 사냥꾼", "레벨 4에 도달하세요.", MilestoneCondition.Level, 4, 70, 1);
            var deepDiver = CreateMilestone("Milestone_Level8", "level_8", "심연의 문턱", "레벨 8에 도달하세요.", MilestoneCondition.Level, 8, 120, 2);
            var secondClear = CreateMilestone("Milestone_Clear3", "clear_3", "균열 숙련자", "던전을 3회 클리어하세요.", MilestoneCondition.DungeonClears, 3, 100, 1);
            var firstRebirth = CreateMilestone("Milestone_Rebirth", "first_rebirth", "다시 쓴 상태창", "회귀를 1회 완료하세요.", MilestoneCondition.Rebirths, 1, 150, 2);
            var voidWalker = CreateMilestone("Milestone_Level12", "level_12", "공허를 걷는 자", "레벨 12에 도달하세요.", MilestoneCondition.Level, 12, 220, 3);
            var veteranHunter = CreateMilestone("Milestone_Clear10", "clear_10", "균열의 지배자", "던전을 10회 클리어하세요.", MilestoneCondition.DungeonClears, 10, 260, 3);

            var watcher = CreateEnemy("Enemy_Watcher", "rift_watcher", "균열 감시자", "균열의 흐름을 읽으며 약한 공격을 빠르게 반복합니다.", 0.85f, 0.8f, 0.85f, 0.9f, 0.9f);
            var devourer = CreateEnemy("Enemy_Devourer", "mana_devourer", "마력 포식자", "두꺼운 마력 껍질로 피해를 버티는 괴물입니다.", 1.45f, 1.1f, 1.45f, 1.2f, 1.2f);
            var berserker = CreateEnemy("Enemy_Berserker", "rift_berserker", "균열 광전사", "느리지만 묵직한 공격을 날립니다.", 1.1f, 1.55f, 1.7f, 1.1f, 1.1f);
            var guardian = CreateEnemy("Enemy_Guardian", "rift_guardian", "균열 수호자", "층의 끝을 지키는 보스입니다. 높은 체력과 보상을 가집니다.", 2.2f, 1.45f, 1.35f, 1.7f, 1.6f);
            watcher.SetCombatTrait(EnemyCombatTrait.Swift);
            devourer.SetCombatTrait(EnemyCombatTrait.Barrier);
            berserker.SetCombatTrait(EnemyCombatTrait.Enrage);
            guardian.SetCombatTrait(EnemyCombatTrait.BarrierEnrage);

            var dungeon = CreateDungeon("Dungeon_Training", "training_rift", "훈련 균열", "기본 빌드와 시간제한을 익히는 첫 던전", 1, 3, 35f, 5, 3, 18, 14, 2, 5, 2, 60, 40);
            var dungeon2 = CreateDungeon("Dungeon_Deep", "deep_rift", "심층 균열", "더 빠른 처치와 안정적인 생존을 요구합니다.", 4, 4, 32f, 6, 4, 35, 22, 3, 8, 3, 110, 75);
            var dungeon3 = CreateDungeon("Dungeon_Calamity", "calamity_rift", "재앙 균열", "완성된 빌드와 회귀 보너스를 시험하는 고난도 균열입니다.", 8, 5, 28f, 7, 5, 58, 32, 4, 12, 4, 180, 120);
            var dungeon4 = CreateDungeon("Dungeon_Void", "void_rift", "공허 균열", "모든 성장 축과 전술 지침을 요구하는 최심부 균열입니다.", 12, 6, 26f, 8, 5, 82, 40, 5, 16, 5, 270, 190);
            var encounters = new List<EnemyDefinition> { watcher, devourer, berserker };
            dungeon.ConfigureEncounters(new List<EnemyDefinition> { watcher, watcher, devourer }, guardian);
            dungeon2.ConfigureEncounters(new List<EnemyDefinition> { devourer, berserker, berserker }, guardian);
            dungeon3.ConfigureEncounters(new List<EnemyDefinition> { watcher, berserker, guardian }, guardian);
            dungeon4.ConfigureEncounters(new List<EnemyDefinition> { devourer, guardian, guardian }, guardian);

            var catalog = ScriptableObject.CreateInstance<PrototypeCatalog>();
            catalog.ConfigurePrototype(
                80,
                5,
                25,
                progression,
                new List<SkillNodeDefinition> { sharpness, quickStep, vitality, arcaneBurst, execution, overdrive, phantomStep, ironHeart, manaEcho },
                new List<EquipmentDefinition> { trainingSword, manaStaff, reinforcedCoat, barrierJacket, swiftBoots, assaultBoots, focusRing, vitalityRing, plasmaBlade, phantomCoat, severanceBoots, voidRing },
                new List<EquipmentSetDefinition> { entrySet, survivalSet, arcaneSet, voidSet },
                new List<LegacyUpgradeDefinition> { inheritedStrength, gildedMemory, unbrokenSoul },
                new List<DungeonProtocolDefinition> { standardProtocol, overloadProtocol, pressureProtocol, assaultProtocol, anomalyProtocol },
                new List<CombatDirectiveDefinition> { assaultDirective, pursuitDirective, fortifyDirective },
                new List<MilestoneDefinition> { firstClear, risingHunter, deepDiver, secondClear, firstRebirth, voidWalker, veteranHunter },
                dungeon,
                new List<DungeonDefinition> { dungeon, dungeon2, dungeon3, dungeon4 });
            AssignPrototypeVisuals(catalog);
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EnsureSceneBootstrap(catalog);
            Debug.Log("StatusWindow prototype ScriptableObject data created.");
        }

        private static SkillNodeDefinition CreateSkill(string assetName, string id, string displayName, string description, int goldCost, SkillNodeDefinition prerequisite, int damageBonus, int activeDamageBonus, int maxHealthBonus, float moveDelayReduction, bool grantsExecute)
        {
            var asset = ScriptableObject.CreateInstance<SkillNodeDefinition>();
            asset.ConfigurePrototype(id, displayName, description, goldCost, prerequisite, damageBonus, activeDamageBonus, maxHealthBonus, moveDelayReduction, grantsExecute);
            AssetDatabase.CreateAsset(asset, $"{DataRoot}/{assetName}.asset");
            return asset;
        }

        private static EquipmentDefinition CreateEquipment(string assetName, string id, EquipmentSlot slot, string displayName, string description, int goldCost, int damageBonus, int activeDamageBonus, int maxHealthBonus, int defenseBonus, float moveDelayReduction, float criticalChanceBonus)
        {
            var asset = ScriptableObject.CreateInstance<EquipmentDefinition>();
            asset.ConfigurePrototype(id, slot, displayName, description, goldCost, damageBonus, activeDamageBonus, maxHealthBonus, defenseBonus, moveDelayReduction, criticalChanceBonus);
            AssetDatabase.CreateAsset(asset, $"{DataRoot}/{assetName}.asset");
            return asset;
        }

        private static EquipmentSetDefinition CreateEquipmentSet(string assetName, string id, string displayName, string description, List<EquipmentDefinition> requiredEquipment, int damageBonus, int activeDamageBonus, int maxHealthBonus, int defenseBonus, float moveDelayReduction, float criticalChanceBonus)
        {
            var asset = ScriptableObject.CreateInstance<EquipmentSetDefinition>();
            asset.ConfigurePrototype(id, displayName, description, requiredEquipment, damageBonus, activeDamageBonus, maxHealthBonus, defenseBonus, moveDelayReduction, criticalChanceBonus);
            AssetDatabase.CreateAsset(asset, $"{DataRoot}/{assetName}.asset");
            return asset;
        }

        private static EnemyDefinition CreateEnemy(string assetName, string id, string displayName, string description, float healthMultiplier, float damageMultiplier, float attackInterval, float goldMultiplier, float experienceMultiplier)
        {
            var asset = ScriptableObject.CreateInstance<EnemyDefinition>();
            asset.ConfigurePrototype(id, displayName, description, healthMultiplier, damageMultiplier, attackInterval, goldMultiplier, experienceMultiplier);
            AssetDatabase.CreateAsset(asset, $"{DataRoot}/{assetName}.asset");
            return asset;
        }

        private static LegacyUpgradeDefinition CreateLegacyUpgrade(string assetName, string id, string displayName, string description, int maximumRank, int shardCostPerRank, float damageBonusPerRank, float goldBonusPerRank, int maxHealthBonusPerRank)
        {
            var asset = ScriptableObject.CreateInstance<LegacyUpgradeDefinition>();
            asset.ConfigurePrototype(id, displayName, description, maximumRank, shardCostPerRank, damageBonusPerRank, goldBonusPerRank, maxHealthBonusPerRank);
            AssetDatabase.CreateAsset(asset, $"{DataRoot}/{assetName}.asset");
            return asset;
        }

        private static DungeonProtocolDefinition CreateProtocol(string assetName, string id, string displayName, string description, float enemyHealthMultiplier, float enemyDamageMultiplier, float timeLimitMultiplier, float rewardMultiplier)
        {
            var asset = ScriptableObject.CreateInstance<DungeonProtocolDefinition>();
            asset.ConfigurePrototype(id, displayName, description, enemyHealthMultiplier, enemyDamageMultiplier, timeLimitMultiplier, rewardMultiplier);
            AssetDatabase.CreateAsset(asset, $"{DataRoot}/{assetName}.asset");
            return asset;
        }

        private static CombatDirectiveDefinition CreateDirective(string assetName, string id, string displayName, string description, float damageMultiplier, float maxHealthMultiplier, float moveDelayMultiplier, float criticalChanceBonus)
        {
            var asset = ScriptableObject.CreateInstance<CombatDirectiveDefinition>();
            asset.ConfigurePrototype(id, displayName, description, damageMultiplier, maxHealthMultiplier, moveDelayMultiplier, criticalChanceBonus);
            AssetDatabase.CreateAsset(asset, $"{DataRoot}/{assetName}.asset");
            return asset;
        }

        private static MilestoneDefinition CreateMilestone(string assetName, string id, string displayName, string description, MilestoneCondition condition, int targetValue, int goldReward, int statPointReward)
        {
            var asset = ScriptableObject.CreateInstance<MilestoneDefinition>();
            asset.ConfigurePrototype(id, displayName, description, condition, targetValue, goldReward, statPointReward);
            AssetDatabase.CreateAsset(asset, $"{DataRoot}/{assetName}.asset");
            return asset;
        }

        private static DungeonDefinition CreateDungeon(string assetName, string id, string displayName, string description, int requiredLevel, int floorCount, float floorTimeLimit, int baseKillTarget, int killTargetPerFloor, int baseEnemyHealth, int enemyHealthPerFloor, int enemyHealthPerKill, int enemyDamageBase, int enemyDamagePerFloor, int clearGoldReward, int clearExperienceReward)
        {
            var asset = ScriptableObject.CreateInstance<DungeonDefinition>();
            asset.ConfigurePrototype(id, displayName, description, requiredLevel, floorCount, floorTimeLimit, baseKillTarget, killTargetPerFloor, baseEnemyHealth, enemyHealthPerFloor, enemyHealthPerKill, enemyDamageBase, enemyDamagePerFloor, clearGoldReward, clearExperienceReward);
            AssetDatabase.CreateAsset(asset, $"{DataRoot}/{assetName}.asset");
            return asset;
        }

        private static void EnsureFolder(string parentFolder, string folderName)
        {
            if (!AssetDatabase.IsValidFolder($"{parentFolder}/{folderName}"))
            {
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
        }

        private static void UpdateExistingPrototypeData(PrototypeCatalog catalog)
        {
            var progression = AssetDatabase.LoadAssetAtPath<ProgressionDefinition>(DataRoot + "/Progression_Prototype.asset");
            if (progression == null)
            {
                progression = ScriptableObject.CreateInstance<ProgressionDefinition>();
                progression.ConfigurePrototype();
                AssetDatabase.CreateAsset(progression, DataRoot + "/Progression_Prototype.asset");
            }

            catalog.SetProgressionIfMissing(progression);
            AssignPrototypeVisuals(catalog);
            progression.EnsureCombatDefaults();
            EditorUtility.SetDirty(catalog);
            EditorUtility.SetDirty(progression);

            var inheritedStrength = GetOrCreateLegacyUpgrade("Legacy_InheritedStrength", "inherited_strength", "전생의 근력", "랭크당 공격력 +8%", 3, 1, 0.08f, 0f, 0);
            var gildedMemory = GetOrCreateLegacyUpgrade("Legacy_GildedMemory", "gilded_memory", "황금의 기억", "랭크당 골드 획득량 +12%", 3, 1, 0f, 0.12f, 0);
            var unbrokenSoul = GetOrCreateLegacyUpgrade("Legacy_UnbrokenSoul", "unbroken_soul", "불굴의 혼", "랭크당 최대 체력 +25", 3, 1, 0f, 0f, 25);
            catalog.SetLegacyUpgradesIfEmpty(new List<LegacyUpgradeDefinition> { inheritedStrength, gildedMemory, unbrokenSoul });
            EditorUtility.SetDirty(catalog);
            var standardProtocol = GetOrCreateProtocol("Protocol_Standard", "standard", "표준 프로토콜", "기본 난이도와 보상으로 공략합니다.", 1f, 1f, 1f, 1f);
            var overloadProtocol = GetOrCreateProtocol("Protocol_Overload", "overload", "과부하 프로토콜", "적 체력 +50%, 보상 +60%", 1.5f, 1f, 1f, 1.6f);
            var pressureProtocol = GetOrCreateProtocol("Protocol_Pressure", "pressure", "압박 프로토콜", "제한시간 -25%, 보상 +70%", 1f, 1f, 0.75f, 1.7f);
            var assaultProtocol = GetOrCreateProtocol("Protocol_Assault", "assault", "습격 프로토콜", "적 피해 +50%, 보상 +65%", 1f, 1.5f, 1f, 1.65f);
            var anomalyProtocol = GetOrCreateProtocol("Protocol_Anomaly", "anomaly", "변칙 프로토콜", "적 체력 +30%, 피해 +25%, 제한시간 -10%, 보상 +125%", 1.3f, 1.25f, 0.9f, 2.25f);
            catalog.AddDungeonProtocolIfMissing(standardProtocol);
            catalog.AddDungeonProtocolIfMissing(overloadProtocol);
            catalog.AddDungeonProtocolIfMissing(pressureProtocol);
            catalog.AddDungeonProtocolIfMissing(assaultProtocol);
            catalog.AddDungeonProtocolIfMissing(anomalyProtocol);
            var assaultDirective = GetOrCreateDirective("Directive_Assault", "assault", "공세 지침", "기본 공격력 +15%, 최대 체력 -10%", 1.15f, 0.9f, 1f, 0f);
            var pursuitDirective = GetOrCreateDirective("Directive_Pursuit", "pursuit", "추적 지침", "다음 적까지 이동 시간 -20%, 치명타 +5%", 1f, 1f, 0.8f, 0.05f);
            var fortifyDirective = GetOrCreateDirective("Directive_Fortify", "fortify", "방호 지침", "최대 체력 +20%, 기본 공격력 -8%", 0.92f, 1.2f, 1f, 0f);
            catalog.AddCombatDirectiveIfMissing(assaultDirective);
            catalog.AddCombatDirectiveIfMissing(pursuitDirective);
            catalog.AddCombatDirectiveIfMissing(fortifyDirective);
            EditorUtility.SetDirty(catalog);
            var firstClear = GetOrCreateMilestone("Milestone_FirstClear", "first_clear", "첫 균열 돌파", "던전을 1회 클리어하세요.", MilestoneCondition.DungeonClears, 1, 40, 1);
            var risingHunter = GetOrCreateMilestone("Milestone_Level4", "level_4", "성장하는 사냥꾼", "레벨 4에 도달하세요.", MilestoneCondition.Level, 4, 70, 1);
            var deepDiver = GetOrCreateMilestone("Milestone_Level8", "level_8", "심연의 문턱", "레벨 8에 도달하세요.", MilestoneCondition.Level, 8, 120, 2);
            var secondClear = GetOrCreateMilestone("Milestone_Clear3", "clear_3", "균열 숙련자", "던전을 3회 클리어하세요.", MilestoneCondition.DungeonClears, 3, 100, 1);
            var firstRebirth = GetOrCreateMilestone("Milestone_Rebirth", "first_rebirth", "다시 쓴 상태창", "회귀를 1회 완료하세요.", MilestoneCondition.Rebirths, 1, 150, 2);
            var voidWalker = GetOrCreateMilestone("Milestone_Level12", "level_12", "공허를 걷는 자", "레벨 12에 도달하세요.", MilestoneCondition.Level, 12, 220, 3);
            var veteranHunter = GetOrCreateMilestone("Milestone_Clear10", "clear_10", "균열의 지배자", "던전을 10회 클리어하세요.", MilestoneCondition.DungeonClears, 10, 260, 3);
            catalog.AddMilestoneIfMissing(firstClear);
            catalog.AddMilestoneIfMissing(risingHunter);
            catalog.AddMilestoneIfMissing(deepDiver);
            catalog.AddMilestoneIfMissing(secondClear);
            catalog.AddMilestoneIfMissing(firstRebirth);
            catalog.AddMilestoneIfMissing(voidWalker);
            catalog.AddMilestoneIfMissing(veteranHunter);
            EditorUtility.SetDirty(catalog);

            var sharpness = FindSkill(catalog, "sharpness");
            var quickStep = FindSkill(catalog, "quick_step");
            var vitality = FindSkill(catalog, "vitality");
            var arcaneBurst = FindSkill(catalog, "arcane_burst");
            AddSkill(catalog, "Skill_Overdrive", "overdrive", "한계 가속", "기본 공격력 +12", 100, sharpness, 12, 0, 0, 0f, false);
            AddSkill(catalog, "Skill_PhantomStep", "phantom_step", "환영 보행", "다음 적까지 이동 시간 추가 감소", 95, quickStep, 0, 0, 0, 0.12f, false);
            AddSkill(catalog, "Skill_IronHeart", "iron_heart", "강철 심장", "최대 체력 +50", 95, vitality, 0, 0, 50, 0f, false);
            AddSkill(catalog, "Skill_ManaEcho", "mana_echo", "마력 메아리", "자동 액티브 스킬 피해 +35", 110, arcaneBurst, 0, 35, 0, 0f, false);

            var firstDungeon = catalog.Dungeon;
            firstDungeon.ConfigureIdentityIfEmpty("training_rift", "훈련 균열", "기본 빌드와 시간제한을 익히는 첫 던전", 1, 60, 40);
            EditorUtility.SetDirty(firstDungeon);
            var deepDungeon = GetOrCreateDungeon("Dungeon_Deep", "deep_rift", "심층 균열", "더 빠른 처치와 안정적인 생존을 요구합니다.", 4, 4, 32f, 6, 4, 35, 22, 3, 8, 3, 110, 75);
            var calamityDungeon = GetOrCreateDungeon("Dungeon_Calamity", "calamity_rift", "재앙 균열", "완성된 빌드와 회귀 보너스를 시험하는 고난도 균열입니다.", 8, 5, 28f, 7, 5, 58, 32, 4, 12, 4, 180, 120);
            var voidDungeon = GetOrCreateDungeon("Dungeon_Void", "void_rift", "공허 균열", "모든 성장 축과 전술 지침을 요구하는 최심부 균열입니다.", 12, 6, 26f, 8, 5, 82, 40, 5, 16, 5, 270, 190);
            catalog.SetDungeonsIfEmpty(new List<DungeonDefinition> { firstDungeon, deepDungeon, calamityDungeon });
            catalog.AddDungeonIfMissing(voidDungeon);
            var reinforcedCoat = GetOrCreateEquipment("Equipment_ReinforcedCoat", "reinforced_coat", EquipmentSlot.Armor, "강화 코트", "최대 체력 +30, 방어 +4", 75, 0, 0, 30, 4, 0f, 0f);
            var barrierJacket = GetOrCreateEquipment("Equipment_BarrierJacket", "barrier_jacket", EquipmentSlot.Armor, "방호 재킷", "최대 체력 +10, 방어 +8", 105, 0, 0, 10, 8, 0f, 0f);
            var assaultBoots = GetOrCreateEquipment("Equipment_AssaultBoots", "assault_boots", EquipmentSlot.Boots, "돌격 장화", "기본 공격력 +4, 이동 지연 감소", 95, 4, 0, 0, 0, 0.08f, 0f);
            var vitalityRing = GetOrCreateEquipment("Equipment_VitalityRing", "vitality_ring", EquipmentSlot.Ring, "생명의 반지", "최대 체력 +25, 기본 공격력 +3", 100, 3, 0, 25, 0, 0f, 0f);
            var plasmaBlade = GetOrCreateEquipment("Equipment_PlasmaBlade", "plasma_blade", EquipmentSlot.Weapon, "플라즈마 블레이드", "기본 공격력 +14, 치명타 확률 +6%", 150, 14, 0, 0, 0, 0f, 0.06f);
            var phantomCoat = GetOrCreateEquipment("Equipment_PhantomCoat", "phantom_coat", EquipmentSlot.Armor, "환영 코트", "최대 체력 +35, 방어 +7, 이동 지연 감소", 155, 0, 0, 35, 7, 0.04f, 0f);
            var severanceBoots = GetOrCreateEquipment("Equipment_SeveranceBoots", "severance_boots", EquipmentSlot.Boots, "단절의 장화", "기본 공격력 +6, 이동 지연 감소", 140, 6, 0, 0, 0, 0.12f, 0f);
            var voidRing = GetOrCreateEquipment("Equipment_VoidRing", "void_ring", EquipmentSlot.Ring, "공허의 반지", "자동 액티브 피해 +30, 치명타 확률 +8%", 160, 0, 30, 0, 0, 0f, 0.08f);
            catalog.AddEquipmentIfMissing(reinforcedCoat);
            catalog.AddEquipmentIfMissing(barrierJacket);
            catalog.AddEquipmentIfMissing(assaultBoots);
            catalog.AddEquipmentIfMissing(vitalityRing);
            catalog.AddEquipmentIfMissing(plasmaBlade);
            catalog.AddEquipmentIfMissing(phantomCoat);
            catalog.AddEquipmentIfMissing(severanceBoots);
            catalog.AddEquipmentIfMissing(voidRing);
            EditorUtility.SetDirty(catalog);
            var watcher = GetOrCreateEnemy("Enemy_Watcher", "rift_watcher", "균열 감시자", "균열의 흐름을 읽으며 약한 공격을 빠르게 반복합니다.", 0.85f, 0.8f, 0.85f, 0.9f, 0.9f);
            var devourer = GetOrCreateEnemy("Enemy_Devourer", "mana_devourer", "마력 포식자", "두꺼운 마력 껍질로 피해를 버티는 괴물입니다.", 1.45f, 1.1f, 1.45f, 1.2f, 1.2f);
            var berserker = GetOrCreateEnemy("Enemy_Berserker", "rift_berserker", "균열 광전사", "느리지만 묵직한 공격을 날립니다.", 1.1f, 1.55f, 1.7f, 1.1f, 1.1f);
            var guardian = GetOrCreateEnemy("Enemy_Guardian", "rift_guardian", "균열 수호자", "층의 끝을 지키는 보스입니다. 높은 체력과 보상을 가집니다.", 2.2f, 1.45f, 1.35f, 1.7f, 1.6f);
            watcher.SetCombatTrait(EnemyCombatTrait.Swift);
            devourer.SetCombatTrait(EnemyCombatTrait.Barrier);
            berserker.SetCombatTrait(EnemyCombatTrait.Enrage);
            guardian.SetCombatTrait(EnemyCombatTrait.BarrierEnrage);
            EditorUtility.SetDirty(watcher);
            EditorUtility.SetDirty(devourer);
            EditorUtility.SetDirty(berserker);
            EditorUtility.SetDirty(guardian);
            var encounters = new List<EnemyDefinition> { watcher, devourer, berserker };
            firstDungeon.ConfigureEncounters(new List<EnemyDefinition> { watcher, watcher, devourer }, guardian);
            deepDungeon.ConfigureEncounters(new List<EnemyDefinition> { devourer, berserker, berserker }, guardian);
            calamityDungeon.ConfigureEncounters(new List<EnemyDefinition> { watcher, berserker, guardian }, guardian);
            voidDungeon.ConfigureEncounters(new List<EnemyDefinition> { devourer, guardian, guardian }, guardian);
            EditorUtility.SetDirty(firstDungeon);
            EditorUtility.SetDirty(deepDungeon);
            EditorUtility.SetDirty(calamityDungeon);
            EditorUtility.SetDirty(voidDungeon);

            var equipmentIds = new Dictionary<string, string>
            {
                { "훈련용 검", "training_sword" }, { "마력 지팡이", "mana_staff" }, { "강화 코트", "reinforced_coat" }, { "방호 재킷", "barrier_jacket" }, { "질풍의 장화", "swift_boots" }, { "돌격 장화", "assault_boots" }, { "집중의 반지", "focus_ring" }, { "생명의 반지", "vitality_ring" },
            };
            foreach (var equipment in catalog.Equipment)
            {
                if (!equipmentIds.TryGetValue(equipment.DisplayName, out var id)) continue;
                equipment.SetPrototypeIdIfEmpty(id);
                equipment.EnsureUpgradeDefaults();
                EditorUtility.SetDirty(equipment);
            }

            var entrySet = GetOrCreateEquipmentSet("EquipmentSet_Entry", "entry_set", "균열 입문자", "훈련용 검과 질풍의 장화: 공격력 +4, 이동 지연 -0.05초", new List<EquipmentDefinition> { FindEquipment(catalog, "training_sword"), FindEquipment(catalog, "swift_boots") }, 4, 0, 0, 0, 0.05f, 0f);
            var survivalSet = GetOrCreateEquipmentSet("EquipmentSet_Survival", "survival_set", "생존 전술", "강화 코트와 생명의 반지: 최대 체력 +25, 방어 +3", new List<EquipmentDefinition> { FindEquipment(catalog, "reinforced_coat"), FindEquipment(catalog, "vitality_ring") }, 0, 0, 25, 3, 0f, 0f);
            var arcaneSet = GetOrCreateEquipmentSet("EquipmentSet_Arcane", "arcane_set", "마력 동조", "마력 지팡이와 집중의 반지: 액티브 피해 +15, 치명타 +6%", new List<EquipmentDefinition> { FindEquipment(catalog, "mana_staff"), FindEquipment(catalog, "focus_ring") }, 0, 15, 0, 0, 0f, 0.06f);
            var voidSet = GetOrCreateEquipmentSet("EquipmentSet_Void", "void_set", "공허 절단", "플라즈마 블레이드와 공허의 반지: 공격력 +10, 액티브 피해 +20", new List<EquipmentDefinition> { FindEquipment(catalog, "plasma_blade"), FindEquipment(catalog, "void_ring") }, 10, 20, 0, 0, 0f, 0f);
            catalog.AddEquipmentSetIfMissing(entrySet);
            catalog.AddEquipmentSetIfMissing(survivalSet);
            catalog.AddEquipmentSetIfMissing(arcaneSet);
            catalog.AddEquipmentSetIfMissing(voidSet);
            EditorUtility.SetDirty(catalog);

            foreach (var skill in catalog.SkillNodes)
            {
                if (skill.Id != "execution") continue;

                skill.ReplaceDescriptionIfMatches("다음 노드 확장을 위한 최상위 노드", "적 체력이 25% 이하일 때 즉시 처형");
                EditorUtility.SetDirty(skill);
            }

            AssetDatabase.SaveAssets();
        }

        private static void AssignPrototypeVisuals(PrototypeCatalog catalog)
        {
            var backdrop = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/05. Art/03. Sprites/Background_RiftRooftop_v1.png");
            var hunter = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/05. Art/03. Sprites/Hero_StatusHunter_v1.png");
            var watcher = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/05. Art/03. Sprites/Enemy_RiftWatcher_v1.png");
            var warden = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/05. Art/03. Sprites/Boss_NullWarden_v1.png");
            var equipmentIcons = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/05. Art/03. Sprites/Equipment_IconSheet_v1.png");
            var manaDevourer = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/05. Art/03. Sprites/Enemy_ManaDevourer_v1.png");
            var riftBerserker = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/05. Art/03. Sprites/Enemy_RiftBerserker_v1.png");
            catalog.SetVisualsIfMissing(backdrop, hunter, watcher, warden);
            catalog.SetEquipmentIconSheetIfMissing(equipmentIcons);
            catalog.SetEnemyPortraitsIfMissing(manaDevourer, riftBerserker);
        }

        private static DungeonDefinition GetOrCreateDungeon(string assetName, string id, string displayName, string description, int requiredLevel, int floorCount, float floorTimeLimit, int baseKillTarget, int killTargetPerFloor, int baseEnemyHealth, int enemyHealthPerFloor, int enemyHealthPerKill, int enemyDamageBase, int enemyDamagePerFloor, int clearGoldReward, int clearExperienceReward)
        {
            var assetPath = $"{DataRoot}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<DungeonDefinition>(assetPath);
            if (existing != null) return existing;
            return CreateDungeon(assetName, id, displayName, description, requiredLevel, floorCount, floorTimeLimit, baseKillTarget, killTargetPerFloor, baseEnemyHealth, enemyHealthPerFloor, enemyHealthPerKill, enemyDamageBase, enemyDamagePerFloor, clearGoldReward, clearExperienceReward);
        }

        private static void AddSkill(PrototypeCatalog catalog, string assetName, string id, string displayName, string description, int goldCost, SkillNodeDefinition prerequisite, int damageBonus, int activeDamageBonus, int maxHealthBonus, float moveDelayReduction, bool grantsExecute)
        {
            var skill = GetOrCreateSkill(assetName, id, displayName, description, goldCost, prerequisite, damageBonus, activeDamageBonus, maxHealthBonus, moveDelayReduction, grantsExecute);
            catalog.AddSkillIfMissing(skill);
            EditorUtility.SetDirty(skill);
            EditorUtility.SetDirty(catalog);
        }

        private static SkillNodeDefinition GetOrCreateSkill(string assetName, string id, string displayName, string description, int goldCost, SkillNodeDefinition prerequisite, int damageBonus, int activeDamageBonus, int maxHealthBonus, float moveDelayReduction, bool grantsExecute)
        {
            var assetPath = $"{DataRoot}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<SkillNodeDefinition>(assetPath);
            if (existing != null) return existing;
            return CreateSkill(assetName, id, displayName, description, goldCost, prerequisite, damageBonus, activeDamageBonus, maxHealthBonus, moveDelayReduction, grantsExecute);
        }

        private static SkillNodeDefinition FindSkill(PrototypeCatalog catalog, string id)
        {
            foreach (var skill in catalog.SkillNodes)
            {
                if (skill.Id == id) return skill;
            }

            return null;
        }

        private static EnemyDefinition GetOrCreateEnemy(string assetName, string id, string displayName, string description, float healthMultiplier, float damageMultiplier, float attackInterval, float goldMultiplier, float experienceMultiplier)
        {
            var assetPath = $"{DataRoot}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(assetPath);
            if (existing != null) return existing;
            return CreateEnemy(assetName, id, displayName, description, healthMultiplier, damageMultiplier, attackInterval, goldMultiplier, experienceMultiplier);
        }

        private static LegacyUpgradeDefinition GetOrCreateLegacyUpgrade(string assetName, string id, string displayName, string description, int maximumRank, int shardCostPerRank, float damageBonusPerRank, float goldBonusPerRank, int maxHealthBonusPerRank)
        {
            var assetPath = $"{DataRoot}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<LegacyUpgradeDefinition>(assetPath);
            if (existing != null) return existing;
            return CreateLegacyUpgrade(assetName, id, displayName, description, maximumRank, shardCostPerRank, damageBonusPerRank, goldBonusPerRank, maxHealthBonusPerRank);
        }

        private static DungeonProtocolDefinition GetOrCreateProtocol(string assetName, string id, string displayName, string description, float enemyHealthMultiplier, float enemyDamageMultiplier, float timeLimitMultiplier, float rewardMultiplier)
        {
            var assetPath = $"{DataRoot}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<DungeonProtocolDefinition>(assetPath);
            if (existing != null) return existing;
            return CreateProtocol(assetName, id, displayName, description, enemyHealthMultiplier, enemyDamageMultiplier, timeLimitMultiplier, rewardMultiplier);
        }

        private static CombatDirectiveDefinition GetOrCreateDirective(string assetName, string id, string displayName, string description, float damageMultiplier, float maxHealthMultiplier, float moveDelayMultiplier, float criticalChanceBonus)
        {
            var assetPath = $"{DataRoot}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<CombatDirectiveDefinition>(assetPath);
            if (existing != null) return existing;
            return CreateDirective(assetName, id, displayName, description, damageMultiplier, maxHealthMultiplier, moveDelayMultiplier, criticalChanceBonus);
        }

        private static MilestoneDefinition GetOrCreateMilestone(string assetName, string id, string displayName, string description, MilestoneCondition condition, int targetValue, int goldReward, int statPointReward)
        {
            var assetPath = $"{DataRoot}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<MilestoneDefinition>(assetPath);
            if (existing != null) return existing;
            return CreateMilestone(assetName, id, displayName, description, condition, targetValue, goldReward, statPointReward);
        }

        private static EquipmentDefinition GetOrCreateEquipment(string assetName, string id, EquipmentSlot slot, string displayName, string description, int goldCost, int damageBonus, int activeDamageBonus, int maxHealthBonus, int defenseBonus, float moveDelayReduction, float criticalChanceBonus)
        {
            var assetPath = $"{DataRoot}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<EquipmentDefinition>(assetPath);
            if (existing != null) return existing;
            return CreateEquipment(assetName, id, slot, displayName, description, goldCost, damageBonus, activeDamageBonus, maxHealthBonus, defenseBonus, moveDelayReduction, criticalChanceBonus);
        }

        private static EquipmentSetDefinition GetOrCreateEquipmentSet(string assetName, string id, string displayName, string description, List<EquipmentDefinition> requiredEquipment, int damageBonus, int activeDamageBonus, int maxHealthBonus, int defenseBonus, float moveDelayReduction, float criticalChanceBonus)
        {
            var assetPath = $"{DataRoot}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<EquipmentSetDefinition>(assetPath);
            if (existing != null) return existing;
            return CreateEquipmentSet(assetName, id, displayName, description, requiredEquipment, damageBonus, activeDamageBonus, maxHealthBonus, defenseBonus, moveDelayReduction, criticalChanceBonus);
        }

        private static EquipmentDefinition FindEquipment(PrototypeCatalog catalog, string id)
        {
            foreach (var equipment in catalog.Equipment)
            {
                if (equipment.Id == id) return equipment;
            }
            return null;
        }

        private static void EnsureSceneBootstrap(PrototypeCatalog catalog)
        {
            var scene = SceneManager.GetSceneByPath(PrototypeScenePath);
            var openedForSetup = false;
            if (!scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(PrototypeScenePath, OpenSceneMode.Additive);
                openedForSetup = true;
            }

            PrototypeBootstrap bootstrap = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                bootstrap = root.GetComponent<PrototypeBootstrap>();
                if (bootstrap != null) break;
            }

            if (bootstrap == null)
            {
                var bootstrapObject = new GameObject("StatusWindowPrototypeBootstrap");
                SceneManager.MoveGameObjectToScene(bootstrapObject, scene);
                bootstrap = bootstrapObject.AddComponent<PrototypeBootstrap>();
            }

            var serializedBootstrap = new SerializedObject(bootstrap);
            var catalogProperty = serializedBootstrap.FindProperty("catalog");
            if (catalogProperty.objectReferenceValue != catalog)
            {
                catalogProperty.objectReferenceValue = catalog;
                serializedBootstrap.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.SaveScene(scene);
            }

            if (openedForSetup)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
