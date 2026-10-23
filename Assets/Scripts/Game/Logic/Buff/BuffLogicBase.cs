
public abstract class BuffLogicBase
{
    protected BuffLogicMachine mBuffLogicMachie;
    protected UnitLogicBase mUnitLogic;

    private float remainTime;
    public float RemainTIme =>  remainTime;

    public int CfgId => BuffCfg.id;

    public bool IsTimeFlow => BuffCfg != null && BuffCfg.time >= 0;

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

    public void PlayEnterEffect()
    {
        if (mUnitLogic == null || mUnitLogic.UnitView == null || BuffCfg == null
            || string.IsNullOrWhiteSpace(BuffCfg.enterBuffEff))
        {
            return;
        }

        float effectTime = BuffCfg.time > 0f ? BuffCfg.time : 2f;
        mUnitLogic.UnitView.PlayAttachEffect(BuffCfg.enterBuffEff, effectTime);
    }

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

    public virtual bool TryPreventDeath()
    {
        return false;
    }

    /// <summary>在伤害真正扣除前修改伤害数值。</summary>
    public virtual void ModifyDamage(ref int damage)
    {
    }

    /// <summary>记录一次受击，并返回本次受击后需要反击的伤害。</summary>
    public virtual int RecordDamage(int damage, bool isAttack)
    {
        return 0;
    }
}
