using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using ZTools;

public class MapCellManager : Singleton<MapCellManager>, IManager
{
    NativeArray<UnitData> units;

    // Cell + Camp -> UnitIndex
    NativeParallelMultiHashMap<int, int> cellMap;

    public float cellSize = 20f;
    public float invCellSize;

    // 地图范围
    public float minX = -300f;
    public float maxX = 300f;

    public float minZ = -300f;
    public float maxZ = 300f;

    public int cellCountX;
    public int cellCountZ;

    // 阵营数量
    public const int CampCount = 2;

    int curRequestCount;

    NativeArray<SearchRequest> requests;

    NativeArray<int> resultIndex;
    NativeArray<int> resultCount;
    NativeArray<int> nearResultIndex;

    const int MaxSearchRequest = 10000;
    const int MaxResult = 32;

    private int unitCount = 0;

    private bool initState = false;


    public void ManagerInit()
    {
        invCellSize = 1f / cellSize;

        cellCountX =
            (int)math.ceil(
                (maxX - minX) * invCellSize);

        cellCountZ =
            (int)math.ceil(
                (maxZ - minZ) * invCellSize);


        units =
            new NativeArray<UnitData>(
                MaxSearchRequest,
                Allocator.Persistent,
                NativeArrayOptions.ClearMemory);


        cellMap =
            new NativeParallelMultiHashMap<int, int>(
                MaxSearchRequest * 2,
                Allocator.Persistent);


        resultCount =
            new NativeArray<int>(
                MaxSearchRequest,
                Allocator.Persistent,
                NativeArrayOptions.ClearMemory);


        nearResultIndex =
            new NativeArray<int>(
                MaxSearchRequest,
                Allocator.Persistent,
                NativeArrayOptions.ClearMemory);


        resultIndex =
            new NativeArray<int>(
                MaxSearchRequest * MaxResult,
                Allocator.Persistent,
                NativeArrayOptions.ClearMemory);


        requests =
            new NativeArray<SearchRequest>(
                MaxSearchRequest,
                Allocator.Persistent,
                NativeArrayOptions.ClearMemory);


        initState = true;
    }


    public int AddUnit(
        float3 position,
        int campType)
    {
        if (unitCount >= units.Length)
        {
            UnityEngine.Debug.LogError(
                "units数量不够，需要扩容了");

            return -1;
        }


        int index = unitCount;


        units[index] =
            new UnitData
            {
                UnitIndex = index,
                Position = position,
                CampType = campType,
                DeadState = 0,
            };


        unitCount++;

        return index;
    }


    public int RequestSearch(
        int unitIndex,
        float radius,
        int searchCampType)
    {
        if (!initState)
            return -1;


        if (curRequestCount >= MaxSearchRequest)
            return -1;


        if (unitIndex < 0 ||
            unitIndex >= unitCount)
            return -1;


        int reqId =
            curRequestCount;


        requests[reqId] =
            new SearchRequest
            {
                UnitIndex = unitIndex,
                Radius = radius,
                SearchCamp = searchCampType
            };


        curRequestCount++;


        return reqId;
    }


    public void ExecuteSearch()
    {
        if (!initState)
            return;


        if (curRequestCount <= 0)
            return;


        int requestCount =
            curRequestCount;

       // UnityEngine.Debug.Log(requestCount);

        JobHandle handle =
            new SearchJob
            {
                requests = requests,

                units = units,

                cellMap = cellMap,

                resultIndex = resultIndex,

                resultCount = resultCount,

                nearResultIndex = nearResultIndex,

                maxResult = MaxResult,

                invCellSize = invCellSize,

                minX = minX,

                minZ = minZ,

                cellCountX = cellCountX,

                cellCountZ = cellCountZ,

                cellSize = cellSize,

                campCount = CampCount

            }
            .Schedule(
                requestCount,
                64);


        handle.Complete();


        curRequestCount = 0;
    }


    public void GetResult(
        int requestId,
        List<int> list,
        ref int nearestIndex)
    {
        int count =
            resultCount[requestId];


        int offset =
            requestId * MaxResult;


        for (int i = 0; i < count; i++)
        {
            list.Add(
                resultIndex[offset + i]);
        }


        nearestIndex =
            nearResultIndex[requestId];
    }


    public void ManagerUpdate(float dt)
    {
        UpdateJob();
        
    }


    public void ManagerLateUpdate(float dt)
    {
        ExecuteSearch();
    }


    public void ManagerRefuse()
    {
    }


    public void ManagerDestroy()
    {
        Dispose();
    }


    private void UpdateJob()
    {
        if (!initState)
            return;


        cellMap.Clear();


        if (unitCount <= 0)
            return;


        for (int i = 0; i < unitCount; i++)
        {
            UnitData unitData =
                units[i];


            unitData.Position =
                UnitManager.Instance
                    .UnitList[i]
                    .CurPos;


            unitData.UnitIndex = i;


            unitData.CampType =
                UnitManager.Instance
                    .UnitList[i]
                    .CampTypeInt;

            unitData.DeadState = UnitManager.Instance
                    .UnitList[i].IsDead == true ? 1 : 0;


            units[i] =
                unitData;
        }


        JobHandle buildHandle =
            new BuildCellJob
            {
                units = units,

                writer =
                    cellMap.AsParallelWriter(),

                invCellSize =
                    invCellSize,

                minX =
                    minX,

                minZ =
                    minZ,

                cellCountX =
                    cellCountX,

                cellCountZ =
                    cellCountZ,

                campCount =
                    CampCount

            }
            .Schedule(
                unitCount,
                64);


        buildHandle.Complete();
    }


    private void Dispose()
    {
        if (!initState)
            return;


        units.Dispose();
        cellMap.Dispose();
        requests.Dispose();
        resultIndex.Dispose();
        resultCount.Dispose();
        nearResultIndex.Dispose();


        initState = false;
    }
}


public struct UnitData
{
    public int UnitIndex;

    public float3 Position;

    public float3 CatchPosition;

    // 阵营
    public int CampType;

    // 士兵类型
    public int UnitType;

    public int DeadState;
}


public struct SearchRequest
{
    // 谁搜索
    public int UnitIndex;

    // 搜索半径
    public float Radius;

    // 搜索哪个阵营
    public int SearchCamp;
}