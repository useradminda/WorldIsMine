using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
// 障碍物配置
public class ObstacleConfig : MonoBehaviour
{
    [Header("边界配置")]
    public List<Vector3> BorderList;

}
