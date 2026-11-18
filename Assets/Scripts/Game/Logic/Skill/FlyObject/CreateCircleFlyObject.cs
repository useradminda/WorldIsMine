using System;
using System.Collections.Generic;
using System.Globalization;
using Nebukam.ORCA;
using UnityEngine;

/// <summary>
/// 创建圆形RVO障碍并持续造成范围伤害的飞行器。
/// 圆心使用释放者位置，半径读取飞行器flyParam，圆形初始使用20个顶点。
/// </summary>
public class CreateCircleFlyObject : FlyObjectLogicBase
{
    // 圆形障碍初始使用的顶点数量。
    private const int CirclePointCount = 20;

    private Obstacle obstacle;
    private Vector3 circleCenter;
    private float circleRadius;
    private float liveTime;
    private float damageClipTime;
    private int triggeredDamageCount;
    private bool damagePending;
    private int searchRequest = -1;
    private readonly List<int> resultIndices = new List<int>();
    private readonly List<int> resultUids = new List<int>();
    private int nearestIndex = -1;
    private int randomIndex = -1;
    private GameObject circleGob;
    private bool destroyed;

    /// <summary>
    /// 初始化圆形障碍、解析半径、创建表现对象并准备范围伤害搜索。
    /// </summary>
    public override void SetFlyObjectInfo(
        FlyObjectCfg flyObjectCfg, Vector3 oriPos, Vector3 tarPos,
        UnitLogicBase atkUnitLogic, List<UnitLogicBase> targetLogicList,
        UnitLogicBase searchTargetLogic, SkillLogicBase skillLogic,
        int attackDamage, int flyUIndex)
    {
        base.SetFlyObjectInfo(flyObjectCfg, oriPos, tarPos, atkUnitLogic,
            targetLogicList, searchTargetLogic, skillLogic, attackDamage, flyUIndex);
        liveTime = flyObjectCfg.liveTime;
        damageClipTime = 0f;
        triggeredDamageCount = 0;
        damagePending = false;
        destroyed = false;
        resultIndices.Clear();
        resultUids.Clear();

        if (!TryParseRadius(flyObjectCfg.flyParam, out circleRadius))
        {
            Debug.LogError($"Create circle flyParam无效，FlyObjectId={flyObjectCfg.id}");
            Die();
            return;
        }

        circleCenter = atkUnitLogic.CurPos;
        obstacle = RvoManager.Instance.AddDynamicObstacle(CreateCircleVertices(circleCenter, circleRadius));
        circleGob = string.IsNullOrWhiteSpace(flyObjectCfg.prefab)
            ? null
            : UnitViewFactory.CreateGob(flyObjectCfg.prefab, circleCenter, Vector3.forward);
    }

    /// <summary>
    /// 更新圆形障碍生命周期，并提交圆内敌人范围搜索。
    /// </summary>
    public override void FlyObjectUpdate(float dt)
    {
        if (destroyed)
        {
            return;
        }

        if (mAtkUnitLogic == null || mAtkUnitLogic.IsDead)
        {
            Die();
            return;
        }

        if (mFlyObjectCfg.triggerDamageCount < 0
            || triggeredDamageCount < mFlyObjectCfg.triggerDamageCount)
        {
            damageClipTime -= dt;
            if (damageClipTime <= 0f && !damagePending)
            {
                searchRequest = MapCellManager.Instance.RequestSearch(
                    circleCenter,
                    circleRadius,
                    mAtkUnitLogic.OtherCampTypeInt);
                damagePending = searchRequest >= 0;
                damageClipTime = Mathf.Max(0.02f, mFlyObjectCfg.damClipTime);
            }
        }

        liveTime -= dt;
        if (liveTime <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// 读取圆形范围搜索结果，并对范围内敌人结算一次技能伤害。
    /// </summary>
    public override void FlyObjectLateUpdate(float dt)
    {
        if (destroyed || !damagePending)
        {
            return;
        }

        resultIndices.Clear();
        nearestIndex = -1;
        randomIndex = -1;
        MapCellManager.Instance.GetResult(searchRequest, resultIndices, ref nearestIndex, ref randomIndex);
        UnitTargetSnapshot.Capture(resultIndices, resultUids);
        damagePending = false;
        triggeredDamageCount++;

        for (int i = 0; i < resultIndices.Count; i++)
        {
            if (!UnitTargetSnapshot.TryGet(resultIndices, resultUids, i, out UnitLogicBase target)
                || target == null || target.IsDead
                || target.CampTypeInt == mAtkUnitLogic.CampTypeInt)
            {
                continue;
            }

            BattleLogicDamageTools.DoDamage(
                mAtkUnitLogic,
                target,
                GetFinalDamage(target),
                target.UId,
                mSkillLogic.SkillCfg.dieType,
                target.CurPos);
        }

        DamageEnemyWallInArea(circleCenter, circleRadius);
    }

    /// <summary>
    /// 按固定20个顶点生成XZ平面的圆形障碍。
    /// </summary>
    private static List<Vector3> CreateCircleVertices(Vector3 center, float radius)
    {
        List<Vector3> vertices = new List<Vector3>(CirclePointCount);
        for (int i = 0; i < CirclePointCount; i++)
        {
            float angle = Mathf.PI * 2f * i / CirclePointCount;
            vertices.Add(center + new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius));
        }

        return vertices;
    }

    /// <summary>
    /// 解析飞行器flyParam中的圆形半径。
    /// </summary>
    private static bool TryParseRadius(string value, out float radius)
    {
        radius = 0f;
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out radius)
            && radius > 0f
            && !float.IsNaN(radius)
            && !float.IsInfinity(radius);
    }

    /// <summary>
    /// 移除圆形动态障碍、回收表现对象并销毁飞行器逻辑。
    /// </summary>
    public override void ClearForBattle()
    {
        Die();
    }

    private void Die()
    {
        if (destroyed)
        {
            return;
        }

        destroyed = true;
        if (obstacle != null)
        {
            RvoManager.Instance.RemoveDynamicObstacle(obstacle);
            obstacle = null;
        }

        if (circleGob != null)
        {
            UnitViewFactory.RemoveGob(mFlyObjectCfg.prefab, circleGob);
            circleGob = null;
        }

        UnitFactory.RemoveFlyObjectLogic(this);
    }
}
