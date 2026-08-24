using UnityEngine;

namespace StatusWindow.Progression
{
    public enum MilestoneCondition
    {
        Level,
        DungeonClears,
        Rebirths,
    }

    [CreateAssetMenu(menuName = "StatusWindow/Milestone", fileName = "Milestone_")]
    public sealed class MilestoneDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [TextArea] [SerializeField] private string description;
        [SerializeField] private MilestoneCondition condition;
        [Min(1)] [SerializeField] private int targetValue = 1;
        [Min(0)] [SerializeField] private int goldReward;
        [Min(0)] [SerializeField] private int statPointReward;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public MilestoneCondition Condition => condition;
        public int TargetValue => targetValue;
        public int GoldReward => goldReward;
        public int StatPointReward => statPointReward;

#if UNITY_EDITOR
        public void ConfigurePrototype(string newId, string newDisplayName, string newDescription, MilestoneCondition newCondition, int newTargetValue, int newGoldReward, int newStatPointReward)
        {
            id = newId; displayName = newDisplayName; description = newDescription;
            condition = newCondition; targetValue = newTargetValue; goldReward = newGoldReward; statPointReward = newStatPointReward;
        }
#endif
    }
}
