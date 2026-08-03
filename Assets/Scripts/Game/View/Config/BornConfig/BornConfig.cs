
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
