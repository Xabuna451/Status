using UnityEngine;

namespace StatusWindow.Progression
{
    [CreateAssetMenu(menuName = "StatusWindow/Progression", fileName = "Progression_")]
    public sealed class ProgressionDefinition : ScriptableObject
    {
        [Header("Leveling")]
        [Min(1)] [SerializeField] private int baseExperienceToLevel = 20;
        [Min(0)] [SerializeField] private int experienceIncreasePerLevel = 10;
        [Min(0)] [SerializeField] private int statPointsPerLevel = 3;

        [Header("Combat Base")]
        [Min(0)] [SerializeField] private int baseDamage = 4;
        [Min(0)] [SerializeField] private int damagePerStrength = 2;
        [Min(0.01f)] [SerializeField] private float baseAttackInterval = 1.1f;
        [Min(0f)] [SerializeField] private float attackIntervalReductionPerAgility = 0.05f;
        [Min(0.01f)] [SerializeField] private float minimumAttackInterval = 0.25f;
        [Min(1)] [SerializeField] private int baseHealth = 40;
        [Min(0)] [SerializeField] private int healthPerWill = 10;
        [Min(0.01f)] [SerializeField] private float baseMoveDelay = 0.55f;
        [Min(0f)] [SerializeField] private float moveDelayReductionPerAgility = 0.02f;
        [Min(0.01f)] [SerializeField] private float minimumMoveDelay = 0.05f;
        [Min(0)] [SerializeField] private int activeDamagePerMagic = 4;
        [Min(0f)] [SerializeField] private float criticalChancePerSense = 0.02f;
        [Range(0f, 1f)] [SerializeField] private float maximumCriticalChance = 0.75f;
        [Min(1)] [SerializeField] private int maximumEquippedSkillCount = 4;

        [Header("Rebirth")]
        [Min(1)] [SerializeField] private int rebirthRequiredLevel = 8;
        [Min(1)] [SerializeField] private int rebirthRequiredClears = 1;
        [Min(1)] [SerializeField] private int rebirthShardReward = 1;
        [Min(0f)] [SerializeField] private float damageBonusPerShard = 0.1f;
        [Min(0f)] [SerializeField] private float goldBonusPerShard = 0.15f;

        public int BaseExperienceToLevel => baseExperienceToLevel;
        public int ExperienceIncreasePerLevel => experienceIncreasePerLevel;
        public int StatPointsPerLevel => statPointsPerLevel;
        public int BaseDamage => baseDamage;
        public int DamagePerStrength => damagePerStrength;
        public float BaseAttackInterval => baseAttackInterval;
        public float AttackIntervalReductionPerAgility => attackIntervalReductionPerAgility;
        public float MinimumAttackInterval => minimumAttackInterval;
        public int BaseHealth => baseHealth;
        public int HealthPerWill => healthPerWill;
        public float BaseMoveDelay => baseMoveDelay;
        public float MoveDelayReductionPerAgility => moveDelayReductionPerAgility;
        public float MinimumMoveDelay => minimumMoveDelay;
        public int ActiveDamagePerMagic => activeDamagePerMagic;
        public float CriticalChancePerSense => criticalChancePerSense;
        public float MaximumCriticalChance => maximumCriticalChance <= 0f ? 0.75f : maximumCriticalChance;
        public int MaximumEquippedSkillCount => Mathf.Max(1, maximumEquippedSkillCount);
        public int RebirthRequiredLevel => rebirthRequiredLevel;
        public int RebirthRequiredClears => rebirthRequiredClears;
        public int RebirthShardReward => rebirthShardReward;
        public float DamageBonusPerShard => damageBonusPerShard;
        public float GoldBonusPerShard => goldBonusPerShard;

#if UNITY_EDITOR
        public void ConfigurePrototype()
        {
            baseExperienceToLevel = 20; experienceIncreasePerLevel = 10; statPointsPerLevel = 3;
            baseDamage = 4; damagePerStrength = 2; baseAttackInterval = 1.1f; attackIntervalReductionPerAgility = 0.05f; minimumAttackInterval = 0.25f;
            baseHealth = 40; healthPerWill = 10; baseMoveDelay = 0.55f; moveDelayReductionPerAgility = 0.02f; minimumMoveDelay = 0.05f;
            activeDamagePerMagic = 4; criticalChancePerSense = 0.02f; maximumCriticalChance = 0.75f; maximumEquippedSkillCount = 4;
            rebirthRequiredLevel = 8; rebirthRequiredClears = 1; rebirthShardReward = 1; damageBonusPerShard = 0.1f; goldBonusPerShard = 0.15f;
        }

        public void EnsureCombatDefaults()
        {
            if (maximumCriticalChance <= 0f) maximumCriticalChance = 0.75f;
            if (maximumEquippedSkillCount <= 0) maximumEquippedSkillCount = 4;
        }
#endif
    }
}
