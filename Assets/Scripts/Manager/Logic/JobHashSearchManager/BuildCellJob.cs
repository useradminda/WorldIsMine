
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;


[BurstCompile]
public struct BuildCellJob : IJobParallelFor
{

    [ReadOnly]
    public NativeArray<UnitData> units;

    public NativeParallelMultiHashMap<long, int>.ParallelWriter writer;

    public float invCellSize;

    public static long GetCellKey(int x, int z)
    {
        return ((long)x << 32) | (uint)z;
    }

    public void Execute(int index)
    {
        UnitData unit = units[index];

        int x = (int)math.floor(
            unit.Position.x * invCellSize);

        int z = (int)math.floor(
            unit.Position.z * invCellSize);

        long cellKey = GetCellKey(x, z);

        writer.Add(cellKey, index);
    }
}
