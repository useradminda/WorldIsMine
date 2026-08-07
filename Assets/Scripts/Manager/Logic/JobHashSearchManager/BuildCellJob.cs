
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

    public int mapWidth;

    public int ExecuteCellID(float3 pos)
    {
        int x = (int)math.floor(pos.x * invCellSize);
        int z = (int)math.floor(pos.z * invCellSize);

        return z * mapWidth + x;
    }

    public void Execute(int index)
    {
        UnitData unit = units[index];
        int cellId = ExecuteCellID(index);
        unit.CellId = cellId;
        writer.Add(cellId, index);
    }
}
