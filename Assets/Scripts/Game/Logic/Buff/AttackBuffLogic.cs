using UnityEngine;

public class AttackBuffLogic : BuffLogicBase
{
    private int attackValue;
    public int AttackValue => attackValue;

    public AttackBuffLogic(
        UnitLogicBase unitLogic,
        BuffLogicMachine buffLogicMachine,
        int cfgId)
        : base(unitLogic, buffLogicMachine, cfgId)
    {
    }

    /// <summary>按照千分比配置计算实际攻击力增量。</summary>
    public override void Enter()
    {
        if (mUnitLogic.SoliderCfg == null)
        {
            attackValue = 0;
            return;
        }

        attackValue = Mathf.RoundToInt(
            mUnitLogic.SoliderCfg.atk * BuffCfg.value / 1000f);
        mUnitLogic.ChangeAttackBuffValue(attackValue);
    }

    /// <summary>移除Buff时撤销本Buff提供的攻击力。</summary>
    public override void Exit()
    {
        mUnitLogic.ChangeAttackBuffValue(-attackValue);
        attackValue = 0;
    }
}
