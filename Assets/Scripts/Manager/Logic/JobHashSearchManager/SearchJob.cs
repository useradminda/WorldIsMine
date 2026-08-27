using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

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

    [WriteOnly]
    [NativeDisableParallelForRestriction]
    public NativeArray<int> randomResultIndex;


    [ReadOnly]
    public NativeArray<SearchRequest> requests;


    public int maxResult;

    public float invCellSize;

    public float minX;
    public float minZ;

    public int cellCountX;
    public int cellCountZ;

    public float cellSize;

    public int campCount;


    public void Execute(int index)
    {
        SearchRequest req =
            requests[index];

        float3 searchPos = req.SearchPos;
        //UnitData me =
        //    units[req.UnitIndex];


        int offset =
            index * maxResult;


        int count = 0;


        float radiusSq =
            req.Radius * req.Radius;


        // -------------------------
        // 自己所在 Cell
        // -------------------------

        int cx =
            (int)math.floor(
                (searchPos.x - minX) *
                invCellSize);


        int cz =
            (int)math.floor(
                (searchPos.z - minZ) *
                invCellSize);


        int range =
            (int)math.ceil(
                req.Radius *
                invCellSize);


        float minDistSq = float.MaxValue;


        nearResultIndex[index] = -1;

        randomResultIndex[index] = -1;


        // -------------------------
        // 遍历 Cell
        // -------------------------

        for (int z = -range;
             z <= range;
             z++)
        {
            int cellZ =
                cz + z;


            if (cellZ < 0 ||
                cellZ >= cellCountZ)
            {
                continue;
            }


            for (int x = -range;
                 x <= range;
                 x++)
            {
                int cellX =
                    cx + x;


                if (cellX < 0 ||
                    cellX >= cellCountX)
                {
                    continue;
                }


                int cellId =
                    cellZ * cellCountX +
                    cellX;


                // -------------------------
                // Cell + Camp
                // -------------------------

                int key =
                    cellId *
                    campCount +
                    req.SearchCamp;


                NativeParallelMultiHashMapIterator<int>
                    iterator;


                int other;


                if (!cellMap.TryGetFirstValue(
                        key,
                        out other,
                        out iterator))
                {
                    continue;
                }


                // -------------------------
                // 遍历这个 Cell 中
                // 指定阵营的单位
                // -------------------------

                do
                {
                    //if (other ==
                    //    req.UnitIndex)
                    //{
                    //    continue;
                    //}


                    float distSq =
                        math.lengthsq(
                            units[other].Position -
                            searchPos);


                    if (distSq >
                        radiusSq)
                    {
                        continue;
                    }


                    // -------------------------
                    // 最近单位
                    // -------------------------

                    if (distSq <
                        minDistSq)
                    {
                        minDistSq =
                            distSq;


                        nearResultIndex[index] =
                            other;
                    }


                    // -------------------------
                    // 普通结果
                    // -------------------------

                    if (count <
                        maxResult)
                    {
                        resultIndex[
                            offset + count] =
                            other;


                        count++;
                    }

                }
                while (
                    cellMap.TryGetNextValue(
                        out other,
                        ref iterator));
            }
        }


        resultCount[index] =
            count;

        // -------------------------
        // 随机选择一个目标
        // -------------------------

        if (count > 0)
        {
            uint seed =
                req.RandomSeed;

            // Random 的 Seed 不能为 0
            if (seed == 0)
                seed = 1;

            Random random =
                new Random(seed);

            int randomIndex =
                random.NextInt(count);

            randomResultIndex[index] =
                resultIndex[
                    offset + randomIndex];
        }
    }
}