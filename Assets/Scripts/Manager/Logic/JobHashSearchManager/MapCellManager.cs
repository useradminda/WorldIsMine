

using System.Collections.Generic;

using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using ZTools;
public class MapCellManager : Singleton<MapCellManager>, IManager
{
    NativeArray<UnitData> units;
    NativeParallelMultiHashMap<long, int> cellMap;
    public float cellSize = 20f;
    public float invCellSize;

    int curRequestCount;
    NativeArray<SearchRequest> requests;
    // 搜索结果
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

        units =  new NativeArray<UnitData>(MaxSearchRequest, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        cellMap = new NativeParallelMultiHashMap<long,int>(MaxSearchRequest * 2, Allocator.Persistent);
        // 每个单位保存找到多少个目标
        resultCount = new NativeArray<int>(MaxSearchRequest, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        nearResultIndex = new NativeArray<int>(MaxSearchRequest, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        // 每个单位最多 MaxResult 个目标
        resultIndex = new NativeArray<int>(MaxSearchRequest * MaxResult, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        requests = new NativeArray<SearchRequest>(MaxSearchRequest, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        initState = true;
    }

    public int AddUnit(float3 position, int campType)
    {
        if (unitCount >= units.Length)
        {
            UnityEngine.Debug.LogError("units数量不够，需要扩容了");
            return -1;
        }
        int index = unitCount;

        units[index] = new UnitData
        {
            UnitIndex = index,
            Position = position,
            CampType = campType,
        };

        unitCount++;

        return index;
    }

    public int RequestSearch(int unitIndex, float radius, int searchCampType)
    {
        if (initState == false)
        {
            return -1;
        }
        if (curRequestCount >= MaxSearchRequest)
            return -1;

        if (unitIndex < 0 || unitIndex >= units.Length)
            return -1;

        int reqId = curRequestCount;
        requests[reqId] = new SearchRequest
        {
            UnitIndex = unitIndex,
            Radius = radius,
            SearchCamp = searchCampType,
        };

        curRequestCount++;

        return reqId;
    }

    // 执行搜索
    public void ExecuteSearch()
    {
        if (initState == false)
            return;
        if (curRequestCount <= 0)
            return;

        int requsetCount = curRequestCount;
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

            }
            .Schedule(requsetCount, 64);

        handle.Complete();

        curRequestCount = 0;
    }

    public void GetResult(int requestId, List<int> list,ref int neastIndex)
    {
        int count = resultCount[requestId];

        int offset = requestId * MaxResult;

        for (int i = 0; i < count; i++)
        {
            list.Add(resultIndex[offset + i]);
        }
        neastIndex = nearResultIndex[requestId];
    }

    public void ManagerUpdate(float dt)
    {
        updateJob();
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
        dispose();
    }

    private void updateJob()
    {
        if (initState == false)
            return;
        
        cellMap.Clear();

        if (unitCount <= 0)
        {
            return;
        }

        for (int i = 0; i < unitCount; i++)
        {
            UnitData unitData = units[i];
            unitData.Position = UnitManager.Instance.UnitList[i].CurPos;
            unitData.UnitIndex = i;
            unitData.CampType = UnitManager.Instance.UnitList[i].CampTypeInt;
            units[i] = unitData;
        }

        JobHandle buildHandle =
         new BuildCellJob
         {
             units = units,
             writer = cellMap.AsParallelWriter(),
             invCellSize = invCellSize,
         }
         .Schedule(unitCount, 64);

        buildHandle.Complete();
    }


    private void dispose()
    {
        units.Dispose();

        cellMap.Dispose();
        requests.Dispose();
        resultIndex.Dispose();
        resultCount.Dispose();
    }
}

public struct UnitData
{
    public int UnitIndex;

    public float3 Position;

    public float3 CatchPosition;

    // Camp
    public int CampType;
     
    // solider type
    public int UnitType;
}

public struct SearchRequest
{
    // 
    public int UnitIndex;

    //
    public float Radius;

    // 
    public int SearchCamp;
}
