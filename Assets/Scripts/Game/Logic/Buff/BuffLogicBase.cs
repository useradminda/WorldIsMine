
public abstract class BuffLogicBase
{
    protected BuffLogicMachine mBuffLogicMachie;
    protected UnitLogicBase mUnitLogic;

    private float remainTime;
    public int CfgId => BuffCfg.id;
    public bool IsExpired => remainTime <= 0f;
    protected BuffCfg BuffCfg { get; private set; }

    public BuffLogicBase(UnitLogicBase unitLoigc, BuffLogicMachine buffLogicMachie, int cfgId)
    {
        this.mUnitLogic = unitLoigc;
        this.mBuffLogicMachie = buffLogicMachie;
        BuffCfg = BuffCfgConfig.Ins.SearchById(cfgId);
        remainTime = BuffCfg == null ? 0f : BuffCfg.time;
    }

    /// <summary>Buff生效时调用。</summary>
    public abstract void Enter();

    /// <summary>更新Buff剩余时间。</summary>
    public void Update(float dt)
    {
        remainTime -= dt;
    }

    /// <summary>相同Buff再次添加时刷新持续时间。</summary>
    public void Refresh(float duration = -1f)
    {
        remainTime = duration > 0f ? duration : BuffCfg.time;
    }

    /// <summary>Buff移除时恢复它修改的属性。</summary>
    public abstract void Exit();
}
