using System.Collections.Generic;
using UnityEngine;

namespace StatusWindow.Progression
{
    public enum EquipmentSlot
    {
        Weapon,
        Armor,
        Boots,
        Ring,
    }

    [CreateAssetMenu(menuName = "StatusWindow/Equipment", fileName = "Equipment_")]
    public sealed class EquipmentDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private EquipmentSlot slot;
        [SerializeField] private string displayName;
        [TextArea]
        [SerializeField] private string description;
        [Min(0)]
        [SerializeField] private int goldCost;
        [Header("Combat Modifiers")]
        [SerializeField] private int damageBonus;
        [SerializeField] private int activeDamageBonus;
        [SerializeField] private int maxHealthBonus;
        [SerializeField] private int defenseBonus;
        [SerializeField] private float moveDelayReduction;
        [Range(0f, 1f)]
        [SerializeField] private float criticalChanceBonus;
        [Header("Upgrade")]
        [Min(1)] [SerializeField] private int maximumUpgradeLevel = 5;
        [Min(1)] [SerializeField] private int upgradeBaseGoldCost = 25;
        [Range(0.01f, 1f)] [SerializeField] private float bonusIncreasePerLevel = 0.15f;

        public string Id => id;
        public EquipmentSlot Slot => slot;
        public string DisplayName => displayName;
        public string Description => description;
        public int GoldCost => goldCost;
        public int DamageBonus => damageBonus;
        public int ActiveDamageBonus => activeDamageBonus;
        public int MaxHealthBonus => maxHealthBonus;
        public int DefenseBonus => defenseBonus;
        public float MoveDelayReduction => moveDelayReduction;
        public float CriticalChanceBonus => criticalChanceBonus;
        public int MaximumUpgradeLevel => Mathf.Max(1, maximumUpgradeLevel);

        public int GetUpgradeGoldCost(int currentLevel)
        {
            return Mathf.Max(1, upgradeBaseGoldCost) * (currentLevel + 1);
        }

        public float GetUpgradeMultiplier(int level)
        {
            return 1f + level * Mathf.Max(0.01f, bonusIncreasePerLevel);
        }

#if UNITY_EDITOR
        public void ConfigurePrototype(string newId, EquipmentSlot newSlot, string newDisplayName, string newDescription, int newGoldCost, int newDamageBonus, int newActiveDamageBonus, int newMaxHealthBonus, int newDefenseBonus, float newMoveDelayReduction, float newCriticalChanceBonus)
        {
            id = newId;
            slot = newSlot;
            displayName = newDisplayName;
            description = newDescription;
            goldCost = newGoldCost;
            damageBonus = newDamageBonus;
            activeDamageBonus = newActiveDamageBonus;
            maxHealthBonus = newMaxHealthBonus;
            defenseBonus = newDefenseBonus;
            moveDelayReduction = newMoveDelayReduction;
            criticalChanceBonus = newCriticalChanceBonus;
        }

        public void SetPrototypeIdIfEmpty(string newId)
        {
            if (string.IsNullOrEmpty(id)) id = newId;
        }

        public void EnsureUpgradeDefaults()
        {
            if (maximumUpgradeLevel <= 0) maximumUpgradeLevel = 5;
            if (upgradeBaseGoldCost <= 0) upgradeBaseGoldCost = 25;
            if (bonusIncreasePerLevel <= 0f) bonusIncreasePerLevel = 0.15f;
        }
#endif
    }

    [CreateAssetMenu(menuName = "StatusWindow/Equipment Set", fileName = "EquipmentSet_")]
    public sealed class EquipmentSetDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [TextArea] [SerializeField] private string description;
        [SerializeField] private List<EquipmentDefinition> requiredEquipment = new List<EquipmentDefinition>();
        [Header("Combat Modifiers")]
        [SerializeField] private int damageBonus;
        [SerializeField] private int activeDamageBonus;
        [SerializeField] private int maxHealthBonus;
        [SerializeField] private int defenseBonus;
        [SerializeField] private float moveDelayReduction;
        [Range(0f, 1f)] [SerializeField] private float criticalChanceBonus;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public IReadOnlyList<EquipmentDefinition> RequiredEquipment => requiredEquipment;
        public int DamageBonus => damageBonus;
        public int ActiveDamageBonus => activeDamageBonus;
        public int MaxHealthBonus => maxHealthBonus;
        public int DefenseBonus => defenseBonus;
        public float MoveDelayReduction => moveDelayReduction;
        public float CriticalChanceBonus => criticalChanceBonus;

#if UNITY_EDITOR
        public void ConfigurePrototype(string newId, string newDisplayName, string newDescription, List<EquipmentDefinition> newRequiredEquipment, int newDamageBonus, int newActiveDamageBonus, int newMaxHealthBonus, int newDefenseBonus, float newMoveDelayReduction, float newCriticalChanceBonus)
        {
            id = newId;
            displayName = newDisplayName;
            description = newDescription;
            requiredEquipment = newRequiredEquipment;
            damageBonus = newDamageBonus;
            activeDamageBonus = newActiveDamageBonus;
            maxHealthBonus = newMaxHealthBonus;
            defenseBonus = newDefenseBonus;
            moveDelayReduction = newMoveDelayReduction;
            criticalChanceBonus = newCriticalChanceBonus;
        }
#endif
    }
}
