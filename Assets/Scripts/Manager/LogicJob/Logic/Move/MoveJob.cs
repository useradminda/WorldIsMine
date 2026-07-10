using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct MoveJob : IJobParallelFor
{
    public NativeArray<float3> positions;

    [ReadOnly]
    public NativeArray<float3> velocities;

    public float deltaTime;

    public void Execute(int index)
    {
        float3 pos =
            positions[index];

        pos +=
            velocities[index]
            * deltaTime;

        // 边界反弹
        if (pos.x > 100 || pos.x < -100)
        {
            var v = velocities[index];
            v.x *= -1;
            velocities[index] = v;
        }

        if (pos.z > 100 || pos.z < -100)
        {
            var v = velocities[index];
            v.z *= -1;
            velocities[index] = v;
        }

        positions[index] = pos;
    }
}