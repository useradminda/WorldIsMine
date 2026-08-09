

using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using ZTools;
public class MapCellManager : Singleton<MapCellManager>, IManager
{
    NativeArray<UnitData> units;
    // key:
    // cell编号
    //
    // value:
    // unit id
    NativeParallelMultiHashMap<int, int> cellMap;
    public float cellSize = 10f;
    public float invCellSize;
    public int mapWidth = 200;

    int curRequestCount;
    NativeArray<SearchRequest> requests;
    // 搜索结果
    NativeArray<int> resultIndex;
    NativeArray<int> resultCount;
    const int MaxSearchRequest = 8192;
    const int MaxResult = 128;

    private bool initState = false;
    public void Init(int count)
    {

        invCellSize = 1f / cellSize;


        units =
        new NativeArray<UnitData>(
            count,
            Allocator.Persistent);


        cellMap =
        new NativeParallelMultiHashMap<int,int>(
            count * 4,
            Allocator.Persistent);


        // 每个单位保存找到多少个目标
        resultCount = new NativeArray<int>(MaxSearchRequest, Allocator.Persistent, NativeArrayOptions.ClearMemory);


        // 每个单位最多 MaxResult 个目标
        resultIndex = new NativeArray<int>(MaxSearchRequest * MaxResult, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        requests = new NativeArray<SearchRequest>(MaxSearchRequest, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        initState = true;
    }

    public int RequestSearch(int unitIndex, float radius)
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
        };

        curRequestCount++;

        return reqId;
    }

    // 执行搜索
    public void ExecuteSearch()
    {

        if (initState == false)
        {
            return;
        }
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

                maxResult = MaxResult,

                invCellSize = invCellSize,

                mapWidth = mapWidth
            }
            .Schedule(requsetCount, 64);

        handle.Complete();

        curRequestCount = 0;
    }

    public void GetResult(int requestId, List<int> list)
    {
        int count = resultCount[requestId];

        int offset = requestId * MaxResult;

        for (int i = 0; i < count; i++)
        {
            list.Add(resultIndex[offset + i]);
        }
    }



    public void ManagerInit()
    {
      
    }

    public void ManagerUpdate(float dt)
    {
        updateJob();
    }

    public void ManagerLateUpdate(float dt)
    {
      
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
        {
            return;
        }
        cellMap.Clear();

        for (int i = 0; i < UnitManager.Instance.UnitList.Count; i++)
        {
            UnitData unitData = units[i];
            unitData.Position = UnitManager.Instance.UnitList[i].CurPos;
            unitData.UnitIndex = i;
            units[i] = unitData;
        }

        JobHandle buildHandle =
         new BuildCellJob
         {
             units = units,
             writer = cellMap.AsParallelWriter(),
             invCellSize = invCellSize,
             mapWidth = mapWidth
         }
         .Schedule(units.Length, 64);

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

    public int CellId;
}

public struct SearchRequest
{
    // 谁搜索
    public int UnitIndex;

    // 搜索半径
    public float Radius;
}
