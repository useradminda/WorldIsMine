using System.Collections;
using System.Collections.Generic;

using UnityEngine;
public class FlyObjectLogicBase
{
    private bool useState = false;
    public bool UseState => useState;

    protected Vector3 mOriPos;
    protected Vector3 mTarPos;

    protected UnitLogicBase mAtkUnitLogic;
    protected List<UnitLogicBase> mTargetLogicList;
    protected UnitLogicBase mSearchTargetUnitLogic;
    protected SkillLogicBase mSkillLogic;
    protected FlyObjectCfg mFlyObjectCfg;

    protected int mDamage;

    protected GameObject mFlyObjectGob;

    // 唯一下标
    private int flyUIndex;
    public int FlyUIndex => flyUIndex; 

    public FlyObjectLogicBase()
    {
        
    }

    public virtual void SetFlyObjectInfo(FlyObjectCfg flyObjectCfg, Vector3 oriPos, Vector3 tarPos, UnitLogicBase atkUnitLogic, List<UnitLogicBase> targetLogicList, UnitLogicBase searchTargetUnitLogic, SkillLogicBase skillLogic, int damage, int flyUIndex)
    {
        this.mFlyObjectCfg = flyObjectCfg;
        this.mOriPos = oriPos;
        this.mTarPos = tarPos;
        this.mAtkUnitLogic = atkUnitLogic;
        this.mTargetLogicList = targetLogicList;
        this.mSearchTargetUnitLogic = searchTargetUnitLogic;
        this.mSkillLogic = skillLogic;
        this.flyUIndex = flyUIndex;
        mDamage = damage;
    }

    public virtual void FlyObjectUpdate(float dt)
    {

    }

    public virtual void ArriveTarPos()
    {

    }

    public void Reset()
    {
        useState = false;
    }
}
