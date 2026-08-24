using UnityEngine;

namespace StatusWindow.Progression
{
    public enum EnemyCombatTrait
    {
        None,
        Swift,
        Barrier,
        Enrage,
        BarrierEnrage,
    }

    [CreateAssetMenu(menuName = "StatusWindow/Enemy", fileName = "Enemy_")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [TextArea] [SerializeField] private string description;
        [Header("Combat Modifiers")]
        [Min(0.1f)] [SerializeField] private float healthMultiplier = 1f;
        [Min(0.1f)] [SerializeField] private float damageMultiplier = 1f;
        [Min(0.2f)] [SerializeField] private float attackInterval = 1.25f;
        [Min(0.1f)] [SerializeField] private float goldMultiplier = 1f;
        [Min(0.1f)] [SerializeField] private float experienceMultiplier = 1f;
        [SerializeField] private EnemyCombatTrait combatTrait;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public float HealthMultiplier => healthMultiplier;
        public float DamageMultiplier => damageMultiplier;
        public float AttackInterval => attackInterval;
        public float GoldMultiplier => goldMultiplier;
        public float ExperienceMultiplier => experienceMultiplier;
        public EnemyCombatTrait CombatTrait => combatTrait;

#if UNITY_EDITOR
        public void ConfigurePrototype(string newId, string newDisplayName, string newDescription, float newHealthMultiplier, float newDamageMultiplier, float newAttackInterval, float newGoldMultiplier, float newExperienceMultiplier)
        {
            id = newId;
            displayName = newDisplayName;
            description = newDescription;
            healthMultiplier = newHealthMultiplier;
            damageMultiplier = newDamageMultiplier;
            attackInterval = newAttackInterval;
            goldMultiplier = newGoldMultiplier;
            experienceMultiplier = newExperienceMultiplier;
        }

        public void SetCombatTrait(EnemyCombatTrait newCombatTrait)
        {
            combatTrait = newCombatTrait;
        }
#endif
    }
}
