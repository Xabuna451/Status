using System;

namespace StatusWindow
{
    public readonly struct OfflineReward
    {
        public OfflineReward(int elapsedSeconds, int gold, int experience)
        {
            ElapsedSeconds = elapsedSeconds;
            Gold = gold;
            Experience = experience;
        }

        public int ElapsedSeconds { get; }
        public int Gold { get; }
        public int Experience { get; }
        public bool HasReward => Gold > 0 || Experience > 0;
    }

    /// <summary>Calculates capped, device-local offline rewards without mutating game state.</summary>
    public sealed class OfflineRewardCalculator
    {
        public const int MaximumOfflineSeconds = 8 * 60 * 60;
        private const float Efficiency = 0.6f;

        public OfflineReward Calculate(long lastSavedUtcTicks, DateTime utcNow, float goldPerSecond, float experiencePerSecond)
        {
            if (lastSavedUtcTicks <= 0 || goldPerSecond < 0f || experiencePerSecond < 0f)
            {
                return new OfflineReward(0, 0, 0);
            }

            DateTime lastSavedUtc;
            try
            {
                lastSavedUtc = new DateTime(lastSavedUtcTicks, DateTimeKind.Utc);
            }
            catch (ArgumentOutOfRangeException)
            {
                return new OfflineReward(0, 0, 0);
            }

            var elapsedSeconds = (int)Math.Floor((utcNow - lastSavedUtc).TotalSeconds);
            elapsedSeconds = Math.Max(0, Math.Min(elapsedSeconds, MaximumOfflineSeconds));
            var gold = (int)Math.Floor(elapsedSeconds * goldPerSecond * Efficiency);
            var experience = (int)Math.Floor(elapsedSeconds * experiencePerSecond * Efficiency);
            return new OfflineReward(elapsedSeconds, Math.Max(0, gold), Math.Max(0, experience));
        }
    }
}
