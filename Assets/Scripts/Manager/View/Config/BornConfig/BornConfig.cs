
using System.Collections.Generic;
using UnityEngine;

public class BornConfig : MonoBehaviour
{
    // 红方出生点
    public List<Transform> RedPoints = new List<Transform>();
    // 蓝方
    public List<Transform> BluePoints = new List<Transform>();

    public Vector3 GetBornPoint(ECampType campType)
    {
        if (campType == ECampType.Red)
        {
            if (RedPoints.Count > 0)
            {
                return RedPoints[0].position;
            }
        }
        if (campType == ECampType.Blue)
        {
            if (BluePoints.Count > 0)
            {
                return BluePoints[0].position;
            }
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
