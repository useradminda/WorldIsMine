using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public class UnitManager : MonoBehaviour
{
    public GameObject HeroPrefab;

    public int Count = 70000;

    NativeArray<float3> positions;

    TransformAccessArray transforms;
 
}
