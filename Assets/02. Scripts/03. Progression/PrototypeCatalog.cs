using System.Collections.Generic;
using UnityEngine;

namespace StatusWindow.Progression
{
    [CreateAssetMenu(menuName = "StatusWindow/Prototype Catalog", fileName = "StatusWindowPrototypeCatalog")]
    public sealed class PrototypeCatalog : ScriptableObject
    {
        [Header("Starting State")]
        [Min(0)]
        [SerializeField] private int startingGold = 80;
        [Min(0)]
        [SerializeField] private int startingStatPoints = 5;
        [Min(1)]
        [SerializeField] private int statPointGoldCost = 25;
        [SerializeField] private ProgressionDefinition progression;
        [Header("Content")]
        [SerializeField] private List<SkillNodeDefinition> skillNodes = new List<SkillNodeDefinition>();
        [SerializeField] private List<EquipmentDefinition> equipment = new List<EquipmentDefinition>();
        [SerializeField] private List<EquipmentSetDefinition> equipmentSets = new List<EquipmentSetDefinition>();
        [SerializeField] private List<LegacyUpgradeDefinition> legacyUpgrades = new List<LegacyUpgradeDefinition>();
        [SerializeField] private List<DungeonProtocolDefinition> dungeonProtocols = new List<DungeonProtocolDefinition>();
        [SerializeField] private List<CombatDirectiveDefinition> combatDirectives = new List<CombatDirectiveDefinition>();
        [SerializeField] private List<MilestoneDefinition> milestones = new List<MilestoneDefinition>();
        [SerializeField] private DungeonDefinition dungeon;
        [SerializeField] private List<DungeonDefinition> dungeons = new List<DungeonDefinition>();
        [Header("Prototype Visuals")]
        [SerializeField] private Texture2D dungeonBackdrop;
        [SerializeField] private Texture2D hunterPortrait;
        [SerializeField] private Texture2D riftWatcherPortrait;
        [SerializeField] private Texture2D nullWardenPortrait;
        [SerializeField] private Texture2D equipmentIconSheet;
        [SerializeField] private Texture2D manaDevourerPortrait;
        [SerializeField] private Texture2D riftBerserkerPortrait;

        public int StartingGold => startingGold;
        public int StartingStatPoints => startingStatPoints;
        public int StatPointGoldCost => statPointGoldCost;
        public ProgressionDefinition Progression => progression;
        public IReadOnlyList<SkillNodeDefinition> SkillNodes => skillNodes;
        public IReadOnlyList<EquipmentDefinition> Equipment => equipment;
        public IReadOnlyList<EquipmentSetDefinition> EquipmentSets => equipmentSets;
        public IReadOnlyList<LegacyUpgradeDefinition> LegacyUpgrades => legacyUpgrades;
        public IReadOnlyList<DungeonProtocolDefinition> DungeonProtocols => dungeonProtocols;
        public IReadOnlyList<CombatDirectiveDefinition> CombatDirectives => combatDirectives;
        public IReadOnlyList<MilestoneDefinition> Milestones => milestones;
        public DungeonDefinition Dungeon => dungeon;
        public IReadOnlyList<DungeonDefinition> Dungeons => dungeons;
        public Texture2D DungeonBackdrop => dungeonBackdrop;
        public Texture2D HunterPortrait => hunterPortrait;
        public Texture2D RiftWatcherPortrait => riftWatcherPortrait;
        public Texture2D NullWardenPortrait => nullWardenPortrait;
        public Texture2D EquipmentIconSheet => equipmentIconSheet;
        public Texture2D ManaDevourerPortrait => manaDevourerPortrait;
        public Texture2D RiftBerserkerPortrait => riftBerserkerPortrait;

#if UNITY_EDITOR
        public void ConfigurePrototype(int newStartingGold, int newStartingStatPoints, int newStatPointGoldCost, ProgressionDefinition newProgression, List<SkillNodeDefinition> newSkillNodes, List<EquipmentDefinition> newEquipment, List<EquipmentSetDefinition> newEquipmentSets, List<LegacyUpgradeDefinition> newLegacyUpgrades, List<DungeonProtocolDefinition> newDungeonProtocols, List<CombatDirectiveDefinition> newCombatDirectives, List<MilestoneDefinition> newMilestones, DungeonDefinition newDungeon, List<DungeonDefinition> newDungeons)
        {
            startingGold = newStartingGold;
            startingStatPoints = newStartingStatPoints;
            statPointGoldCost = newStatPointGoldCost;
            progression = newProgression;
            skillNodes = newSkillNodes;
            equipment = newEquipment;
            equipmentSets = newEquipmentSets;
            legacyUpgrades = newLegacyUpgrades;
            dungeonProtocols = newDungeonProtocols;
            combatDirectives = newCombatDirectives;
            milestones = newMilestones;
            dungeon = newDungeon;
            dungeons = newDungeons;
        }

        public void SetProgressionIfMissing(ProgressionDefinition newProgression)
        {
            if (progression == null) progression = newProgression;
        }

        public void SetDungeonsIfEmpty(List<DungeonDefinition> newDungeons)
        {
            if (dungeons == null || dungeons.Count == 0) dungeons = newDungeons;
        }

        public void AddDungeonIfMissing(DungeonDefinition newDungeon)
        {
            if (newDungeon == null) return;
            if (dungeons == null) dungeons = new List<DungeonDefinition>();
            if (!dungeons.Contains(newDungeon)) dungeons.Add(newDungeon);
        }

        public void AddEquipmentIfMissing(EquipmentDefinition newEquipment)
        {
            if (newEquipment == null) return;
            if (equipment == null) equipment = new List<EquipmentDefinition>();
            if (!equipment.Contains(newEquipment)) equipment.Add(newEquipment);
        }

        public void AddEquipmentSetIfMissing(EquipmentSetDefinition newEquipmentSet)
        {
            if (newEquipmentSet == null) return;
            if (equipmentSets == null) equipmentSets = new List<EquipmentSetDefinition>();
            if (!equipmentSets.Contains(newEquipmentSet)) equipmentSets.Add(newEquipmentSet);
        }

        public void AddSkillIfMissing(SkillNodeDefinition newSkill)
        {
            if (newSkill == null) return;
            if (skillNodes == null) skillNodes = new List<SkillNodeDefinition>();
            if (!skillNodes.Contains(newSkill)) skillNodes.Add(newSkill);
        }

        public void SetLegacyUpgradesIfEmpty(List<LegacyUpgradeDefinition> newLegacyUpgrades)
        {
            if (legacyUpgrades == null || legacyUpgrades.Count == 0) legacyUpgrades = newLegacyUpgrades;
        }

        public void SetDungeonProtocolsIfEmpty(List<DungeonProtocolDefinition> newDungeonProtocols)
        {
            if (dungeonProtocols == null || dungeonProtocols.Count == 0) dungeonProtocols = newDungeonProtocols;
        }

        public void AddDungeonProtocolIfMissing(DungeonProtocolDefinition newProtocol)
        {
            if (newProtocol == null) return;
            if (dungeonProtocols == null) dungeonProtocols = new List<DungeonProtocolDefinition>();
            if (!dungeonProtocols.Contains(newProtocol)) dungeonProtocols.Add(newProtocol);
        }

        public void AddCombatDirectiveIfMissing(CombatDirectiveDefinition newDirective)
        {
            if (newDirective == null) return;
            if (combatDirectives == null) combatDirectives = new List<CombatDirectiveDefinition>();
            if (!combatDirectives.Contains(newDirective)) combatDirectives.Add(newDirective);
        }

        public void SetMilestonesIfEmpty(List<MilestoneDefinition> newMilestones)
        {
            if (milestones == null || milestones.Count == 0) milestones = newMilestones;
        }

        public void AddMilestoneIfMissing(MilestoneDefinition newMilestone)
        {
            if (newMilestone == null) return;
            if (milestones == null) milestones = new List<MilestoneDefinition>();
            if (!milestones.Contains(newMilestone)) milestones.Add(newMilestone);
        }

        public void SetVisualsIfMissing(Texture2D newDungeonBackdrop, Texture2D newHunterPortrait, Texture2D newRiftWatcherPortrait, Texture2D newNullWardenPortrait)
        {
            if (dungeonBackdrop == null) dungeonBackdrop = newDungeonBackdrop;
            if (hunterPortrait == null) hunterPortrait = newHunterPortrait;
            if (riftWatcherPortrait == null) riftWatcherPortrait = newRiftWatcherPortrait;
            if (nullWardenPortrait == null) nullWardenPortrait = newNullWardenPortrait;
        }

        public void SetEquipmentIconSheetIfMissing(Texture2D newEquipmentIconSheet)
        {
            if (equipmentIconSheet == null) equipmentIconSheet = newEquipmentIconSheet;
        }

        public void SetEnemyPortraitsIfMissing(Texture2D newManaDevourerPortrait, Texture2D newRiftBerserkerPortrait)
        {
            if (manaDevourerPortrait == null) manaDevourerPortrait = newManaDevourerPortrait;
            if (riftBerserkerPortrait == null) riftBerserkerPortrait = newRiftBerserkerPortrait;
        }
#endif
    }
}
