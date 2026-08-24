using System;
using StatusWindow.Progression;

namespace StatusWindow.Combat
{
    public sealed class CombatBuildAdvisor
    {
        public string CreateRecommendation(DungeonResult result, CombatRunStatistics statistics, CombatProfile profile, int floor, int kills, int killTarget)
        {
            switch (result)
            {
                case DungeonResult.Defeated:
                    return profile.Defense <= floor
                        ? "추천: 의지에 투자하거나 방어 장비를 장착해 받는 피해를 줄이세요."
                        : "추천: 의지·생명력으로 최대 체력을 높여 보스 구간을 버티세요.";
                case DungeonResult.TimeExpired:
                    return statistics.ActiveSkillCastCount == 0 && profile.ActiveDamage == 0
                        ? "추천: 근력 또는 마력에 투자해 처치 시간을 줄이세요."
                        : kills < killTarget / 2
                            ? "추천: 근력·예리함·마력 폭발로 즉시 화력을 높이세요."
                            : "추천: 민첩·질풍의 장화로 다음 몬스터까지의 이동 지연을 줄이세요.";
                case DungeonResult.Cleared:
                    return "추천: 더 높은 균열에 도전하거나 회귀 조건을 향해 성장하세요.";
                default:
                    return "추천: 스탯과 스킬·장비 조합을 바꾼 뒤 던전에 입장하세요.";
            }
        }
    }

    public enum DungeonReadiness
    {
        Critical,
        Risky,
        Ready,
        Dominant,
    }

    public sealed class DungeonReadinessReport
    {
        public DungeonReadinessReport(DungeonReadiness readiness, float projectedClearSeconds, float totalTimeLimit, float projectedIncomingDamage, string recommendation)
        {
            Readiness = readiness;
            ProjectedClearSeconds = projectedClearSeconds;
            TotalTimeLimit = totalTimeLimit;
            ProjectedIncomingDamage = projectedIncomingDamage;
            Recommendation = recommendation;
        }

        public DungeonReadiness Readiness { get; }
        public float ProjectedClearSeconds { get; }
        public float TotalTimeLimit { get; }
        public float ProjectedIncomingDamage { get; }
        public string Recommendation { get; }
    }

    /// <summary>입장 전 빌드와 던전 데이터만 사용해 공략 위험도를 추정한다.</summary>
    public sealed class DungeonReadinessAnalyzer
    {
        public DungeonReadinessReport Analyze(DungeonDefinition dungeon, DungeonProtocolDefinition protocol, CombatProfile profile)
        {
            if (dungeon == null)
            {
                return new DungeonReadinessReport(DungeonReadiness.Critical, 0f, 0f, 0f, "던전과 빌드 정보를 확인할 수 없습니다.");
            }

            var timeMultiplier = protocol == null ? 1f : protocol.TimeLimitMultiplier;
            var healthMultiplier = protocol == null ? 1f : protocol.EnemyHealthMultiplier;
            var damageMultiplier = protocol == null ? 1f : protocol.EnemyDamageMultiplier;
            var totalKills = 0;
            var totalHealth = 0f;
            var totalTimeLimit = dungeon.FloorCount * dungeon.FloorTimeLimit * timeMultiplier;

            for (var floor = 1; floor <= dungeon.FloorCount; floor++)
            {
                var killTarget = dungeon.BaseKillTarget + (floor - 1) * dungeon.KillTargetPerFloor;
                totalKills += killTarget;
                for (var kill = 0; kill < killTarget; kill++)
                {
                    totalHealth += (dungeon.BaseEnemyHealth + floor * dungeon.EnemyHealthPerFloor + kill * dungeon.EnemyHealthPerKill) * healthMultiplier;
                }
            }

            var attackSeconds = totalHealth / Math.Max(0.1f, profile.ExpectedDamagePerSecond);
            var movementSeconds = Math.Max(0, totalKills - dungeon.FloorCount) * profile.MoveDelay;
            var projectedClearSeconds = attackSeconds + movementSeconds;
            var averageEnemyDamage = (dungeon.EnemyDamageBase + dungeon.FloorCount * dungeon.EnemyDamagePerFloor) * damageMultiplier;
            var incomingDamage = projectedClearSeconds / 1.25f * Math.Max(1f, averageEnemyDamage - profile.Defense);

            if (projectedClearSeconds > totalTimeLimit)
            {
                return new DungeonReadinessReport(DungeonReadiness.Critical, projectedClearSeconds, totalTimeLimit, incomingDamage, "공략 시간 초과 예상: 공세 지침으로 화력을 높이거나 추적 지침으로 이동 시간을 줄이세요.");
            }

            if (incomingDamage > profile.MaxHealth * 1.1f)
            {
                return new DungeonReadinessReport(DungeonReadiness.Risky, projectedClearSeconds, totalTimeLimit, incomingDamage, "생존 위험 예상: 방호 지침, 의지, 방어 장비를 우선 검토하세요.");
            }

            if (projectedClearSeconds <= totalTimeLimit * 0.55f && incomingDamage <= profile.MaxHealth * 0.55f)
            {
                return new DungeonReadinessReport(DungeonReadiness.Dominant, projectedClearSeconds, totalTimeLimit, incomingDamage, "안정 공략 예상: 더 높은 프로토콜 또는 다음 균열에 도전할 수 있습니다.");
            }

            return new DungeonReadinessReport(DungeonReadiness.Ready, projectedClearSeconds, totalTimeLimit, incomingDamage, "공략 가능 예상: 결과를 확인해 부족한 축에만 투자하세요.");
        }
    }
}
