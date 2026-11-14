using System;
using System.Collections.Generic;
using System.Globalization;
using Nebukam.ORCA;
using UnityEngine;

/// <summary>
/// 临时矩形 RVO 障碍飞行器。
/// 适用于创建城墙或阻挡区域；长度、宽度和前方距离从 flyParam 读取，并在生命周期结束时移除对应的 RVO 障碍和表现对象。
/// </summary>
public class CreateWallFlyObject : FlyObjectLogicBase
{
    private Obstacle obstacle;
    private float liveTime;
    private GameObject wallGob;
    private bool destroyed;

    public override void SetFlyObjectInfo(
        FlyObjectCfg flyObjectCfg, Vector3 oriPos, Vector3 tarPos,
        UnitLogicBase atkUnitLogic, List<UnitLogicBase> targetLogicList,
        UnitLogicBase searchTargetLogic, SkillLogicBase skillLogic,
        int attackDamage, int flyUIndex)
    {
        base.SetFlyObjectInfo(flyObjectCfg, oriPos, tarPos, atkUnitLogic,
            targetLogicList, searchTargetLogic, skillLogic, attackDamage, flyUIndex);
        liveTime = flyObjectCfg.liveTime;

        if (!TryParseFlyParam(flyObjectCfg != null ? flyObjectCfg.flyParam : string.Empty,
                out float length, out float width, out float forwardDistance))
        {
            Debug.LogError($"Create wall flyParam is invalid. FlyObjectId={flyObjectCfg?.id}");
            Die();
            return;
        }

        Vector3 forward = atkUnitLogic.TargetForward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.0001f) forward = Vector3.forward;
        forward.Normalize();
        Vector3 center = atkUnitLogic.CurPos + forward * forwardDistance;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        float halfLength = length * 0.5f;
        float halfWidth = width * 0.5f;
        List<Vector3> vertices = new List<Vector3>(4)
        {
            center - right * halfLength - forward * halfWidth,
            center + right * halfLength - forward * halfWidth,
            center + right * halfLength + forward * halfWidth,
            center - right * halfLength + forward * halfWidth
        };
        obstacle = RvoManager.Instance.AddDynamicObstacle(vertices);
        wallGob = UnitViewFactory.CreateGob(flyObjectCfg.prefab, center, forward);
    }

    public override void FlyObjectUpdate(float dt)
    {
        if (destroyed) return;
        if (mAtkUnitLogic == null || mAtkUnitLogic.IsDead) { Die(); return; }
        liveTime -= dt;
        if (liveTime <= 0f) Die();
    }

    private void Die()
    {
        if (destroyed) return;
        destroyed = true;
        if (obstacle != null) { RvoManager.Instance.RemoveDynamicObstacle(obstacle); obstacle = null; }
        if (wallGob != null) { UnitViewFactory.RemoveGob(mFlyObjectCfg.prefab, wallGob); wallGob = null; }
        UnitFactory.RemoveFlyObjectLogic(this);
    }

    private static bool TryParseFlyParam(string value, out float length, out float width, out float distance)
    {
        length = width = distance = 0f;
        string[] parts = (value ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 3
            && TryPositive(parts[0], out length)
            && TryPositive(parts[1], out width)
            && TryNonNegative(parts[2], out distance);
    }

    private static bool TryPositive(string value, out float result) => TryNonNegative(value, out result) && result > 0f;

    private static bool TryNonNegative(string value, out float result) =>
        float.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result) && result >= 0f;
}
