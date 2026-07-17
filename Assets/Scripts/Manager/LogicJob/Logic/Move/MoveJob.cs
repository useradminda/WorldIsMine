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

    public float deltaTime;

    public void Execute(int index)
    {
        //float3 pos = RvoManager.Instance.Agents[index].pos;// positions[index];
       // positions[index] = pos;
    }
}