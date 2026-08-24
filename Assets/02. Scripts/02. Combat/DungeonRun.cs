using System;
using StatusWindow.Progression;

namespace StatusWindow.Combat
{
    public enum DungeonResult
    {
        None,
        Cleared,
        TimeExpired,
        Defeated,
        Cancelled,
    }

    /// <summary>Presentation-safe combat event classification. UI must not infer gameplay from localized messages.</summary>
    public enum CombatEventType
    {
        Idle,
        DungeonEntered,
        FloorAdvanced,
        BasicAttack,
        CriticalAttack,
        ActiveSkill,
        Execute,
        EnemyAttack,
        BarrierRaised,
        EnemyEnraged,
        Cancelled,
        Cleared,
        Failed,
    }

    public sealed class DungeonRun
    {
        private readonly Random random = new Random();
        private readonly DungeonDefinition definition;
        private readonly DungeonProtocolDefinition protocol;
        private readonly CombatBuildAdvisor buildAdvisor = new CombatBuildAdvisor();
        private StatusWindowGameState gameState;
        private CombatProfile profile;
        private float attackTimer;
        private float enemyAttackTimer;
        private float movementTimer;
        private float activeSkillTimer;
        private int enemyHealth;
        private int enemyBarrier;
        private bool enemyEnraged;
        private EnemyDefinition currentEnemy;
        private int combatEventSequence;

        public bool IsRunning { get; private set; }
        public int Floor { get; private set; }
        public int Kills { get; private set; }
        public int KillTarget { get; private set; }
        public int PlayerHealth { get; private set; }
        public int PlayerMaxHealth => profile.MaxHealth;
        public int EnemyHealth => enemyHealth;
        public int EnemyMaxHealth { get; private set; }
        public int EnemyBarrier => enemyBarrier;
        public string ProtocolName => protocol == null ? "표준 프로토콜" : protocol.DisplayName;
        public string CurrentEnemyName => currentEnemy == null ? "균열의 잔재" : currentEnemy.DisplayName;
        public string CurrentEnemyDescription => currentEnemy == null ? "불안정한 마력이 응집된 적입니다." : currentEnemy.Description;
        public string CurrentEnemyTrait => GetTraitDescription(currentEnemy == null ? EnemyCombatTrait.None : currentEnemy.CombatTrait);
        public float TimeRemaining { get; private set; }
        public float CurrentFloorTimeLimit => definition.FloorTimeLimit * GetTimeLimitMultiplier();
        public float ElapsedTime { get; private set; }
        public DungeonResult Result { get; private set; }
        public string ResultMessage { get; private set; } = "상태창에서 빌드를 만든 뒤 던전에 입장하세요.";
        public string Recommendation { get; private set; } = "스탯과 장비를 구성한 뒤 던전에 입장하세요.";
        public string EquipmentRewardName { get; private set; }
        public int GoldEarned { get; private set; }
        public int ExperienceEarned { get; private set; }
        public string LastCombatEvent { get; private set; } = "전투 대기 중";
        public CombatEventType LastCombatEventType { get; private set; } = CombatEventType.Idle;
        public CombatRunStatistics Statistics { get; } = new CombatRunStatistics();

        public DungeonRun(DungeonDefinition dungeonDefinition, DungeonProtocolDefinition protocolDefinition = null)
        {
            definition = dungeonDefinition;
            protocol = protocolDefinition;
        }

        public void Start(StatusWindowGameState state)
        {
            gameState = state;
            profile = state.CreateCombatProfile();
            Floor = 1;
            Kills = 0;
            KillTarget = GetKillTarget(Floor);
            PlayerHealth = profile.MaxHealth;
            TimeRemaining = definition.FloorTimeLimit * GetTimeLimitMultiplier();
            ElapsedTime = 0f;
            Result = DungeonResult.None;
            ResultMessage = "빌드가 잠겼습니다. 자동전투를 시작합니다.";
            Recommendation = "전투 결과가 빌드 분석으로 표시됩니다.";
            EquipmentRewardName = string.Empty;
            GoldEarned = 0;
            ExperienceEarned = 0;
            combatEventSequence = 0;
            Statistics.Reset();
            IsRunning = true;
            SpawnEnemy();
            SetCombatEvent($"{Floor}층 균열에 진입했습니다. {CurrentEnemyName} 탐색 완료.", CombatEventType.DungeonEntered);
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning)
            {
                return;
            }

            ElapsedTime += deltaTime;
            TimeRemaining -= deltaTime;
            if (TimeRemaining <= 0f)
            {
                Finish(DungeonResult.TimeExpired, "시간 초과: 화력 또는 이동 효율을 높여 보세요.");
                return;
            }

            if (movementTimer > 0f)
            {
                movementTimer -= deltaTime;
                return;
            }

            attackTimer -= deltaTime;
            enemyAttackTimer -= deltaTime;
            activeSkillTimer -= deltaTime;

            if (attackTimer <= 0f)
            {
                var critical = random.NextDouble() < profile.CriticalChance;
                var damage = profile.Damage * (critical ? 2 : 1);
                ApplyBasicAttack(damage, critical);
                attackTimer += profile.AttackInterval;
            }

            if (profile.ActiveDamage > 0 && activeSkillTimer <= 0f)
            {
                ApplyActiveSkill(profile.ActiveDamage);
                activeSkillTimer = 5f;
            }

            TryExecuteEnemy();

            if (enemyHealth <= 0)
            {
                DefeatEnemy();
                return;
            }

            if (enemyAttackTimer <= 0f)
            {
                var rawDamage = (definition.EnemyDamageBase + Floor * definition.EnemyDamagePerFloor) * GetEnemyDamageMultiplier();
                var damageTaken = Math.Max(1, (int)Math.Ceiling(rawDamage) - profile.Defense);
                PlayerHealth -= damageTaken;
                Statistics.RecordDamageTaken(damageTaken);
                SetCombatEvent($"{CurrentEnemyName}의 공격! {damageTaken} 피해를 받았습니다.", CombatEventType.EnemyAttack);
                enemyAttackTimer = GetEnemyAttackInterval();
                if (PlayerHealth <= 0)
                {
                    Finish(DungeonResult.Defeated, "생존 실패: 의지 또는 방어 장비가 필요합니다.");
                }
            }
        }

        /// <summary>Ends a player-requested auto battle without granting a clear reward.</summary>
        public bool Cancel()
        {
            if (!IsRunning)
            {
                return false;
            }

            Result = DungeonResult.Cancelled;
            ResultMessage = "자동전투를 중단했습니다. 빌드를 다시 변경할 수 있습니다.";
            Recommendation = "전투력을 보강하거나 균열 설정을 바꾼 뒤 다시 도전하세요.";
            IsRunning = false;
            SetCombatEvent("자동전투를 중단하고 상태창을 다시 열었습니다.", CombatEventType.Cancelled);
            return true;
        }

        private void DefeatEnemy()
        {
            Kills++;
            var goldReward = (int)Math.Ceiling((6 + Floor * 3) * (currentEnemy == null ? 1f : currentEnemy.GoldMultiplier) * GetRewardMultiplier());
            var experienceReward = (int)Math.Ceiling((5 + Floor * 2) * (currentEnemy == null ? 1f : currentEnemy.ExperienceMultiplier) * GetRewardMultiplier());
            GrantCombatReward(goldReward, experienceReward);

            if (Kills >= KillTarget)
            {
                if (Floor >= definition.FloorCount)
                {
                    GrantCombatReward((int)Math.Ceiling(definition.ClearGoldReward * GetRewardMultiplier()), (int)Math.Ceiling(definition.ClearExperienceReward * GetRewardMultiplier()));
                    var masteryIncreased = gameState.RecordDungeonClear(definition);
                    var newRecord = gameState.TryRecordDungeonBestTime(definition, ElapsedTime);
                    GrantEquipmentReward();
                    var equipmentMessage = string.IsNullOrEmpty(EquipmentRewardName) ? string.Empty : $" 장비 보상: {EquipmentRewardName}!";
                    var masteryMessage = masteryIncreased ? $" 균열 숙련도 {gameState.GetDungeonMasteryRank(definition)}/{definition.MaximumMasteryRank}!" : string.Empty;
                    var recordMessage = newRecord ? $" 최고 기록 {ElapsedTime:0.0}초!" : $" 공략 시간 {ElapsedTime:0.0}초.";
                    Finish(DungeonResult.Cleared, $"던전 공략 성공! GOLD +{GoldEarned:N0} / EXP +{ExperienceEarned:N0}.{equipmentMessage}{masteryMessage}{recordMessage} 다음 빌드로 더 빠른 기록에 도전하세요.");
                    return;
                }

                Floor++;
                Kills = 0;
                KillTarget = GetKillTarget(Floor);
                TimeRemaining = definition.FloorTimeLimit * GetTimeLimitMultiplier();
                ResultMessage = $"{Floor}층 진입. 몬스터가 더 강해집니다.";
                SetCombatEvent($"{Floor}층으로 이동합니다. 처치 목표가 증가했습니다.", CombatEventType.FloorAdvanced);
            }

            movementTimer = profile.MoveDelay;
            SpawnEnemy();
        }

        private void SpawnEnemy()
        {
            currentEnemy = ChooseEnemy();
            var baseHealth = definition.BaseEnemyHealth + Floor * definition.EnemyHealthPerFloor + Kills * definition.EnemyHealthPerKill;
            EnemyMaxHealth = Math.Max(1, (int)Math.Ceiling(baseHealth * (currentEnemy == null ? 1f : currentEnemy.HealthMultiplier) * GetEnemyHealthMultiplier()));
            enemyHealth = EnemyMaxHealth;
            enemyBarrier = HasBarrierTrait() ? (int)Math.Ceiling(EnemyMaxHealth * 0.2f) : 0;
            enemyEnraged = false;
            enemyAttackTimer = 0.8f;
            if (enemyBarrier > 0) SetCombatEvent($"{CurrentEnemyName}이(가) 균열 장벽 {enemyBarrier}을 전개했습니다.", CombatEventType.BarrierRaised);
        }

        private EnemyDefinition ChooseEnemy()
        {
            if (Kills == KillTarget - 1 && definition.FloorBoss != null) return definition.FloorBoss;
            if (definition.Enemies == null || definition.Enemies.Count == 0) return null;
            return definition.Enemies[random.Next(definition.Enemies.Count)];
        }

        private void GrantEquipmentReward()
        {
            var maximumGoldCost = 80 + definition.RequiredLevel * 10;
            EquipmentDefinition selectedEquipment = null;
            var eligibleCount = 0;
            foreach (var equipment in gameState.Catalog.Equipment)
            {
                if (gameState.HasEquipment(equipment) || equipment.GoldCost > maximumGoldCost) continue;
                eligibleCount++;
                if (random.Next(eligibleCount) == 0) selectedEquipment = equipment;
            }

            if (selectedEquipment != null && gameState.TryGrantEquipment(selectedEquipment))
            {
                EquipmentRewardName = selectedEquipment.DisplayName;
            }
        }

        private void GrantCombatReward(int goldReward, int experienceReward)
        {
            var goldBefore = gameState.Gold;
            gameState.GainCombatReward(goldReward, experienceReward);
            GoldEarned += Math.Max(0, gameState.Gold - goldBefore);
            ExperienceEarned += Math.Max(0, experienceReward);
        }

        private float GetEnemyDamageMultiplier()
        {
            var multiplier = (currentEnemy == null ? 1f : currentEnemy.DamageMultiplier) * (protocol == null ? 1f : protocol.EnemyDamageMultiplier);
            if (HasEnrageTrait() && enemyHealth <= EnemyMaxHealth / 2)
            {
                if (!enemyEnraged)
                {
                    enemyEnraged = true;
                    SetCombatEvent($"{CurrentEnemyName}이(가) 폭주했습니다! 공격력이 상승합니다.", CombatEventType.EnemyEnraged);
                }
                multiplier *= 1.5f;
            }
            return multiplier;
        }

        private float GetEnemyAttackInterval()
        {
            var interval = currentEnemy == null ? 1.25f : currentEnemy.AttackInterval;
            return currentEnemy != null && currentEnemy.CombatTrait == EnemyCombatTrait.Swift ? interval * 0.75f : interval;
        }

        private bool HasBarrierTrait() => currentEnemy != null && (currentEnemy.CombatTrait == EnemyCombatTrait.Barrier || currentEnemy.CombatTrait == EnemyCombatTrait.BarrierEnrage);
        private bool HasEnrageTrait() => currentEnemy != null && (currentEnemy.CombatTrait == EnemyCombatTrait.Enrage || currentEnemy.CombatTrait == EnemyCombatTrait.BarrierEnrage);

        private float GetEnemyHealthMultiplier() => protocol == null ? 1f : protocol.EnemyHealthMultiplier;
        private float GetTimeLimitMultiplier() => protocol == null ? 1f : protocol.TimeLimitMultiplier;
        private float GetRewardMultiplier() => protocol == null ? 1f : protocol.RewardMultiplier;

        private void ApplyBasicAttack(int damage, bool critical)
        {
            var appliedDamage = ApplyDamage(damage);
            Statistics.RecordBasicAttack(appliedDamage, critical);
            SetCombatEvent(
                critical ? $"치명타! {CurrentEnemyName}에게 {appliedDamage} 피해." : $"기본 공격! {CurrentEnemyName}에게 {appliedDamage} 피해.",
                critical ? CombatEventType.CriticalAttack : CombatEventType.BasicAttack);
        }

        private void ApplyActiveSkill(int damage)
        {
            var appliedDamage = ApplyDamage(damage);
            Statistics.RecordActiveSkill(appliedDamage);
            SetCombatEvent($"자동 액티브 발동! {CurrentEnemyName}에게 {appliedDamage} 피해.", CombatEventType.ActiveSkill);
        }

        private int ApplyDamage(int damage)
        {
            var appliedDamage = Math.Min(damage, enemyBarrier + enemyHealth);
            var barrierDamage = Math.Min(damage, enemyBarrier);
            enemyBarrier -= barrierDamage;
            enemyHealth -= Math.Max(0, damage - barrierDamage);
            return appliedDamage;
        }

        private static string GetTraitDescription(EnemyCombatTrait trait)
        {
            switch (trait)
            {
                case EnemyCombatTrait.Swift: return "속공: 공격 주기 25% 감소";
                case EnemyCombatTrait.Barrier: return "장벽: 등장 시 체력 20% 장벽";
                case EnemyCombatTrait.Enrage: return "폭주: 체력 50% 이하 공격력 +50%";
                case EnemyCombatTrait.BarrierEnrage: return "수호: 장벽 전개 및 체력 50% 이하 폭주";
                default: return "일반";
            }
        }

        private void TryExecuteEnemy()
        {
            if (!profile.Execute || enemyHealth <= 0 || enemyHealth > EnemyMaxHealth * 0.25f)
            {
                return;
            }

            Statistics.RecordExecute(enemyHealth);
            SetCombatEvent($"처형 발동! {CurrentEnemyName}의 남은 체력을 제거했습니다.", CombatEventType.Execute);
            enemyHealth = 0;
        }

        private void Finish(DungeonResult result, string message)
        {
            Result = result;
            ResultMessage = message;
            Recommendation = buildAdvisor.CreateRecommendation(result, Statistics, profile, Floor, Kills, KillTarget);
            IsRunning = false;
            SetCombatEvent(
                result == DungeonResult.Cleared ? "공략 완료. 보상을 상태창에 반영했습니다." : message,
                result == DungeonResult.Cleared ? CombatEventType.Cleared : CombatEventType.Failed);
        }

        private void SetCombatEvent(string message, CombatEventType eventType = CombatEventType.Idle)
        {
            combatEventSequence++;
            LastCombatEvent = $"[{combatEventSequence:00}] {message}";
            LastCombatEventType = eventType;
        }

        private int GetKillTarget(int floor) => definition.BaseKillTarget + floor * definition.KillTargetPerFloor;
    }
}
