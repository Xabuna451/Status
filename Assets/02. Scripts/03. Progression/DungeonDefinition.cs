using System.Collections.Generic;
using UnityEngine;

namespace StatusWindow.Progression
{
    [CreateAssetMenu(menuName = "StatusWindow/Dungeon", fileName = "Dungeon_")]
    public sealed class DungeonDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [TextArea] [SerializeField] private string description;
        [Min(1)] [SerializeField] private int requiredLevel = 1;
        [Min(1)]
        [SerializeField] private int floorCount = 3;
        [Min(1f)]
        [SerializeField] private float floorTimeLimit = 35f;
        [Min(1)]
        [SerializeField] private int baseKillTarget = 5;
        [Min(1)]
        [SerializeField] private int killTargetPerFloor = 3;
        [Min(1)]
        [SerializeField] private int baseEnemyHealth = 18;
        [Min(0)]
        [SerializeField] private int enemyHealthPerFloor = 14;
        [Min(0)]
        [SerializeField] private int enemyHealthPerKill = 2;
        [Min(0)]
        [SerializeField] private int enemyDamageBase = 5;
        [Min(0)]
        [SerializeField] private int enemyDamagePerFloor = 2;
        [Header("Reward")]
        [Min(0)] [SerializeField] private int clearGoldReward = 60;
        [Min(0)] [SerializeField] private int clearExperienceReward = 40;
        [Header("Mastery")]
        [Min(1)] [SerializeField] private int maximumMasteryRank = 10;
        [Min(0f)] [SerializeField] private float damageBonusPerMasteryRank = 0.01f;
        [Header("Encounters")]
        [SerializeField] private List<EnemyDefinition> enemies = new List<EnemyDefinition>();
        [SerializeField] private EnemyDefinition floorBoss;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public int RequiredLevel => requiredLevel;
        public int FloorCount => floorCount;
        public float FloorTimeLimit => floorTimeLimit;
        public int BaseKillTarget => baseKillTarget;
        public int KillTargetPerFloor => killTargetPerFloor;
        public int BaseEnemyHealth => baseEnemyHealth;
        public int EnemyHealthPerFloor => enemyHealthPerFloor;
        public int EnemyHealthPerKill => enemyHealthPerKill;
        public int EnemyDamageBase => enemyDamageBase;
        public int EnemyDamagePerFloor => enemyDamagePerFloor;
        public int ClearGoldReward => clearGoldReward;
        public int ClearExperienceReward => clearExperienceReward;
        public int MaximumMasteryRank => Mathf.Max(1, maximumMasteryRank);
        public float DamageBonusPerMasteryRank => damageBonusPerMasteryRank <= 0f ? 0.01f : damageBonusPerMasteryRank;
        public IReadOnlyList<EnemyDefinition> Enemies => enemies;
        public EnemyDefinition FloorBoss => floorBoss;

#if UNITY_EDITOR
        public void ConfigurePrototype(string newId, string newDisplayName, string newDescription, int newRequiredLevel, int newFloorCount, float newFloorTimeLimit, int newBaseKillTarget, int newKillTargetPerFloor, int newBaseEnemyHealth, int newEnemyHealthPerFloor, int newEnemyHealthPerKill, int newEnemyDamageBase, int newEnemyDamagePerFloor, int newClearGoldReward, int newClearExperienceReward)
        {
            id = newId;
            displayName = newDisplayName;
            description = newDescription;
            requiredLevel = newRequiredLevel;
            floorCount = newFloorCount;
            floorTimeLimit = newFloorTimeLimit;
            baseKillTarget = newBaseKillTarget;
            killTargetPerFloor = newKillTargetPerFloor;
            baseEnemyHealth = newBaseEnemyHealth;
            enemyHealthPerFloor = newEnemyHealthPerFloor;
            enemyHealthPerKill = newEnemyHealthPerKill;
            enemyDamageBase = newEnemyDamageBase;
            enemyDamagePerFloor = newEnemyDamagePerFloor;
            clearGoldReward = newClearGoldReward;
            clearExperienceReward = newClearExperienceReward;
        }

        public void ConfigureIdentityIfEmpty(string newId, string newDisplayName, string newDescription, int newRequiredLevel, int newClearGoldReward, int newClearExperienceReward)
        {
            if (string.IsNullOrEmpty(id)) id = newId;
            if (string.IsNullOrEmpty(displayName)) displayName = newDisplayName;
            if (string.IsNullOrEmpty(description)) description = newDescription;
            requiredLevel = Mathf.Max(1, newRequiredLevel);
            clearGoldReward = newClearGoldReward;
            clearExperienceReward = newClearExperienceReward;
        }

        public void SetEncountersIfEmpty(List<EnemyDefinition> newEnemies, EnemyDefinition newFloorBoss)
        {
            if (enemies == null || enemies.Count == 0) enemies = newEnemies;
            if (floorBoss == null) floorBoss = newFloorBoss;
        }

        public void ConfigureEncounters(List<EnemyDefinition> newEnemies, EnemyDefinition newFloorBoss)
        {
            enemies = newEnemies ?? new List<EnemyDefinition>();
            floorBoss = newFloorBoss;
        }
#endif
    }
}
