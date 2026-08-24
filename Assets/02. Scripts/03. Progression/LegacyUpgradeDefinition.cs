using UnityEngine;

namespace StatusWindow.Progression
{
    [CreateAssetMenu(menuName = "StatusWindow/Legacy Upgrade", fileName = "LegacyUpgrade_")]
    public sealed class LegacyUpgradeDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [TextArea] [SerializeField] private string description;
        [Min(1)] [SerializeField] private int maximumRank = 3;
        [Min(1)] [SerializeField] private int shardCostPerRank = 1;
        [SerializeField] private float damageBonusPerRank;
        [SerializeField] private float goldBonusPerRank;
        [SerializeField] private int maxHealthBonusPerRank;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public int MaximumRank => maximumRank;
        public int ShardCostPerRank => shardCostPerRank;
        public float DamageBonusPerRank => damageBonusPerRank;
        public float GoldBonusPerRank => goldBonusPerRank;
        public int MaxHealthBonusPerRank => maxHealthBonusPerRank;

#if UNITY_EDITOR
        public void ConfigurePrototype(string newId, string newDisplayName, string newDescription, int newMaximumRank, int newShardCostPerRank, float newDamageBonusPerRank, float newGoldBonusPerRank, int newMaxHealthBonusPerRank)
        {
            id = newId;
            displayName = newDisplayName;
            description = newDescription;
            maximumRank = newMaximumRank;
            shardCostPerRank = newShardCostPerRank;
            damageBonusPerRank = newDamageBonusPerRank;
            goldBonusPerRank = newGoldBonusPerRank;
            maxHealthBonusPerRank = newMaxHealthBonusPerRank;
        }
#endif
    }
}
