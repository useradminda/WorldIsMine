using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// 多发随机贝塞尔轨迹追踪飞行器。
/// 适用于多发追踪弹或曲线弹幕，flyParam 配置发射数量和发射间隔，每发独立追踪同一目标。
/// </summary>
public class PointToPointFlyObject : FlyObjectLogicBase
{
    // 贝塞尔控制点相对起止线的横向最小偏移，-10 表示向一侧偏移 10 米。
    private const float ConfigControlSideMin = -10f;
    // 贝塞尔控制点相对起止线的横向最大偏移，10 表示向另一侧偏移 10 米。
    private const float ConfigControlSideMax = 10f;
    // 贝塞尔控制点的最小向上高度，单位为世界坐标米。
    private const float ConfigControlHeightMin = 1f;
    // 贝塞尔控制点的最大向上高度，单位为世界坐标米。
    private const float ConfigControlHeightMax = 8f;

    private sealed class Projectile
    {
        public GameObject gob;
        public float delay;
        public float progress;
        public Vector3 start;
        public Vector3 control;
        public UnitLogicBase target;
        public int targetIndex = -1;
        public int targetUid = -1;
        public Vector3 lastTargetPosition;
        public int searchRequestIndex = -1;
        public bool searchPending;
        public bool targetAssigned;
        public bool flying;
        public bool completed;
    }

    private readonly List<Projectile> projectiles = new List<Projectile>();
    private float launchInterval;
    private float controlSideMin;
    private float controlSideMax;
    private float controlHeightMin;
    private float controlHeightMax;
    private float elapsed;
    private bool destroyed;
    private readonly HashSet<int> assignedTargetUids = new HashSet<int>();
    private readonly List<int> searchResultList = new List<int>();
    private int nearestIndex = -1;
    private int randomIndex = -1;

    public override void SetFlyObjectInfo(
        FlyObjectCfg flyObjectCfg, Vector3 oriPos, Vector3 tarPos,
        UnitLogicBase atkUnitLogic, List<UnitLogicBase> targetLogicList,
        UnitLogicBase searchTargetLogic, SkillLogicBase skillLogic,
        int attackDamage, int flyUIndex)
    {
        base.SetFlyObjectInfo(flyObjectCfg, oriPos, tarPos, atkUnitLogic,
            targetLogicList, searchTargetLogic, skillLogic, attackDamage, flyUIndex);
        projectiles.Clear();
        elapsed = 0f;
        destroyed = false;
        assignedTargetUids.Clear();
        searchResultList.Clear();

        if (!TryParseFlyParam(
            flyObjectCfg.flyParam,
            out int count,
            out launchInterval,
            out controlSideMin,
            out controlSideMax,
            out controlHeightMin,
            out controlHeightMax))
        {
            Debug.LogError($"PointToPoint flyParam is invalid: {flyObjectCfg.flyParam}");
            Die();
            return;
        }

        // 第一发使用攻击状态已经锁定的目标，后续飞弹在自己的发射时刻独立搜索。
        for (int i = 0; i < count; i++)
        {
            Projectile projectile = new Projectile
            {
                delay = i * launchInterval,
                start = oriPos,
                flying = false
            };
            if (i == 0 && searchTargetLogic != null && !searchTargetLogic.IsDead)
            {
                projectile.target = searchTargetLogic;
                projectile.targetIndex = searchTargetLogic.Index;
                projectile.targetUid = searchTargetLogic.UId;
                projectile.targetAssigned = true;
                projectile.lastTargetPosition = searchTargetLogic.CurPos;
                projectile.control = CreateControlPoint(
                    oriPos,
                    searchTargetLogic.CurPos,
                    controlSideMin,
                    controlSideMax,
                    controlHeightMin,
                    controlHeightMax);
            }
            projectiles.Add(projectile);
        }
    }

    public override void FlyObjectUpdate(float dt)
    {
        if (destroyed) return;
        elapsed += dt;
        float speed = Mathf.Max(0.01f, mFlyObjectCfg.speed > 0f ? mFlyObjectCfg.speed : 20f);
        bool anyAlive = false;
        for (int i = 0; i < projectiles.Count; i++)
        {
            Projectile projectile = projectiles[i];
            if (projectile.completed) continue;
            if (!projectile.targetAssigned && !projectile.searchPending && elapsed >= projectile.delay)
            {
                projectile.searchRequestIndex = RequestProjectileTargetSearch();
                projectile.searchPending = projectile.searchRequestIndex >= 0;
                if (!projectile.searchPending)
                {
                    projectile.completed = true;
                }
            }
            if (!projectile.targetAssigned)
            {
                if (!projectile.completed)
                {
                    anyAlive = true;
                }
                continue;
            }

            if (!IsProjectileTargetValid(projectile))
            {
                projectile.completed = true;
                continue;
            }

            if (projectile.gob == null)
            {
                projectile.gob = UnitViewFactory.CreateGob(mFlyObjectCfg.prefab, projectile.start, Vector3.forward);
                projectile.flying = true;
            }

            anyAlive = true;
            bool targetValid = IsProjectileTargetValid(projectile);
            if (targetValid)
            {
                projectile.lastTargetPosition = projectile.target.CurPos;
            }
            Vector3 targetPos = targetValid ? projectile.target.CurPos : projectile.lastTargetPosition;
            float length = Vector3.Distance(projectile.start, targetPos);
            projectile.progress += dt * speed / Mathf.Max(0.1f, length);
            float t = Mathf.Clamp01(projectile.progress);
            Vector3 position = Bezier(projectile.start, projectile.control, targetPos, t);
            projectile.gob.transform.position = position;
            Vector3 direction = Bezier(projectile.start, projectile.control, targetPos, Mathf.Min(1f, t + 0.01f)) - position;
            if (direction.sqrMagnitude > 0.0001f) projectile.gob.transform.rotation = Quaternion.LookRotation(direction);
            if (t >= 1f)
            {
                if (targetValid)
                {
                    BattleLogicDamageTools.DoDamage(mAtkUnitLogic, projectile.target,
                        GetFinalDamage(projectile.target), projectile.targetUid, mSkillLogic.SkillCfg.dieType, position);
                    mSkillLogic.ApplyBuffs(projectile.target);
                }
                UnitViewFactory.RemoveGob(mFlyObjectCfg.prefab, projectile.gob);
                projectile.gob = null;
                projectile.flying = false;
                projectile.completed = true;
            }
        }
        if (!anyAlive) Die();
    }

    /// <summary>
    /// 读取每颗飞弹独立提交的目标搜索结果，并为其分配未被占用的敌人。
    /// </summary>
    public override void FlyObjectLateUpdate(float dt)
    {
        if (destroyed)
        {
            return;
        }

        for (int i = 0; i < projectiles.Count; i++)
        {
            Projectile projectile = projectiles[i];
            if (projectile.completed || !projectile.searchPending)
            {
                continue;
            }

            searchResultList.Clear();
            nearestIndex = -1;
            randomIndex = -1;
            MapCellManager.Instance.GetResult(
                projectile.searchRequestIndex,
                searchResultList,
                ref nearestIndex,
                ref randomIndex);
            projectile.searchPending = false;

            UnitLogicBase target = FindUnusedTarget();
            if (target == null)
            {
                projectile.completed = true;
                continue;
            }

            projectile.target = target;
            projectile.targetIndex = target.Index;
            projectile.targetUid = target.UId;
            projectile.lastTargetPosition = target.CurPos;
            projectile.targetAssigned = true;
            projectile.control = CreateControlPoint(
                projectile.start,
                target.CurPos,
                controlSideMin,
                controlSideMax,
                controlHeightMin,
                controlHeightMax);
            assignedTargetUids.Add(target.UId);
        }
    }

    /// <summary>
    /// 为当前飞弹提交一次独立敌人搜索请求。
    /// </summary>
    private int RequestProjectileTargetSearch()
    {
        float searchRange = mSkillLogic != null && mSkillLogic.SkillCfg != null
            ? Mathf.Max(0.1f, mSkillLogic.SkillCfg.searchRange)
            : 60f;
        return MapCellManager.Instance.RequestSearch(
            mAtkUnitLogic.CurPos,
            searchRange,
            mAtkUnitLogic.OtherCampTypeInt);
    }

    /// <summary>
    /// 从当前飞弹的搜索结果中选择尚未被其他飞弹占用的存活敌人。
    /// </summary>
    private UnitLogicBase FindUnusedTarget()
    {
        for (int i = 0; i < searchResultList.Count; i++)
        {
            int unitIndex = searchResultList[i];
            if (unitIndex < 0 || unitIndex >= UnitManager.Instance.UnitList.Count)
            {
                continue;
            }

            UnitLogicBase target = UnitManager.Instance.UnitList[unitIndex];
            if (target == null || target.IsDead || target.CampType == mAtkUnitLogic.CampType
                || assignedTargetUids.Contains(target.UId))
            {
                continue;
            }

            return target;
        }

        return null;
    }

    /// <summary>
    /// 校验飞弹目标仍是发射时记录的同一个单位，避免槽位回收复用后误伤新单位。
    /// </summary>
    private static bool IsProjectileTargetValid(Projectile projectile)
    {
        if (projectile.target == null || projectile.target.IsDead || projectile.target.UId != projectile.targetUid)
        {
            return false;
        }

        // 城门是独立逻辑对象，不在普通 UnitList 中，使用对象引用和 UID 校验即可。
        if (projectile.target.UnitType == EUnitType.Wall)
        {
            return true;
        }

        return projectile.targetIndex >= 0
            && projectile.targetIndex < UnitManager.Instance.UnitList.Count
            && UnitManager.Instance.UnitList[projectile.targetIndex] == projectile.target;
    }

    private void Die()
    {
        if (destroyed) return;
        destroyed = true;
        for (int i = 0; i < projectiles.Count; i++)
        {
            if (projectiles[i].gob != null)
                UnitViewFactory.RemoveGob(mFlyObjectCfg.prefab, projectiles[i].gob);
        }
        projectiles.Clear();
        UnitFactory.RemoveFlyObjectLogic(this);
    }

    /// <summary>
    /// 为单发飞弹创建独立的随机贝塞尔控制点，控制横向偏移和抛物线高度。
    /// </summary>
    private static Vector3 CreateControlPoint(
        Vector3 start,
        Vector3 target,
        float sideMin,
        float sideMax,
        float heightMin,
        float heightMax)
    {
        Vector3 direction = target - start;
        Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;
        if (side.sqrMagnitude < 0.001f) side = Vector3.right;
        return Vector3.Lerp(start, target, UnityEngine.Random.Range(0.25f, 0.75f))
            + side * UnityEngine.Random.Range(sideMin, sideMax)
            + Vector3.up * UnityEngine.Random.Range(heightMin, heightMax);
    }

    private static Vector3 Bezier(Vector3 start, Vector3 control, Vector3 end, float t)
    {
        float inverse = 1f - t;
        return inverse * inverse * start + 2f * inverse * t * control + t * t * end;
    }

    /// <summary>
    /// 解析飞弹数量和发射间隔，贝塞尔幅度使用脚本顶部配置。
    /// </summary>
    private static bool TryParseFlyParam(
        string value,
        out int count,
        out float interval,
        out float sideMin,
        out float sideMax,
        out float heightMin,
        out float heightMax)
    {
        count = 0;
        interval = 0f;
        sideMin = ConfigControlSideMin;
        sideMax = ConfigControlSideMax;
        heightMin = ConfigControlHeightMin;
        heightMax = ConfigControlHeightMax;
        string[] values = (value ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        bool baseValid = values.Length == 2
            && int.TryParse(values[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out count)
            && float.TryParse(values[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out interval)
            && count > 0 && interval >= 0f;
        if (!baseValid)
        {
            return false;
        }

        return true;
    }
}
