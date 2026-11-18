using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// 使用 LineRenderer 连接释放点和多个受击点的追踪激光飞行器。
/// 适用于按 damClipTime 周期重新搜索敌人、动态更新激光端点并同步创建受击特效的场景。
/// </summary>
public class LineRenderFlyObject : FlyObjectLogicBase
{
    private const float ReleasePointHeightOffset = 1.5f;

    private sealed class TargetPoint
    {
        public UnitLogicBase Target;
        public int TargetUid;
        public Vector3 Position;
        public GameObject ArriveEffect;
        public bool PositionLocked;
    }

    private readonly List<TargetPoint> targetPointList = new List<TargetPoint>();
    private readonly List<GameObject> arriveEffectList = new List<GameObject>();
    private readonly List<int> resultUnitIndexList = new List<int>();
    private readonly List<int> resultUnitUidList = new List<int>();

    private LineRenderer lineRenderer;
    private float liveTime;
    private float damageClipTime;
    private const float TargetPointHeightOffset = 0.5f;
    private int maxTargetCount;
    private int searchRequestIndex = -1;
    private int nearestIndex = -1;
    private int randomIndex = -1;
    private bool damagePending;
    private bool destroyed;

    /// <summary>
    /// 初始化激光飞行器、解析最大受击人数并创建初始释放点。
    /// </summary>
    public override void SetFlyObjectInfo(
        FlyObjectCfg flyObjectCfg,
        Vector3 oriPos,
        Vector3 tarPos,
        UnitLogicBase atkUnitLogic,
        List<UnitLogicBase> targetLogicList,
        UnitLogicBase searchTargetLogic,
        SkillLogicBase skillLogic,
        int attackDamage,
        int flyUIndex)
    {
        base.SetFlyObjectInfo(
            flyObjectCfg,
            oriPos,
            tarPos,
            atkUnitLogic,
            targetLogicList,
            searchTargetLogic,
            skillLogic,
            attackDamage,
            flyUIndex);

        if (!TryParseFlyParam(flyObjectCfg.flyParam, out maxTargetCount))
        {
            Debug.LogError($"LineRender flyParam 无效，FlyObjectId={flyObjectCfg.id}，flyParam={flyObjectCfg.flyParam}");
            Die();
            return;
        }

        liveTime = flyObjectCfg.liveTime;
        damageClipTime = flyObjectCfg.damClipTime;
        damagePending = false;
        destroyed = false;
        searchRequestIndex = -1;
        targetPointList.Clear();
        arriveEffectList.Clear();
        resultUnitIndexList.Clear();
        resultUnitUidList.Clear();

        mFlyObjectGob = UnitViewFactory.CreateGob(flyObjectCfg.prefab, mOriPos, Vector3.forward);
        lineRenderer = mFlyObjectGob.GetOrAddComponentInChild<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        RebuildTargetPoints();
    }

    /// <summary>
    /// 更新激光生命周期、追踪目标点位并按 damClipTime 发起下一次范围搜索。
    /// </summary>
    public override void FlyObjectUpdate(float dt)
    {
        if (destroyed)
        {
            return;
        }

        UpdateTargetPoints();

        damageClipTime -= dt;
        if (damageClipTime <= 0f && !damagePending)
        {
            searchRequestIndex = RequestTargetSearch();
            damagePending = true;
        }

        liveTime -= dt;
        if (liveTime <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// 读取范围搜索结果，重新规划激光点位、重建受击特效并结算本次伤害。
    /// </summary>
    public override void FlyObjectLateUpdate(float dt)
    {
        if (destroyed || !damagePending)
        {
            return;
        }

        MapCellManager.Instance.GetResult(
            searchRequestIndex,
            resultUnitIndexList,
            ref nearestIndex,
            ref randomIndex);
        UnitTargetSnapshot.Capture(resultUnitIndexList, resultUnitUidList);

        RebuildTargetPoints();
        DamageTargets();

        damageClipTime = mFlyObjectCfg.damClipTime;
        damagePending = false;
        searchRequestIndex = -1;
    }

    /// <summary>
    /// 按技能范围向敌方阵营请求一次目标搜索。
    /// </summary>
    private int RequestTargetSearch()
    {
        return MapCellManager.Instance.RequestSearch(
            GetSearchCenter(),
            mSkillLogic.SkillCfg.skillArea,
            mAtkUnitLogic.OtherCampTypeInt);
    }

    /// <summary>
    /// 获取本次范围搜索中心；初始目标存活时让搜索中心跟随初始目标移动。
    /// </summary>
    private Vector3 GetSearchCenter()
    {
        if (mSearchTargetUnitLogic != null && !mSearchTargetUnitLogic.IsDead)
        {
            mTarPos = mSearchTargetUnitLogic.CurPos;
        }

        return mTarPos;
    }

    /// <summary>
    /// 更新仍然存活目标的点位和对应受击特效，死亡目标保持最后位置。
    /// </summary>
    private void UpdateTargetPoints()
    {
        for (int i = 0; i < targetPointList.Count; i++)
        {
            TargetPoint targetPoint = targetPointList[i];
            if (!targetPoint.PositionLocked
                && targetPoint.Target != null
                && !targetPoint.Target.IsDead
                && targetPoint.Target.UId == targetPoint.TargetUid)
            {
                targetPoint.Position = GetTargetPointPosition(targetPoint.Target);
            }
            else if (targetPoint.Target != null)
            {
                targetPoint.PositionLocked = true;
            }

            if (targetPoint.ArriveEffect != null && !targetPoint.PositionLocked)
            {
                targetPoint.ArriveEffect.transform.position = targetPoint.Position;
            }
        }

        UpdateLineRendererPositions();
    }

    /// <summary>
    /// 清理旧点位并根据本次搜索结果创建释放点和受击点。
    /// </summary>
    private void RebuildTargetPoints()
    {
        ReleaseArriveEffects();
        targetPointList.Clear();

        TargetPoint releasePoint = new TargetPoint
        {
            Position = GetReleasePointPosition(),
            ArriveEffect = CreateLinePointEffect(GetReleasePointPosition())
        };
        targetPointList.Add(releasePoint);

        int createdTargetCount = 0;
        for (int i = 0; i < resultUnitIndexList.Count && createdTargetCount < maxTargetCount; i++)
        {
            if (!UnitTargetSnapshot.TryGet(
                    resultUnitIndexList,
                    resultUnitUidList,
                    i,
                    out UnitLogicBase target)
                || target.CampTypeInt == mAtkUnitLogic.CampTypeInt)
            {
                continue;
            }

            Vector3 targetPosition = GetTargetPointPosition(target);
            TargetPoint targetPoint = new TargetPoint
            {
                Target = target,
                TargetUid = target.UId,
                Position = targetPosition,
                ArriveEffect = CreateLinePointEffect(targetPosition)
            };
            targetPointList.Add(targetPoint);
            createdTargetCount++;
        }

        UpdateLineRendererPositions();
    }

    /// <summary>
    /// 更新 LineRenderer 的点数量和所有世界坐标点位。
    /// </summary>
    private void UpdateLineRendererPositions()
    {
        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.positionCount = targetPointList.Count;
        if (targetPointList.Count == 0)
        {
            return;
        }

        Vector3[] positions = new Vector3[targetPointList.Count];
        for (int i = 0; i < targetPointList.Count; i++)
        {
            positions[i] = targetPointList[i].Position;
        }

        lineRenderer.SetPositions(positions);
    }

    /// <summary>
    /// 对本次重新规划得到的存活敌人结算伤害和技能 Buff。
    /// </summary>
    private void DamageTargets()
    {
        for (int i = 1; i < targetPointList.Count; i++)
        {
            TargetPoint targetPoint = targetPointList[i];
            if (targetPoint.Target == null
                || targetPoint.Target.IsDead
                || targetPoint.Target.UId != targetPoint.TargetUid)
            {
                continue;
            }

            BattleLogicDamageTools.DoDamage(
                mAtkUnitLogic,
                targetPoint.Target,
                GetFinalDamage(targetPoint.Target),
                targetPoint.TargetUid,
                mSkillLogic.SkillCfg.dieType,
                targetPoint.Position);
            mSkillLogic.ApplyBuffs(targetPoint.Target);
        }

        DamageEnemyWallInArea(GetSearchCenter(), mSkillLogic.SkillCfg.skillArea);
    }

    /// <summary>
    /// 创建一个受击或释放点特效并加入本飞行器的回收列表。
    /// </summary>
    private GameObject CreateLinePointEffect(Vector3 position)
    {
        if (string.IsNullOrWhiteSpace(mFlyObjectCfg.arrivePrefab))
        {
            return null;
        }

        GameObject effect = UnitViewFactory.CreateGob(mFlyObjectCfg.arrivePrefab, position, Vector3.forward);
        if (effect != null)
        {
            arriveEffectList.Add(effect);
        }

        return effect;
    }

    /// <summary>
    /// 回收本次规划创建的全部受击特效，供下一次 damClipTime 重建或生命周期结束清理。
    /// </summary>
    private void ReleaseArriveEffects()
    {
        for (int i = 0; i < arriveEffectList.Count; i++)
        {
            GameObject effect = arriveEffectList[i];
            if (effect != null)
            {
                UnitViewFactory.RemoveGob(mFlyObjectCfg.arrivePrefab, effect);
            }
        }

        arriveEffectList.Clear();
    }

    /// <summary>
    /// 获取固定高度偏移后的释放点坐标。
    /// </summary>
    private Vector3 GetReleasePointPosition()
    {
        return mOriPos + Vector3.up * ReleasePointHeightOffset;
    }

    /// <summary>
    /// 获取目标单位固定高度偏移后的追踪点坐标。
    /// </summary>
    private Vector3 GetTargetPointPosition(UnitLogicBase target)
    {
        return target.CurPos + Vector3.up * TargetPointHeightOffset;
    }

    /// <summary>
    /// 回收 LineRenderer 飞行器、全部受击特效和对应的飞行器逻辑。
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
        ReleaseArriveEffects();
        targetPointList.Clear();

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
        }

        UnitFactory.RemoveFlyObjectLogic(this);
        if (mFlyObjectGob != null)
        {
            UnitViewFactory.RemoveGob(mFlyObjectCfg.prefab, mFlyObjectGob);
            mFlyObjectGob = null;
        }
    }

    /// <summary>
    /// 从 flyParam 解析最大受击人数；lineRender 的 flyParam 只允许配置一个整数参数。
    /// </summary>
    private static bool TryParseFlyParam(string value, out int count)
    {
        count = 0;
        return int.TryParse(
                   (value ?? string.Empty).Trim(),
                   NumberStyles.Integer,
                   CultureInfo.InvariantCulture,
                   out count)
            && count > 0;
    }
}
