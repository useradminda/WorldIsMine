
using System;
using UnityEngine;

public class BornConfig : MonoBehaviour
{
    // 红方出生点
    public Transform RedPoint;

    public Transform BluePoint;

    public Transform RedCityDoorPoint;
    public Transform BlueCityDoorPoint;

    public Transform RedCityLogicPoint;
    public Transform BlueCityLogicPoint;
    public Vector3 GetBornPoint(ECampType campType)
    {
        Transform point = GetPoint(campType);
        return point.position;
    }

    public Vector3 GetForward(ECampType campType)
    {
        if (campType == ECampType.Blue)
        {
            return new Vector3(0, 0, -1);
        }
        return new Vector3(0, 0, 1);
    }

    public Vector3 GetCityDoorPoint(ECampType campType)
    {
        Transform point = campType == ECampType.Red ? RedCityDoorPoint : BlueCityDoorPoint;
        if (point != null)
            return point.position;
        return GetBornPoint(campType);
    }

    public Vector3 GetCityDoorForward(ECampType campType)
    {
        Transform point = campType == ECampType.Red ? RedCityDoorPoint : BlueCityDoorPoint;
        if (point != null && point.forward.sqrMagnitude > 0.0001f)
            return point.forward.normalized;
        return GetForward(campType);
    }

   
    private Transform GetPoint(ECampType campType)
    {
        switch (campType)
        {
            case ECampType.Red:
                return RedPoint;
            case ECampType.Blue:
                return BluePoint;
                
        }
        return null;
    }
}
