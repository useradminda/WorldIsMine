using System.Collections;
using System.Collections.Generic;

using UnityEngine;
public class FlyObjectLogicBase
{
    protected FlyObjectCfg mFlyObjectCfg;
    protected Vector3 mOriPos;
    protected Vector3 mTarPos;

    protected UnitLogicBase mAtkUnitLogic;
    protected List<UnitLogicBase> mTargetLogicList;
    protected UnitLogicBase mSearchTargetUnitLogic;
    protected SkillLogicBase mSkillLogic;
   
    protected int mAttackDamage;
    protected int mAttackUnitType;
    protected float mRestrainValue;

    protected GameObject mFlyObjectGob;

    // 唯一下标
    private int flyUIndex;
    public int FlyUIndex => flyUIndex; 

    public FlyObjectLogicBase()
    {
        
    }

    public virtual void SetFlyObjectInfo(FlyObjectCfg flyObjectCfg, Vector3 oriPos, Vector3 tarPos, UnitLogicBase atkUnitLogic, List<UnitLogicBase> targetLogicList, UnitLogicBase searchTargetUnitLogic, SkillLogicBase skillLogic, int attackDamage, int flyUIndex)
    {
        this.mFlyObjectCfg = flyObjectCfg;
        this.mOriPos = oriPos;
        this.mTarPos = tarPos;
        this.mAtkUnitLogic = atkUnitLogic;
        this.mTargetLogicList = targetLogicList;
        this.mSearchTargetUnitLogic = searchTargetUnitLogic;
        this.mSkillLogic = skillLogic;
        this.flyUIndex = flyUIndex;
        mAttackDamage = attackDamage;
        mAttackUnitType = atkUnitLogic.SoliderCfg.unitType;
        mRestrainValue = atkUnitLogic.SoliderCfg.restrainValue;
    }

    public virtual void FlyObjectUpdate(float dt)
    {

    }

    public virtual void ArriveTarPos()
    {

    }

    protected int GetFinalDamage(UnitLogicBase target)
    {
        return -BattleLogicDamageTools.CalcFinalDamage(mAttackUnitType, target.SoliderCfg.unitType, mAttackDamage, mRestrainValue);
    }

    protected void CreateArriveEffect(Vector3 createPos)
    {
      if (mFlyObjectCfg.arrivePrefab == "")
          return;
       GameObject effect = UnitViewFactory.CreateGob(mFlyObjectCfg.arrivePrefab, createPos, Vector3.zero);
       effect.GetOrAddComponent<RecycleGobComponent>().SetRecycleGobTime(2f, mFlyObjectCfg.arrivePrefab);
    }

}
