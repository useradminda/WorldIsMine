
using System;
using UnityEngine;

public class BornConfig : MonoBehaviour
{
    // 红方出生点
    public Transform RedPoint;
    // 蓝方
    public Transform BluePoint;

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
        Transform opponentPoint = GetPoint(
            campType == ECampType.Red ? ECampType.Blue : ECampType.Red);
        if (ownPoint != null && opponentPoint != null)
        {
            Vector3 forward = opponentPoint.position - ownPoint.position;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.0001f)
                return forward.normalized;
        }

        return campType == ECampType.Red
            ? Vector3.forward
            : Vector3.back;
    }

    public bool HasDistinctSpawnPoints(float minimumDistance, out string reason)
    {
        if (RedPoint == null || BluePoint == null)
        {
            reason = "RedPoint and BluePoint must both be assigned.";
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
