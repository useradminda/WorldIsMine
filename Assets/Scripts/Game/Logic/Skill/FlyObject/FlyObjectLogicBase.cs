using System.Collections;
using System.Collections.Generic;

using UnityEngine;
/// <summary>
/// 所有飞行器逻辑的基类。
/// 负责保存施法者、目标、技能、伤害快照和表现对象，并提供统一的生命周期与回收入口。
/// </summary>
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

    public virtual void FlyObjectLateUpdate(float dt)
    {
    }

    /// <summary>
    /// 清理飞行器逻辑和表现对象，用于战斗结束或场景重置。
    /// </summary>
    public virtual void ClearForBattle()
    {
        if (mFlyObjectGob != null && mFlyObjectCfg != null)
        {
            UnitViewFactory.RemoveGob(mFlyObjectCfg.prefab, mFlyObjectGob);
            mFlyObjectGob = null;
        }
    }

    public virtual void ArriveTarPos()
    {

    }

    protected int GetFinalDamage(UnitLogicBase target)
    {
        return -BattleLogicDamageTools.CalcFinalDamage(mAttackUnitType, target.SoliderCfg.unitType, mAttackDamage, mRestrainValue);
    }

    /// <summary>
    /// 查找指定范围内的敌方城门；范围搜索表只包含普通单位，因此城门需要单独判断。
    /// </summary>
    protected UnitLogicBase FindEnemyWallInArea(Vector3 center, float radius)
    {
        UnitLogicBase enemyWall = UnitManager.Instance.GetOtherWallLogic(mAtkUnitLogic.CampType);
        if (enemyWall == null || enemyWall.IsDead)
        {
            return null;
        }

        Vector3 wallPoint = enemyWall.GetClosestPoint(center);
        Vector3 offset = wallPoint - center;
        offset.y = 0f;
        return offset.sqrMagnitude <= radius * radius ? enemyWall : null;
    }

    /// <summary>
    /// 对指定范围内的敌方城门执行一次飞行器伤害。
    /// </summary>
    protected void DamageEnemyWallInArea(Vector3 center, float radius)
    {
        UnitLogicBase enemyWall = FindEnemyWallInArea(center, radius);
        if (enemyWall == null || mSkillLogic == null)
        {
            return;
        }

        Vector3 hitPoint = enemyWall.GetClosestPoint(center);
        BattleLogicDamageTools.DoDamage(
            mAtkUnitLogic,
            enemyWall,
            GetFinalDamage(enemyWall),
            enemyWall.UId,
            mSkillLogic.SkillCfg.dieType,
            hitPoint);
    }

    protected void CreateArriveEffect(Vector3 createPos)
    {
      if (mFlyObjectCfg.arrivePrefab == "")
          return;
       GameObject effect = UnitViewFactory.CreateGob(mFlyObjectCfg.arrivePrefab, createPos, Vector3.zero);
       effect.GetOrAddComponent<RecycleGobComponent>().SetRecycleGobTime(2f, mFlyObjectCfg.arrivePrefab);
    }

}
