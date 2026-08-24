using System;

namespace StatusWindow.Services
{
    /// <summary>
    /// Deterministic client-side score format shared by the future ranking gateway.
    /// Higher cleared rifts always outrank lower rifts; time resolves ties.
    /// </summary>
    public static class RankingScoreCalculator
    {
        private const long TierWeight = 1_000_000_000L;

        public static long Calculate(int dungeonIndex, float clearSeconds)
        {
            var tier = Math.Max(0, dungeonIndex + 1);
            var milliseconds = Math.Max(0L, (long)Math.Round(clearSeconds * 1000f));
            return tier * TierWeight - Math.Min(milliseconds, TierWeight - 1L);
        }
    }
}
