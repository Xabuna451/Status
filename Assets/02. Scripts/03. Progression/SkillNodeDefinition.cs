using UnityEngine;

namespace StatusWindow.Progression
{
    [CreateAssetMenu(menuName = "StatusWindow/Skill Node", fileName = "SkillNode_")]
    public sealed class SkillNodeDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [TextArea]
        [SerializeField] private string description;
        [Min(0)]
        [SerializeField] private int goldCost;
        [SerializeField] private SkillNodeDefinition prerequisite;
        [Header("Combat Modifiers")]
        [SerializeField] private int damageBonus;
        [SerializeField] private int activeDamageBonus;
        [SerializeField] private int maxHealthBonus;
        [SerializeField] private float moveDelayReduction;
        [SerializeField] private bool grantsExecute;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public int GoldCost => goldCost;
        public SkillNodeDefinition Prerequisite => prerequisite;
        public int DamageBonus => damageBonus;
        public int ActiveDamageBonus => activeDamageBonus;
        public int MaxHealthBonus => maxHealthBonus;
        public float MoveDelayReduction => moveDelayReduction;
        public bool GrantsExecute => grantsExecute;

#if UNITY_EDITOR
        public void ConfigurePrototype(string newId, string newDisplayName, string newDescription, int newGoldCost, SkillNodeDefinition newPrerequisite, int newDamageBonus, int newActiveDamageBonus, int newMaxHealthBonus, float newMoveDelayReduction, bool newGrantsExecute)
        {
            id = newId;
            displayName = newDisplayName;
            description = newDescription;
            goldCost = newGoldCost;
            prerequisite = newPrerequisite;
            damageBonus = newDamageBonus;
            activeDamageBonus = newActiveDamageBonus;
            maxHealthBonus = newMaxHealthBonus;
            moveDelayReduction = newMoveDelayReduction;
            grantsExecute = newGrantsExecute;
        }

        public void ReplaceDescriptionIfMatches(string expectedDescription, string replacementDescription)
        {
            if (description == expectedDescription)
            {
                description = replacementDescription;
            }
        }
#endif
    }
}
