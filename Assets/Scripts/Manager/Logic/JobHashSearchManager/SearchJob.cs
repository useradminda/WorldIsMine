using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
[BurstCompile]
public struct SearchJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<UnitData> units;

    [ReadOnly]
    public NativeParallelMultiHashMap<long, int> cellMap;

    [WriteOnly]
    [NativeDisableParallelForRestriction]
    public NativeArray<int> resultIndex;

    [WriteOnly]
    [NativeDisableParallelForRestriction]
    public NativeArray<int> resultCount;

    [WriteOnly]
    [NativeDisableParallelForRestriction]
    public NativeArray<int> nearResultIndex;

    [ReadOnly]
    public NativeArray<SearchRequest> requests;

    public int maxResult;

    public float invCellSize;


    public void Execute(int index)
    {
        SearchRequest req = requests[index];

        UnitData me = units[req.UnitIndex];

        int offset = index * maxResult;

        int count = 0;

        float radiusSq = req.Radius * req.Radius;

        int cx = (int)math.floor(
            me.Position.x * invCellSize);

        int cz = (int)math.floor(
            me.Position.z * invCellSize);

        int range = (int)math.ceil(
            req.Radius * invCellSize);

        // 最近单位
        float minDistSq = float.MaxValue;

        nearResultIndex[index] = -1;

        for (int z = -range; z <= range; z++)
        {
            for (int x = -range; x <= range; x++)
            {
                int cellX = cx + x;
                int cellZ = cz + z;

                long cellKey =
                    ((long)cellX << 32) |
                    (uint)cellZ;

                NativeParallelMultiHashMapIterator<long> iterator;

                int other;

                if (!cellMap.TryGetFirstValue(
                    cellKey,
                    out other,
                    out iterator))
                {
                    continue;
                }

                do
                {
                    // 自己排除
                    if (other == req.UnitIndex)
                        continue;

                    // 阵营不符合
                    if (units[other].CampType != req.SearchCamp)
                        continue;

                    float distSq =
                        math.lengthsq(
                            units[other].Position -
                            me.Position);

                    if (distSq <= radiusSq)
                    {
                        // 最近单位
                        if (distSq < minDistSq)
                        {
                            minDistSq = distSq;

                            nearResultIndex[index] = other;
                        }

                        // 普通搜索结果
                        if (count < maxResult)
                        {
                            resultIndex[
                                offset + count
                            ] = other;

                            count++;
                        }
                    }

                } while (
                    cellMap.TryGetNextValue(
                        out other,
                        ref iterator));
            }
        }

        resultCount[index] = count;
    }
}
