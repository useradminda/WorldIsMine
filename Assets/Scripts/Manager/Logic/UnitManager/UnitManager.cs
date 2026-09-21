
using ZTools;
using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitManager : Singleton<UnitManager>, IManager
{
    public WaitListTemplate<UnitLogicBase> UnitList = new WaitListTemplate<UnitLogicBase>(null);
    private readonly Queue<UnitSpawnRequest> redWaitingRequests = new Queue<UnitSpawnRequest>();
    private readonly Queue<UnitSpawnRequest> blueWaitingRequests = new Queue<UnitSpawnRequest>();
    private Func<int, ECampType, int, int> spawnHandler;

    private UnitLogicBase redWallLogic;
    private UnitLogicBase blueWallLogic;
    public event Action<ECampType> WallDestroyed;

    private class UnitSpawnRequest
    {
        public int CfgId;
        public int Count;
    }
  
    public void ManagerInit()
    {
        redWallLogic = UnitFactory.CreateWall(ECampType.Red);
        blueWallLogic = UnitFactory.CreateWall(ECampType.Blue);
    }

    public void ManagerUpdate(float dt)
    {
        for (int i = 0; i < UnitList.Count; i++)
        {
            UnitList[i].UnitUpdate(dt);
        }

        ProcessWaitingRequests(ECampType.Red);
        ProcessWaitingRequests(ECampType.Blue);
    }

    public void SetSpawnHandler(Func<int, ECampType, int, int> handler)
    {
        spawnHandler = handler;
    }

    public bool HasWaitingRequest(ECampType campType)
    {
        return GetWaitingQueue(campType).Count > 0;
    }

    public int GetWaitingUnitCount(ECampType campType)
    {
        int count = 0;
        Queue<UnitSpawnRequest> queue = GetWaitingQueue(campType);
        foreach (UnitSpawnRequest request in queue)
        {
            count += request.Count;
        }

        return count;
    }

    public void EnqueueSpawn(int cfgId, ECampType campType, int count)
    {
        if (count <= 0)
        {
            return;
        }

        GetWaitingQueue(campType).Enqueue(new UnitSpawnRequest
        {
            CfgId = cfgId,
            Count = count
        });
    }

    public UnitLogicBase GetOtherWallLogic(ECampType campType)
    {
        if (campType == ECampType.Red)
            return blueWallLogic;
        return redWallLogic;
    }

    public Vector3 GetWallCenter(ECampType campType)
    {
        return Vector3.zero;
    }

    public Vector3 GetWallSize()
    {
        return Vector3.zero;
    }

    public void NotifyWallDestroyed(ECampType campType)
    {
        WallDestroyed?.Invoke(campType);
    }

    private void ProcessWaitingRequests(ECampType campType)
    {
        if (spawnHandler == null)
        {
            return;
        }

        Queue<UnitSpawnRequest> queue = GetWaitingQueue(campType);
        if (queue.Count == 0)
        {
            return;
        }

        UnitSpawnRequest request = queue.Peek();
        if (!UnitFactory.CanCreateUnitGroup(campType, request.Count))
        {
            return;
        }

        spawnHandler(request.CfgId, campType, request.Count);
        queue.Dequeue();
    }

    private Queue<UnitSpawnRequest> GetWaitingQueue(ECampType campType)
    {
        return campType == ECampType.Red ? redWaitingRequests : blueWaitingRequests;
    }

    public void ManagerLateUpdate(float dt)
    {
        //UnitList.AddWaitingList();
    }

    public void ManagerRefuse()
    {

    }
    public void ManagerDestroy()
    {
        ClearWaiting();
    }

    //// 增加一个Unit
    //public UnitLogicBase AddUnit(UnitLogicBase unit)
    //{
    //    UnitList.Add(unit);
    //    return unit;
    //}

    public UnitLogicBase AddUnitImmediately(UnitLogicBase unit)
    {
        UnitList.AddImmediately(unit);
        return unit;
    }

    public void ClearWaiting()
    {
        redWaitingRequests.Clear();
        blueWaitingRequests.Clear();
    }
}
