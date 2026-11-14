/// <summary>
/// 死亡保命类型 Buff，在单位受到致死伤害后恢复配置比例的最大生命值。
/// 适用于七星灯、金刚不败等死亡前触发的自救效果。
/// </summary>
public class DeathSaveBuffLogic : BuffLogicBase
{
    private bool triggered;

    /// <summary>
    /// 创建死亡保命 Buff 逻辑并绑定所属单位与 Buff 配置。
    /// </summary>
    public DeathSaveBuffLogic(UnitLogicBase unitLogic, BuffLogicMachine buffLogicMachine, int cfgId)
        : base(unitLogic, buffLogicMachine, cfgId)
    {
    }

    /// <summary>
    /// 进入 Buff 时重置一次性触发标记。
    /// </summary>
    public override void Enter()
    {
        triggered = false;
    }

    /// <summary>
    /// 在致死伤害结算后尝试恢复生命并阻止死亡。
    /// </summary>
    public override bool TryPreventDeath()
    {
        if (triggered || mUnitLogic == null || mUnitLogic.UnitType != EUnitType.Solider || mUnitLogic.SoliderCfg == null || !UnitFactory.IsHero(mUnitLogic.SoliderCfg.id) || BuffCfg == null)
        {
            return false;
        }

        triggered = true;
        // 死亡回血比例统一读取 Buff 配置的 value，技能 skillParam 不参与计算。
        mUnitLogic.Prop.RestoreMaxHpPercent(BuffCfg.value);
        return true;
    }

    /// <summary>
    /// 离开 Buff 时执行清理；该 Buff 当前没有额外资源需要释放。
    /// </summary>
    public override void Exit()
    {
    }
}
