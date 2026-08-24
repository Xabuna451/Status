using System;
using NUnit.Framework;

namespace StatusWindow.Tests.EditMode
{
    public sealed class OfflineRewardCalculatorTests
    {
        private readonly OfflineRewardCalculator calculator = new OfflineRewardCalculator();

        [Test]
        public void Calculate_AppliesSixtyPercentEfficiency()
        {
            var now = new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);
            var reward = calculator.Calculate(now.AddMinutes(-10).Ticks, now, 2f, 1f);

            Assert.That(reward.ElapsedSeconds, Is.EqualTo(600));
            Assert.That(reward.Gold, Is.EqualTo(720));
            Assert.That(reward.Experience, Is.EqualTo(360));
        }

        [Test]
        public void Calculate_CapsElapsedTimeAtEightHours()
        {
            var now = new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);
            var reward = calculator.Calculate(now.AddHours(-12).Ticks, now, 1f, 0f);

            Assert.That(reward.ElapsedSeconds, Is.EqualTo(OfflineRewardCalculator.MaximumOfflineSeconds));
            Assert.That(reward.Gold, Is.EqualTo(17280));
        }

        [Test]
        public void Calculate_RejectsFutureOrMissingSaveTime()
        {
            var now = new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);

            var futureReward = calculator.Calculate(now.AddMinutes(1).Ticks, now, 1f, 1f);
            var missingReward = calculator.Calculate(0L, now, 1f, 1f);

            Assert.That(futureReward.HasReward, Is.False);
            Assert.That(missingReward.HasReward, Is.False);
        }
    }
}
