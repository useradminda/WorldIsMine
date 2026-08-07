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
    public NativeArray<int> resultIndex;

    [WriteOnly]
    public NativeArray<int> resultCount;

    [WriteOnly]
    public NativeArray<SearchRequest> requests;

    //public int curRequestCount;

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


        int cx = (int)math.floor(
            me.Position.x * invCellSize);


        int cz = (int)math.floor(
            me.Position.z * invCellSize);



        int range = (int)math.ceil(
             req.Radius * invCellSize);


        for (int z = -range; z <= range; z++)
        {
            for (int x = -range; x <= range; x++)
            {

                int cellX = cx + x;
                int cellZ = cz + z;


                if (cellX < 0 ||
                   cellX >= mapWidth)
                    continue;


                int cell =
                    cellZ * mapWidth + cellX;

                NativeParallelMultiHashMapIterator<int> it;

                int other;

                if (cellMap.TryGetFirstValue(
                    cell,
                    out other,
                    out it))
                {
                    do
                    {

                        if (other == index)
                            continue;


                        float dist =
                            math.lengthsq(
                                units[other].Position -
                                me.Position);



                        if (dist <= radiusSq)
                        {

                            if (count < maxResult)
                            {
                                resultIndex[
                                    offset + count
                                ] = other;


                                count++;
                            }
                        }


                    }
                    while (
                    cellMap.TryGetNextValue(
                        out other,
                        ref it));
                }
            }
        }


        resultCount[index] = count;
    }
}
