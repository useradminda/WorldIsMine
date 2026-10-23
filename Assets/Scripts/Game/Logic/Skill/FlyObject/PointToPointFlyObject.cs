using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>Spawns several homing projectiles, each following its own random Bezier path.</summary>
public class PointToPointFlyObject : FlyObjectLogicBase
{
    private sealed class Projectile
    {
        public GameObject gob;
        public float delay;
        public float progress;
        public Vector3 start;
        public Vector3 control;
        public bool flying;
        public bool completed;
    }

    private readonly List<Projectile> projectiles = new List<Projectile>();
    private int targetUid = -1;
    private float launchInterval;
    private float elapsed;
    private bool destroyed;

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
        targetUid = searchTargetLogic != null ? searchTargetLogic.UId : -1;

        if (!IsTargetValid()) { Die(); return; }
        if (!TryParseFlyParam(flyObjectCfg.flyParam, out int count, out launchInterval))
        {
            Debug.LogError($"PointToPoint flyParam is invalid: {flyObjectCfg.flyParam}");
            Die();
            return;
        }

        for (int i = 0; i < count; i++)
        {
            projectiles.Add(new Projectile
            {
                delay = i * launchInterval,
                start = oriPos,
                control = CreateControlPoint(oriPos, searchTargetLogic.CurPos),
                flying = false
            });
        }
    }

    public override void FlyObjectUpdate(float dt)
    {
        if (destroyed) return;
        if (!IsTargetValid()) { Die(); return; }

        elapsed += dt;
        float speed = Mathf.Max(0.01f, mFlyObjectCfg.speed > 0f ? mFlyObjectCfg.speed : 20f);
        bool anyAlive = false;
        for (int i = 0; i < projectiles.Count; i++)
        {
            Projectile projectile = projectiles[i];
            if (projectile.completed) continue;
            if (projectile.gob == null && elapsed >= projectile.delay)
            {
                projectile.gob = UnitViewFactory.CreateGob(mFlyObjectCfg.prefab, projectile.start, Vector3.forward);
                projectile.flying = true;
            }
            if (!projectile.flying) { if (projectile.gob == null && elapsed < projectile.delay) anyAlive = true; continue; }

            anyAlive = true;
            Vector3 targetPos = mSearchTargetUnitLogic.CurPos;
            float length = Vector3.Distance(projectile.start, targetPos);
            projectile.progress += dt * speed / Mathf.Max(0.1f, length);
            float t = Mathf.Clamp01(projectile.progress);
            Vector3 position = Bezier(projectile.start, projectile.control, targetPos, t);
            projectile.gob.transform.position = position;
            Vector3 direction = Bezier(projectile.start, projectile.control, targetPos, Mathf.Min(1f, t + 0.01f)) - position;
            if (direction.sqrMagnitude > 0.0001f) projectile.gob.transform.rotation = Quaternion.LookRotation(direction);
            if (t >= 1f)
            {
                BattleLogicDamageTools.DoDamage(mAtkUnitLogic, mSearchTargetUnitLogic,
                    GetFinalDamage(mSearchTargetUnitLogic), targetUid, mSkillLogic.SkillCfg.dieType, position);
                mSkillLogic.ApplyBuffs(mSearchTargetUnitLogic);
                UnitViewFactory.RemoveGob(mFlyObjectCfg.prefab, projectile.gob);
                projectile.gob = null;
                projectile.flying = false;
                projectile.completed = true;
            }
        }
        if (!anyAlive) Die();
    }

    private bool IsTargetValid()
    {
        return mSearchTargetUnitLogic != null && !mSearchTargetUnitLogic.IsDead
            && mSearchTargetUnitLogic.UId == targetUid;
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

    private static Vector3 CreateControlPoint(Vector3 start, Vector3 target)
    {
        Vector3 direction = target - start;
        Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;
        if (side.sqrMagnitude < 0.001f) side = Vector3.right;
        return Vector3.Lerp(start, target, UnityEngine.Random.Range(0.25f, 0.75f))
            + side * UnityEngine.Random.Range(-3f, 3f) + Vector3.up * UnityEngine.Random.Range(0.5f, 2.5f);
    }

    private static Vector3 Bezier(Vector3 start, Vector3 control, Vector3 end, float t)
    {
        float inverse = 1f - t;
        return inverse * inverse * start + 2f * inverse * t * control + t * t * end;
    }

    private static bool TryParseFlyParam(string value, out int count, out float interval)
    {
        count = 0;
        interval = 0f;
        string[] values = (value ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        return values.Length == 2
            && int.TryParse(values[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out count)
            && float.TryParse(values[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out interval)
            && count > 0 && interval >= 0f;
    }
}
