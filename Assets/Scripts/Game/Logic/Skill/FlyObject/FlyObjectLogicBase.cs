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

    protected SkillLogicBase mSkillLogic;

    private FlyObjectCfg flyObjectCfg;
    protected FlyObjectCfg mFlyObjectCfg => flyObjectCfg;

    public FlyObjectLogicBase()
    {
        
    }

    public void SetFlyObjectInfo(FlyObjectCfg flyObjectCfg, Vector3 oriPos, Vector3 tarPos, UnitLogicBase atkUnitLogic, List<UnitLogicBase> targetLogicList, SkillLogicBase skillLogic)
    {
        this.flyObjectCfg = flyObjectCfg;
        this.mOriPos = oriPos;
        this.mTarPos = tarPos;
        this.mAtkUnitLogic = atkUnitLogic;
        this.mTargetLogicList = targetLogicList;
        this.mSkillLogic = skillLogic;
    }

    public virtual void FlyObjectUpdate(float dt)
    {

    }

    public virtual void TouchTarUnit()
    {

    }

    public void Reset()
    {
        useState = false;
    }
}
