using System.Collections.Generic;
using DateTime = System.DateTime;
using NUnit.Framework;
using StatusWindow.Progression;
using UnityEngine;

namespace StatusWindow.Tests.EditMode
{
    public sealed class SkillLoadoutTests
    {
        [Test]
        public void FifthUnlockedSkill_RequiresReplacingAnEquippedSkill()
        {
            var progression = ScriptableObject.CreateInstance<ProgressionDefinition>();
            progression.ConfigurePrototype();
            var skills = new List<SkillNodeDefinition>();
            for (var index = 0; index < 5; index++)
            {
                var skill = ScriptableObject.CreateInstance<SkillNodeDefinition>();
                skill.ConfigurePrototype($"skill_{index}", $"스킬 {index}", "테스트", 0, null, 0, 0, 0, 0f, false);
                skills.Add(skill);
            }

            var catalog = ScriptableObject.CreateInstance<PrototypeCatalog>();
            catalog.ConfigurePrototype(0, 0, 25, progression, skills, new List<EquipmentDefinition>(), new List<EquipmentSetDefinition>(), new List<LegacyUpgradeDefinition>(), new List<DungeonProtocolDefinition>(), new List<CombatDirectiveDefinition>(), new List<MilestoneDefinition>(), null, new List<DungeonDefinition>());
            var state = new StatusWindowGameState(catalog);
            foreach (var skill in skills) Assert.That(state.TryUnlockSkill(skill), Is.True);

            Assert.That(state.EquippedSkillCount, Is.EqualTo(4));
            Assert.That(state.IsSkillEquipped(skills[4]), Is.False);
            Assert.That(state.TryToggleSkill(skills[0]), Is.True);
            Assert.That(state.TryToggleSkill(skills[4]), Is.True);
            Assert.That(state.IsSkillEquipped(skills[4]), Is.True);

            Object.DestroyImmediate(catalog);
            foreach (var skill in skills) Object.DestroyImmediate(skill);
            Object.DestroyImmediate(progression);
        }
    }

    public sealed class DailyContractTests
    {
        [Test]
        public void CompletedContract_CanBeClaimedOnlyOnceAndSurvivesSaveLoad()
        {
            var catalog = CreateDailyContractCatalog();
            var state = new StatusWindowGameState(catalog);
            state.RefreshDailyContracts(DateTime.UtcNow);
            state.GainCombatReward(StatusWindowGameState.DailyCombatGoldTarget, 0);

            Assert.That(state.IsDailyContractComplete(DailyContractType.CombatGold), Is.True);
            Assert.That(state.TryClaimDailyContract(DailyContractType.CombatGold), Is.True);
            Assert.That(state.TryClaimDailyContract(DailyContractType.CombatGold), Is.False);

            var restoredState = new StatusWindowGameState(catalog);
            Assert.That(restoredState.TryLoad(state.CreateSaveData()), Is.True);
            Assert.That(restoredState.HasClaimedDailyContract(DailyContractType.CombatGold), Is.True);

            Object.DestroyImmediate(catalog.Progression);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void NewUtcDay_ResetsProgressAndClaimState()
        {
            var catalog = CreateDailyContractCatalog();
            var state = new StatusWindowGameState(catalog);
            var today = DateTime.UtcNow.Date;
            state.RefreshDailyContracts(today);
            state.RecordDungeonClear(null);
            state.RecordDungeonClear(null);
            Assert.That(state.IsDailyContractComplete(DailyContractType.RiftClear), Is.True);
            Assert.That(state.TryClaimDailyContract(DailyContractType.RiftClear), Is.True);

            Assert.That(state.RefreshDailyContracts(today.AddDays(1)), Is.True);
            Assert.That(state.GetDailyContractProgress(DailyContractType.RiftClear), Is.EqualTo(0));
            Assert.That(state.HasClaimedDailyContract(DailyContractType.RiftClear), Is.False);

            Object.DestroyImmediate(catalog.Progression);
            Object.DestroyImmediate(catalog);
        }

        private static PrototypeCatalog CreateDailyContractCatalog()
        {
            var progression = ScriptableObject.CreateInstance<ProgressionDefinition>();
            progression.ConfigurePrototype();
            var catalog = ScriptableObject.CreateInstance<PrototypeCatalog>();
            catalog.ConfigurePrototype(0, 0, 25, progression, new List<SkillNodeDefinition>(), new List<EquipmentDefinition>(), new List<EquipmentSetDefinition>(), new List<LegacyUpgradeDefinition>(), new List<DungeonProtocolDefinition>(), new List<CombatDirectiveDefinition>(), new List<MilestoneDefinition>(), null, new List<DungeonDefinition>());
            return catalog;
        }
    }
}
