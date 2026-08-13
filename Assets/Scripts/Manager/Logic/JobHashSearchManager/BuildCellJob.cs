using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct BuildCellJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<UnitData> units;

    public NativeParallelMultiHashMap<int, int>.ParallelWriter writer;

    public float invCellSize;

    public float minX;
    public float minZ;

    public int cellCountX;
    public int cellCountZ;

    public int campCount;


    public void Execute(int index)
    {
        UnitData unit =
            units[index];

        if (unit.DeadState == 1)
            return;

        int cellX =
            (int)math.floor(
                (unit.Position.x - minX) *
                invCellSize);


        int cellZ =
            (int)math.floor(
                (unit.Position.z - minZ) *
                invCellSize);


        // 超出地图
        if (cellX < 0 ||
            cellX >= cellCountX ||
            cellZ < 0 ||
            cellZ >= cellCountZ)
        {
            return;
        }


        int cellId =
            cellZ * cellCountX +
            cellX;


        // Cell + Camp
        int key =
            cellId * campCount +
            unit.CampType;


        writer.Add(
            key,
            index);
    }
}