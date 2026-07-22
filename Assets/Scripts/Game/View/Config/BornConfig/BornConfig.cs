
using System.Collections.Generic;
using UnityEngine;

public class BornConfig : MonoBehaviour
{
    // 红方出生点
    public Transform RedPoint;
    // 蓝方
    public Transform BluePoint;

    public Vector3 GetBornPoint(ECampType campType)
    {
        if (campType == ECampType.Red)
        {
            return RedPoint.position;
        }
        if (campType == ECampType.Blue)
        {
            return BluePoint.position;
        }
        return Vector3.zero;
    }

    public Vector3 GetForward(ECampType campType)
    {
        if (campType == ECampType.Red)
        {
            return new Vector3(0, 0, 1);
        }
        return new Vector3(0, 0, -1);
    }
}
