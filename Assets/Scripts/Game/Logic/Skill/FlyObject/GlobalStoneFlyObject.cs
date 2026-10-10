using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局火球的单发飞行逻辑。
/// 火球不进入 ProjectileJobManager，自己计算抛物线并在落点结算范围伤害。
/// </summary>
public class GlobalStoneFlyObject : FlyObjectLogicBase
{
    private readonly List<int> resultUnitIndexList = new List<int>();

    private int searchReqIndex = -1;
    private int nearestIndex = -1;
    private int randomIndex = -1;
    private float flyTime;
    private float flyDuration;
    private float maxFlyTime;
    private float flyHeight;
    private bool arrived;

    /// <summary>
    /// 初始化全局火球，并创建火球表现对象。
    /// </summary>
    public override void SetFlyObjectInfo(FlyObjectCfg flyObjectCfg, Vector3 oriPos, Vector3 tarPos, UnitLogicBase atkUnitLogic, List<UnitLogicBase> targetLogicList, UnitLogicBase searchTargetUnit, SkillLogicBase skillLogic, int attackDamage, int flyUIndex)
    {
        base.SetFlyObjectInfo(flyObjectCfg, oriPos, tarPos, atkUnitLogic, targetLogicList, searchTargetUnit, skillLogic, attackDamage, flyUIndex);

        Vector3 horizontalOffset = tarPos - oriPos;
        horizontalOffset.y = 0f;
        float distance = horizontalOffset.magnitude;
        float speed = Mathf.Max(0.01f, mFlyObjectCfg.speed);

        flyTime = 0f;
        flyDuration = Mathf.Max(0.2f, distance / speed);
        maxFlyTime = mFlyObjectCfg.liveTime > 0f
            ? mFlyObjectCfg.liveTime
            : flyDuration + 1f;
        flyHeight = Mathf.Max(4f, distance * 0.3f);
        arrived = false;

        mFlyObjectGob = UnitViewFactory.CreateGob(mFlyObjectCfg.prefab, oriPos, tarPos - oriPos);
        UpdateFlyTransform(oriPos, tarPos - oriPos);
    }

    /// <summary>
    /// 每帧提交搜索、更新抛物线位置，并在到达落点时结算伤害。
    /// </summary>
    public override void FlyObjectUpdate(float dt)
    {
        if (mFlyObjectGob == null || arrived)
        {
            return;
        }

        RequestDamageSearch();

        flyTime += dt;
        float progress = Mathf.Clamp01(flyTime / flyDuration);
        Vector3 currentPosition = Vector3.Lerp(mOriPos, mTarPos, progress);
        currentPosition.y += Mathf.Sin(progress * Mathf.PI) * flyHeight;
        Vector3 nextPosition = GetParabolaPosition(Mathf.Clamp01((flyTime + 0.02f) / flyDuration));
        UpdateFlyTransform(currentPosition, nextPosition - currentPosition);

        if (progress >= 1f || flyTime >= maxFlyTime)
        {
            ArriveTarPos();
        }
    }

    /// <summary>
    /// 在火球落点创建命中特效，并对搜索结果中的敌方单位造成范围伤害。
    /// </summary>
    public override void ArriveTarPos()
    {
        if (arrived)
        {
            return;
        }

        arrived = true;
        CreateArriveEffect(mTarPos);
        DamageTargets();
        Die();
    }

    /// <summary>
    /// 每帧搜索落点范围内的敌方单位。
    /// </summary>
    private void RequestDamageSearch()
    {
        if (mSkillLogic == null || mAtkUnitLogic == null)
        {
            return;
        }

        searchReqIndex = MapCellManager.Instance.RequestSearch(mTarPos, mSkillLogic.SkillCfg.skillArea, mAtkUnitLogic.OtherCampTypeInt);
        MapCellManager.Instance.GetResult(searchReqIndex, resultUnitIndexList, ref nearestIndex, ref randomIndex);
    }

    /// <summary>
    /// 对落点范围内的敌方单位结算一次伤害。
    /// </summary>
    private void DamageTargets()
    {
        if (mSkillLogic == null || resultUnitIndexList.Count == 0)
        {
            return;
        }

        for (int i = 0; i < resultUnitIndexList.Count; i++)
        {
            int unitIndex = resultUnitIndexList[i];
            if (unitIndex < 0 || unitIndex >= UnitManager.Instance.UnitList.Count)
            {
                continue;
            }

            UnitLogicBase target = UnitManager.Instance.UnitList[unitIndex];
            if (target == null || target.IsDead || target.CampTypeInt == mAtkUnitLogic.CampTypeInt)
            {
                continue;
            }

            BattleLogicDamageTools.DoDamage(mAtkUnitLogic, target, GetFinalDamage(target), target.UId, mSkillLogic.SkillCfg.dieType, mTarPos);
            mSkillLogic.ApplyBuffs(target);
        }
    }

    /// <summary>
    /// 计算指定进度下的抛物线位置。
    /// </summary>
    private Vector3 GetParabolaPosition(float progress)
    {
        Vector3 position = Vector3.Lerp(mOriPos, mTarPos, progress);
        position.y += Mathf.Sin(progress * Mathf.PI) * flyHeight;
        return position;
    }

    /// <summary>
    /// 更新火球位置和朝向。
    /// </summary>
    private void UpdateFlyTransform(Vector3 position, Vector3 direction)
    {
        mFlyObjectGob.transform.position = position;
        if (direction.sqrMagnitude > 0.0001f)
        {
            mFlyObjectGob.transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    /// <summary>
    /// 回收火球表现并从飞行物管理器移除逻辑对象。
    /// </summary>
    private void Die()
    {
        if (mFlyObjectGob != null)
        {
            UnitViewFactory.RemoveGob(mFlyObjectCfg.prefab, mFlyObjectGob);
            mFlyObjectGob = null;
        }

        UnitFactory.RemoveFlyObjectLogic(this);
    }
}
