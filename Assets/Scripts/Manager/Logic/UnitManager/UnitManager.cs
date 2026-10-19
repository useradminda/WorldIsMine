
using ZTools;
using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitManager : Singleton<UnitManager>, IManager
{
    public WaitListTemplate<UnitLogicBase> UnitList = new WaitListTemplate<UnitLogicBase>(null);
    private readonly List<UnitLogicBase> redUnitList = new List<UnitLogicBase>();
    private readonly List<UnitLogicBase> blueUnitList = new List<UnitLogicBase>();
    public List<UnitLogicBase> RedUnitList => redUnitList;
    public List<UnitLogicBase> BlueUnitList => blueUnitList;
    private readonly Queue<UnitSpawnRequest> redWaitingRequests = new Queue<UnitSpawnRequest>();
    private readonly Queue<UnitSpawnRequest> blueWaitingRequests = new Queue<UnitSpawnRequest>();
    private Func<int, ECampType, int, int> spawnHandler;

    private UnitLogicBase redWallLogic;
    private UnitLogicBase blueWallLogic;
    private UnitLogicBase redPlayerLogic;
    private UnitLogicBase bluePlayerLogic;
    private BornConfig bornConfig;
    private Vector3 redWallCenter;
    private Vector3 blueWallCenter;
    public event Action<ECampType> WallDestroyed;

    private static readonly Vector3 WallSize = new Vector3(BattleDefine.AreaTotalWith * 2, 1f, 1f);

    private class UnitSpawnRequest
    {
        public int CfgId;
        public int Count;
    }
  
    public void ManagerInit()
    {
        redWallLogic = UnitFactory.CreateWall(ECampType.Red);
        blueWallLogic = UnitFactory.CreateWall(ECampType.Blue);
        redPlayerLogic = UnitFactory.CreatePlayerLogic(ECampType.Red);
        bluePlayerLogic = UnitFactory.CreatePlayerLogic(ECampType.Blue);
    }

    /// <summary>
    /// 设置出生点配置，用于计算红蓝双方城墙位置。
    /// </summary>
    public void SetBornConfig(BornConfig config)
    {
        bornConfig = config;
        if (bornConfig == null)
        {
            redWallCenter = Vector3.zero;
            blueWallCenter = Vector3.zero;
            return;
        }

        redWallCenter = bornConfig.RedCityLogicPoint.transform.position;
        blueWallCenter = bornConfig.BlueCityLogicPoint.transform.position;

        // 城门模型位置和城门逻辑位置分离：
        // CityDoorPoint 只负责模型显示，攻击搜索/命中仍使用原来的出生点逻辑位置。
        //redWallCenter = bornConfig.GetBornPoint(ECampType.Red)
        //    + bornConfig.GetForward(ECampType.Red) * 2f;
        //blueWallCenter = bornConfig.GetBornPoint(ECampType.Blue)
        //    + bornConfig.GetForward(ECampType.Blue) * 2f;
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

    public UnitLogicBase GetWallLogic(ECampType campType)
    {
        return campType == ECampType.Red ? redWallLogic : blueWallLogic;
    }

    /// <summary>
    /// 获取指定阵营的玩家逻辑对象，用作全局技能的攻击来源。
    /// </summary>
    public UnitLogicBase GetPlayerLogic(ECampType campType)
    {
        if (campType == ECampType.Red)
        {
            return redPlayerLogic;
        }

        return bluePlayerLogic;
    }

    public Vector3 GetWallCenter(ECampType campType)
    {
        if (campType == ECampType.Red)
        {
            return redWallCenter;
        }

        return blueWallCenter;
    }

    public Vector3 GetWallSize()
    {
        return WallSize;
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
        if (!UnitFactory.CanCreateUnitGroup(request.CfgId, campType, request.Count))
        {
            UnitSpawnRequest heroRequest = TakeFirstCreatableHeroRequest(queue, campType);
            if (heroRequest != null)
            {
                spawnHandler(heroRequest.CfgId, campType, heroRequest.Count);
            }
            return;
        }

        spawnHandler(request.CfgId, campType, request.Count);
        queue.Dequeue();
    }

    private UnitSpawnRequest TakeFirstCreatableHeroRequest(Queue<UnitSpawnRequest> queue, ECampType campType)
    {
        int requestCount = queue.Count;
        UnitSpawnRequest heroRequest = null;
        for (int i = 0; i < requestCount; i++)
        {
            UnitSpawnRequest request = queue.Dequeue();
            if (heroRequest == null && UnitFactory.IsHero(request.CfgId) && UnitFactory.CanCreateUnitGroup(request.CfgId, campType, request.Count))
            {
                heroRequest = request;
                continue;
            }
            queue.Enqueue(request);
        }
        return heroRequest;
    }

    private Queue<UnitSpawnRequest> GetWaitingQueue(ECampType campType)
    {
        return campType == ECampType.Red ? redWaitingRequests : blueWaitingRequests;
    }

    public void ManagerLateUpdate(float dt)
    {
        for (int i = 0; i < UnitList.Count; i++)
        {
            UnitList[i].UnitLateUpdate(dt);
        }
    }

    public void ManagerRefuse()
    {

    }
    public void ManagerDestroy()
    {
        ClearWaiting();
        redPlayerLogic = null;
        bluePlayerLogic = null;
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
        GetCampUnitList(unit.CampType).Add(unit);
        return unit;
    }

    /// <summary>获取指定阵营创建过的全部士兵逻辑对象，包含已经死亡并等待复用的对象。</summary>
    public List<UnitLogicBase> GetCampUnitList(ECampType campType)
    {
        return campType == ECampType.Red ? redUnitList : blueUnitList;
    }

    public void ClearWaiting()
    {
        redWaitingRequests.Clear();
        blueWaitingRequests.Clear();
    }
}
