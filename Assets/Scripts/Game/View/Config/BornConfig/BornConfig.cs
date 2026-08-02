
using System;
using UnityEngine;

public class BornConfig : MonoBehaviour
{
    // 红方出生点
    public Transform RedPoint;
    // 蓝方
    public Transform BluePoint;
    // 红蓝双方共同目标点
    public Transform TargetPoint;

    public Vector3 GetBornPoint(ECampType campType)
    {
        Transform point = GetPoint(campType);
        if (point == null)
        {
            throw new InvalidOperationException(
                $"Spawn point is not assigned. Camp={campType}");
        }

        return point.position;
    }

    public Vector3 GetForward(ECampType campType)
    {
        Transform ownPoint = GetPoint(campType);
        if (ownPoint != null && TargetPoint != null)
        {
            Vector3 forward = TargetPoint.position - ownPoint.position;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.0001f)
                return forward.normalized;
        }

        return campType == ECampType.Red
            ? Vector3.forward
            : Vector3.back;
    }

    public Vector3 GetTargetPoint()
    {
        if (TargetPoint == null)
            throw new InvalidOperationException("TargetPoint is not assigned.");

        return TargetPoint.position;
    }

    public bool HasDistinctSpawnPoints(float minimumDistance, out string reason)
    {
        if (RedPoint == null || BluePoint == null || TargetPoint == null)
        {
            reason = "RedPoint, BluePoint and TargetPoint must all be assigned.";
            return false;
        }

        Vector3 red = RedPoint.position;
        Vector3 blue = BluePoint.position;
        red.y = 0f;
        blue.y = 0f;
        if (Vector3.Distance(red, blue) < Mathf.Max(0.01f, minimumDistance))
        {
            reason = $"Spawn points are too close. Red={red}, Blue={blue}";
            return false;
        }

        Vector3 target = TargetPoint.position;
        target.y = 0f;
        if (Vector3.Distance(red, target) < 0.01f ||
            Vector3.Distance(blue, target) < 0.01f)
        {
            reason = $"A spawn point overlaps the target. Red={red}, Blue={blue}, Target={target}";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private Transform GetPoint(ECampType campType)
    {
        switch (campType)
        {
            case ECampType.Red:
                return RedPoint;
            case ECampType.Blue:
                return BluePoint;
            default:
                throw new ArgumentOutOfRangeException(nameof(campType), campType, null);
        }
    }
}
