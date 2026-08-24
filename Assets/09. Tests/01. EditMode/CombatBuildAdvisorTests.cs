using NUnit.Framework;
using StatusWindow.Combat;

namespace StatusWindow.Tests.EditMode
{
    public sealed class CombatBuildAdvisorTests
    {
        [Test]
        public void DefeatedWithLowDefense_RecommendsDefenseInvestment()
        {
            var advisor = new CombatBuildAdvisor();
            var profile = new CombatProfile(10, 1f, 30, 1, 0.2f, 0, 0f, false);

            var recommendation = advisor.CreateRecommendation(DungeonResult.Defeated, new CombatRunStatistics(), profile, 3, 2, 8);

            StringAssert.Contains("방어", recommendation);
        }

        [Test]
        public void TimeExpiredWithoutActiveDamage_RecommendsDamageInvestment()
        {
            var advisor = new CombatBuildAdvisor();
            var profile = new CombatProfile(10, 1f, 30, 4, 0.2f, 0, 0f, false);

            var recommendation = advisor.CreateRecommendation(DungeonResult.TimeExpired, new CombatRunStatistics(), profile, 2, 4, 10);

            StringAssert.Contains("근력", recommendation);
        }

        [Test]
        public void Cleared_RecommendsNextChallenge()
        {
            var advisor = new CombatBuildAdvisor();
            var profile = new CombatProfile(20, 1f, 60, 5, 0.2f, 10, 0.1f, false);

            var recommendation = advisor.CreateRecommendation(DungeonResult.Cleared, new CombatRunStatistics(), profile, 3, 8, 8);

            StringAssert.Contains("높은 균열", recommendation);
        }

        [Test]
        public void CombatProfile_ExpectedDamagePerSecond_IncludesCriticalAndActiveDamage()
        {
            var profile = new CombatProfile(10, 2f, 40, 0, 0.2f, 10, 0.5f, false);

            Assert.That(profile.ExpectedDamagePerSecond, Is.EqualTo(9.5f).Within(0.001f));
        }
    }
}
