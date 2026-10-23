using UnityEngine;

/// <summary>反击螺旋：累计五次受击后，按累计伤害的比例反击一次。</summary>
public class CounterSpiralBuffLogic : BuffLogicBase
{
    private const int AttackCountToTrigger = 5;
    private int receivedAttackCount;
    private int accumulatedDamage;

    public CounterSpiralBuffLogic(
        UnitLogicBase unitLogic,
        BuffLogicMachine buffLogicMachine,
        int cfgId)
        : base(unitLogic, buffLogicMachine, cfgId)
    {
    }

    public override void Enter()
    {
        receivedAttackCount = 0;
        accumulatedDamage = 0;
    }

    public override int RecordDamage(int damage, bool isAttack)
    {
        if (!isAttack || mUnitLogic == null || mUnitLogic.IsDead)
        {
            return 0;
        }

        receivedAttackCount++;
        long currentDamage = damage < 0 ? -(long)damage : 0;
        long totalDamage = (long)accumulatedDamage + currentDamage;
        accumulatedDamage = totalDamage >= int.MaxValue ? int.MaxValue : (int)totalDamage;

        if (receivedAttackCount < AttackCountToTrigger)
        {
            return 0;
        }

        int counterDamage = Mathf.RoundToInt(accumulatedDamage * GetCounterDamageRate());
        if (accumulatedDamage > 0)
        {
            counterDamage = Mathf.Max(1, counterDamage);
        }
        receivedAttackCount = 0;
        accumulatedDamage = 0;
        return Mathf.Max(0, counterDamage);
    }

    public override void Exit()
    {
        receivedAttackCount = 0;
        accumulatedDamage = 0;
    }

    private float GetCounterDamageRate()
    {
        return BuffCfg == null ? 0f : Mathf.Max(0f, BuffCfg.value);
    }
}
