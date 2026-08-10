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
    public NativeParallelMultiHashMap<int, int> cellMap;

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

    public int mapWidth;

    public void Execute(int index)
    {
        SearchRequest req = requests[index];

        UnitData me = units[req.UnitIndex];

        int offset = index * maxResult;

        int count = 0;

        float radiusSq = req.Radius * req.Radius;

        // 当前单位所在 Cell
        int cx = (int)math.floor(
            me.Position.x * invCellSize);

        int cz = (int)math.floor(
            me.Position.z * invCellSize);

        // 搜索多少圈 Cell
        int range = (int)math.ceil(
            req.Radius * invCellSize);

        // 最近单位
        float minDistSq = float.MaxValue;

        nearResultIndex[index] = -1;

        for (int z = -range; z <= range; z++)
        {
            int cellZ = cz + z;

            if (cellZ < 0)
                continue;

            if (cellZ >= mapWidth)
                continue;

            for (int x = -range; x <= range; x++)
            {
                int cellX = cx + x;

                if (cellX < 0)
                    continue;

                if (cellX >= mapWidth)
                    continue;

                int cellId =
                    cellZ * mapWidth + cellX;

                NativeParallelMultiHashMapIterator<int> iterator;

                int other;

                if (!cellMap.TryGetFirstValue(
                    cellId,
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
