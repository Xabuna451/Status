namespace StatusWindow.Combat
{
    public sealed class CombatRunStatistics
    {
        public int DamageDealt { get; private set; }
        public int DamageTaken { get; private set; }
        public int BasicAttackCount { get; private set; }
        public int CriticalHitCount { get; private set; }
        public int ActiveSkillCastCount { get; private set; }
        public int ExecuteCount { get; private set; }

        public void Reset()
        {
            DamageDealt = 0;
            DamageTaken = 0;
            BasicAttackCount = 0;
            CriticalHitCount = 0;
            ActiveSkillCastCount = 0;
            ExecuteCount = 0;
        }

        public void RecordBasicAttack(int damage, bool critical)
        {
            DamageDealt += damage;
            BasicAttackCount++;
            if (critical) CriticalHitCount++;
        }

        public void RecordActiveSkill(int damage)
        {
            DamageDealt += damage;
            ActiveSkillCastCount++;
        }

        public void RecordDamageTaken(int damage)
        {
            DamageTaken += damage;
        }

        public void RecordExecute(int damage)
        {
            DamageDealt += damage;
            ExecuteCount++;
        }
    }
}
