using NUnit.Framework;
using StatusWindow.Services;

namespace StatusWindow.Tests.EditMode
{
    public sealed class RankingScoreCalculatorTests
    {
        [Test]
        public void HigherDungeonAlwaysOutranksFasterLowerDungeon()
        {
            var lowerDungeon = RankingScoreCalculator.Calculate(0, 1f);
            var higherDungeon = RankingScoreCalculator.Calculate(1, 999f);

            Assert.That(higherDungeon, Is.GreaterThan(lowerDungeon));
        }

        [Test]
        public void FasterClearOutranksSlowerClearWithinSameDungeon()
        {
            var faster = RankingScoreCalculator.Calculate(2, 23.4f);
            var slower = RankingScoreCalculator.Calculate(2, 23.5f);

            Assert.That(faster, Is.GreaterThan(slower));
        }
    }
}
