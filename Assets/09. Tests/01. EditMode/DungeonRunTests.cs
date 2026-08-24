using System.Collections.Generic;
using NUnit.Framework;
using StatusWindow.Combat;
using StatusWindow.Progression;
using UnityEngine;

namespace StatusWindow.Tests.EditMode
{
    public sealed class DungeonRunTests
    {
        [Test]
        public void BarrierTrait_CreatesShieldWhenEnemySpawns()
        {
            var progression = ScriptableObject.CreateInstance<ProgressionDefinition>();
            progression.ConfigurePrototype();
            var boss = ScriptableObject.CreateInstance<EnemyDefinition>();
            boss.ConfigurePrototype("boss", "보스", "테스트", 1f, 1f, 1f, 1f, 1f);
            boss.SetCombatTrait(EnemyCombatTrait.Barrier);
            var dungeon = ScriptableObject.CreateInstance<DungeonDefinition>();
            dungeon.ConfigurePrototype("test", "테스트 균열", "테스트", 1, 1, 30f, 1, 1, 100, 0, 0, 1, 0, 0, 0);
            dungeon.SetEncountersIfEmpty(new List<EnemyDefinition>(), boss);
            var catalog = ScriptableObject.CreateInstance<PrototypeCatalog>();
            catalog.ConfigurePrototype(0, 0, 25, progression, new List<SkillNodeDefinition>(), new List<EquipmentDefinition>(), new List<EquipmentSetDefinition>(), new List<LegacyUpgradeDefinition>(), new List<DungeonProtocolDefinition>(), new List<CombatDirectiveDefinition>(), new List<MilestoneDefinition>(), dungeon, new List<DungeonDefinition> { dungeon });

            var run = new DungeonRun(dungeon);
            run.Start(new StatusWindowGameState(catalog));

            Assert.That(run.EnemyBarrier, Is.EqualTo(20));
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(dungeon);
            Object.DestroyImmediate(boss);
            Object.DestroyImmediate(progression);
        }

        [Test]
        public void ReadinessAnalyzer_WhenProjectedTimeExceedsLimit_ReportsCritical()
        {
            var dungeon = ScriptableObject.CreateInstance<DungeonDefinition>();
            dungeon.ConfigurePrototype("test", "테스트 균열", "테스트", 1, 1, 5f, 3, 1, 100, 0, 0, 1, 0, 0, 0);
            var profile = new CombatProfile(1, 1f, 100, 0, 0f, 0, 0f, false);

            var report = new DungeonReadinessAnalyzer().Analyze(dungeon, null, profile);

            Assert.That(report.Readiness, Is.EqualTo(DungeonReadiness.Critical));
            StringAssert.Contains("시간 초과", report.Recommendation);
            Object.DestroyImmediate(dungeon);
        }

        [Test]
        public void ReadinessAnalyzer_WhenBuildHasLargeMargin_ReportsDominant()
        {
            var dungeon = ScriptableObject.CreateInstance<DungeonDefinition>();
            dungeon.ConfigurePrototype("test", "테스트 균열", "테스트", 1, 1, 30f, 1, 1, 10, 0, 0, 1, 0, 0, 0);
            var profile = new CombatProfile(100, 0.5f, 1000, 100, 0f, 0, 0f, false);

            var report = new DungeonReadinessAnalyzer().Analyze(dungeon, null, profile);

            Assert.That(report.Readiness, Is.EqualTo(DungeonReadiness.Dominant));
            Object.DestroyImmediate(dungeon);
        }

        [Test]
        public void CombatDirective_ChangesCombatProfileAfterSelection()
        {
            var progression = ScriptableObject.CreateInstance<ProgressionDefinition>();
            progression.ConfigurePrototype();
            var dungeon = ScriptableObject.CreateInstance<DungeonDefinition>();
            dungeon.ConfigurePrototype("test", "테스트 균열", "테스트", 1, 1, 30f, 2, 1, 10, 0, 0, 1, 0, 0, 0);
            var directive = ScriptableObject.CreateInstance<CombatDirectiveDefinition>();
            directive.ConfigurePrototype("assault", "공세", "테스트", 1.5f, 0.5f, 0.8f, 0.1f);
            var catalog = ScriptableObject.CreateInstance<PrototypeCatalog>();
            catalog.ConfigurePrototype(0, 0, 25, progression, new List<SkillNodeDefinition>(), new List<EquipmentDefinition>(), new List<EquipmentSetDefinition>(), new List<LegacyUpgradeDefinition>(), new List<DungeonProtocolDefinition>(), new List<CombatDirectiveDefinition> { directive }, new List<MilestoneDefinition>(), dungeon, new List<DungeonDefinition> { dungeon });

            var state = new StatusWindowGameState(catalog);
            Assert.That(state.TrySelectCombatDirective(0), Is.True);
            var profile = state.CreateCombatProfile();

            Assert.That(profile.Damage, Is.EqualTo(6));
            Assert.That(profile.MaxHealth, Is.EqualTo(20));
            Assert.That(profile.MoveDelay, Is.EqualTo(0.44f).Within(0.001f));
            Assert.That(profile.CriticalChance, Is.EqualTo(0.1f).Within(0.001f));

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(directive);
            Object.DestroyImmediate(dungeon);
            Object.DestroyImmediate(progression);
        }

        [Test]
        public void DungeonRecord_PersistsFastestClearAcrossSaveLoad()
        {
            var progression = ScriptableObject.CreateInstance<ProgressionDefinition>();
            progression.ConfigurePrototype();
            var dungeon = ScriptableObject.CreateInstance<DungeonDefinition>();
            dungeon.ConfigurePrototype("test", "테스트 균열", "테스트", 1, 1, 30f, 2, 1, 10, 0, 0, 1, 0, 0, 0);
            var catalog = ScriptableObject.CreateInstance<PrototypeCatalog>();
            catalog.ConfigurePrototype(0, 0, 25, progression, new List<SkillNodeDefinition>(), new List<EquipmentDefinition>(), new List<EquipmentSetDefinition>(), new List<LegacyUpgradeDefinition>(), new List<DungeonProtocolDefinition>(), new List<CombatDirectiveDefinition>(), new List<MilestoneDefinition>(), dungeon, new List<DungeonDefinition> { dungeon });

            var state = new StatusWindowGameState(catalog);
            Assert.That(state.TryRecordDungeonBestTime(dungeon, 18f), Is.True);
            Assert.That(state.TryRecordDungeonBestTime(dungeon, 20f), Is.False);
            Assert.That(state.TryRecordDungeonBestTime(dungeon, 15f), Is.True);

            var restoredState = new StatusWindowGameState(catalog);
            Assert.That(restoredState.TryLoad(state.CreateSaveData()), Is.True);
            Assert.That(restoredState.GetDungeonTotalClearCount(dungeon), Is.EqualTo(3));
            Assert.That(restoredState.GetDungeonBestClearSeconds(dungeon), Is.EqualTo(15f).Within(0.001f));

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(dungeon);
            Object.DestroyImmediate(progression);
        }

        [Test]
        public void Tick_TracksElapsedSimulationTime()
        {
            var progression = ScriptableObject.CreateInstance<ProgressionDefinition>();
            progression.ConfigurePrototype();
            var dungeon = ScriptableObject.CreateInstance<DungeonDefinition>();
            dungeon.ConfigurePrototype("test", "테스트 균열", "테스트", 1, 1, 30f, 2, 1, 999, 0, 0, 8, 2, 0, 0);
            var catalog = ScriptableObject.CreateInstance<PrototypeCatalog>();
            catalog.ConfigurePrototype(0, 0, 25, progression, new List<SkillNodeDefinition>(), new List<EquipmentDefinition>(), new List<EquipmentSetDefinition>(), new List<LegacyUpgradeDefinition>(), new List<DungeonProtocolDefinition>(), new List<CombatDirectiveDefinition>(), new List<MilestoneDefinition>(), dungeon, new List<DungeonDefinition> { dungeon });

            var run = new DungeonRun(dungeon);
            run.Start(new StatusWindowGameState(catalog));
            run.Tick(0.5f);

            Assert.That(run.ElapsedTime, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(dungeon);
            Object.DestroyImmediate(progression);
        }

        [Test]
        public void Cancel_WhenAutoBattleIsRunning_StopsWithoutGrantingCombatRewards()
        {
            var progression = ScriptableObject.CreateInstance<ProgressionDefinition>();
            progression.ConfigurePrototype();
            var dungeon = ScriptableObject.CreateInstance<DungeonDefinition>();
            dungeon.ConfigurePrototype("test", "테스트 균열", "테스트", 1, 1, 30f, 2, 1, 999, 0, 0, 8, 2, 0, 0);
            var catalog = ScriptableObject.CreateInstance<PrototypeCatalog>();
            catalog.ConfigurePrototype(0, 0, 25, progression, new List<SkillNodeDefinition>(), new List<EquipmentDefinition>(), new List<EquipmentSetDefinition>(), new List<LegacyUpgradeDefinition>(), new List<DungeonProtocolDefinition>(), new List<CombatDirectiveDefinition>(), new List<MilestoneDefinition>(), dungeon, new List<DungeonDefinition> { dungeon });
            var state = new StatusWindowGameState(catalog);
            var run = new DungeonRun(dungeon);
            run.Start(state);

            Assert.That(run.Cancel(), Is.True);
            Assert.That(run.IsRunning, Is.False);
            Assert.That(run.Result, Is.EqualTo(DungeonResult.Cancelled));
            Assert.That(run.GoldEarned, Is.EqualTo(0));
            Assert.That(run.ExperienceEarned, Is.EqualTo(0));

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(dungeon);
            Object.DestroyImmediate(progression);
        }

        [Test]
        public void ProtocolEnemyDamageMultiplier_IsAppliedExactlyOnce()
        {
            var progression = ScriptableObject.CreateInstance<ProgressionDefinition>();
            progression.ConfigurePrototype();
            var dungeon = ScriptableObject.CreateInstance<DungeonDefinition>();
            dungeon.ConfigurePrototype("test", "테스트 균열", "테스트", 1, 1, 30f, 2, 1, 999, 0, 0, 8, 2, 0, 0);
            var protocol = ScriptableObject.CreateInstance<DungeonProtocolDefinition>();
            protocol.ConfigurePrototype("damage_x2", "위험", "피해 2배", 1f, 2f, 1f, 1f);
            var catalog = ScriptableObject.CreateInstance<PrototypeCatalog>();
            catalog.ConfigurePrototype(0, 0, 25, progression, new List<SkillNodeDefinition>(), new List<EquipmentDefinition>(), new List<EquipmentSetDefinition>(), new List<LegacyUpgradeDefinition>(), new List<DungeonProtocolDefinition> { protocol }, new List<CombatDirectiveDefinition>(), new List<MilestoneDefinition>(), dungeon, new List<DungeonDefinition> { dungeon });

            var state = new StatusWindowGameState(catalog);
            var run = new DungeonRun(dungeon, protocol);
            run.Start(state);
            run.Tick(0.8f);

            Assert.That(run.PlayerHealth, Is.EqualTo(20));

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(protocol);
            Object.DestroyImmediate(dungeon);
            Object.DestroyImmediate(progression);
        }

        [Test]
        public void ProgressionGoalAdvisor_PrioritizesStatPointsBeforeAFutureDungeonUnlock()
        {
            var progression = ScriptableObject.CreateInstance<ProgressionDefinition>();
            progression.ConfigurePrototype();
            var currentDungeon = ScriptableObject.CreateInstance<DungeonDefinition>();
            currentDungeon.ConfigurePrototype("current", "현재 균열", "테스트", 1, 1, 30f, 1, 1, 10, 0, 0, 1, 0, 0, 0);
            var nextDungeon = ScriptableObject.CreateInstance<DungeonDefinition>();
            nextDungeon.ConfigurePrototype("next", "다음 균열", "테스트", 4, 1, 30f, 1, 1, 10, 0, 0, 1, 0, 0, 0);
            var catalog = ScriptableObject.CreateInstance<PrototypeCatalog>();
            catalog.ConfigurePrototype(0, 2, 25, progression, new List<SkillNodeDefinition>(), new List<EquipmentDefinition>(), new List<EquipmentSetDefinition>(), new List<LegacyUpgradeDefinition>(), new List<DungeonProtocolDefinition>(), new List<CombatDirectiveDefinition>(), new List<MilestoneDefinition>(), currentDungeon, new List<DungeonDefinition> { currentDungeon, nextDungeon });

            var goal = new ProgressionGoalAdvisor().Create(new StatusWindowGameState(catalog));

            Assert.That(goal.Kind, Is.EqualTo(ProgressionGoalKind.SpendStatPoints));
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(currentDungeon);
            Object.DestroyImmediate(nextDungeon);
            Object.DestroyImmediate(progression);
        }
    }
}
