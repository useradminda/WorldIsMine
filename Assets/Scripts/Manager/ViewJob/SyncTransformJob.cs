using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[BurstCompile]
public struct SyncTransformJob
    : IJobParallelForTransform
{
    [ReadOnly]
    public NativeArray<float3> positions;

    public void Execute(int index, TransformAccess transform)
    {
        transform.position = positions[index];
    }
}
