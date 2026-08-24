using UnityEngine;

namespace StatusWindow.Progression
{
    [CreateAssetMenu(menuName = "StatusWindow/Dungeon Protocol", fileName = "Protocol_")]
    public sealed class DungeonProtocolDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [TextArea] [SerializeField] private string description;
        [Min(0.1f)] [SerializeField] private float enemyHealthMultiplier = 1f;
        [Min(0.1f)] [SerializeField] private float enemyDamageMultiplier = 1f;
        [Min(0.1f)] [SerializeField] private float timeLimitMultiplier = 1f;
        [Min(0.1f)] [SerializeField] private float rewardMultiplier = 1f;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public float EnemyHealthMultiplier => enemyHealthMultiplier;
        public float EnemyDamageMultiplier => enemyDamageMultiplier;
        public float TimeLimitMultiplier => timeLimitMultiplier;
        public float RewardMultiplier => rewardMultiplier;

#if UNITY_EDITOR
        public void ConfigurePrototype(string newId, string newDisplayName, string newDescription, float newEnemyHealthMultiplier, float newEnemyDamageMultiplier, float newTimeLimitMultiplier, float newRewardMultiplier)
        {
            id = newId; displayName = newDisplayName; description = newDescription;
            enemyHealthMultiplier = newEnemyHealthMultiplier; enemyDamageMultiplier = newEnemyDamageMultiplier;
            timeLimitMultiplier = newTimeLimitMultiplier; rewardMultiplier = newRewardMultiplier;
        }
#endif
    }

    [CreateAssetMenu(menuName = "StatusWindow/Combat Directive", fileName = "Directive_")]
    public sealed class CombatDirectiveDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [TextArea] [SerializeField] private string description;
        [Min(0.1f)] [SerializeField] private float damageMultiplier = 1f;
        [Min(0.1f)] [SerializeField] private float maxHealthMultiplier = 1f;
        [Min(0.1f)] [SerializeField] private float moveDelayMultiplier = 1f;
        [Range(0f, 1f)] [SerializeField] private float criticalChanceBonus;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public float DamageMultiplier => damageMultiplier;
        public float MaxHealthMultiplier => maxHealthMultiplier;
        public float MoveDelayMultiplier => moveDelayMultiplier;
        public float CriticalChanceBonus => criticalChanceBonus;

#if UNITY_EDITOR
        public void ConfigurePrototype(string newId, string newDisplayName, string newDescription, float newDamageMultiplier, float newMaxHealthMultiplier, float newMoveDelayMultiplier, float newCriticalChanceBonus)
        {
            id = newId;
            displayName = newDisplayName;
            description = newDescription;
            damageMultiplier = newDamageMultiplier;
            maxHealthMultiplier = newMaxHealthMultiplier;
            moveDelayMultiplier = newMoveDelayMultiplier;
            criticalChanceBonus = newCriticalChanceBonus;
        }
#endif
    }
}
