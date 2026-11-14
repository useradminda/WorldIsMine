
public abstract class BuffLogicBase
{
    protected BuffLogicMachine mBuffLogicMachie;
    protected UnitLogicBase mUnitLogic;

    private float remainTime;
    public float RemainTIme =>  remainTime;

    public int CfgId => BuffCfg.id;

    /// <summary>
    /// 判断当前 Buff 是否属于指定类型，供死亡表现等逻辑查询当前状态使用。
    /// </summary>
    public bool IsType(BuffDefine.EBuffType buffType)
    {
        return BuffCfg != null && BuffCfg.tyep == (int)buffType;
    }

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

    /// <summary>播放 Buff 生效时的附着特效。</summary>
    public void PlayEnterEffect()
    {
        if (mUnitLogic == null || mUnitLogic.UnitView == null || BuffCfg == null
            || string.IsNullOrWhiteSpace(BuffCfg.enterBuffEff))
        {
            return;
        }

        float effectTime = BuffCfg.time > 0f ? BuffCfg.time : 2f;
        mUnitLogic.UnitView.PlayAttachEffect(BuffCfg.enterBuffEff, effectTime, UnityEngine.Vector3.zero);
    }

    /// <summary>播放 Buff 达成触发条件时的瞬时特效。</summary>
    public void PlayTriggerEffect()
    {
        if (mUnitLogic == null || mUnitLogic.UnitView == null || BuffCfg == null
            || string.IsNullOrWhiteSpace(BuffCfg.triggerBuffEff))
        {
            return;
        }
        mUnitLogic.UnitView.PlayEffect(BuffCfg.triggerBuffEff, mUnitLogic.CurPos, 2);
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
