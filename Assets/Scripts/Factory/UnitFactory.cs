
using Nebukam;
using Nebukam.ORCA;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public static class UnitFactory
{
    public const int MaxActiveUnitCountPerCamp = 3000;
    public const int MaxActiveReinforcementCountPerCamp = 1000;
    private static int blueUId = 0;
    private static int redUId = 0;

    private static int redFreeCount = 0;
    private static List<int> redFreeIndexList = new List<int>();

    private static int blueFreeCount = 0;
    private static List<int> blueFreeIndexList = new List<int>();

    private static int redActiveCount = 0;
    private static int blueActiveCount = 0;
    private static int redReinforcementCount = 0;
    private static int blueReinforcementCount = 0;

    public static int GetActiveCount(ECampType campType)
    {
        return campType == ECampType.Red ? redActiveCount : blueActiveCount;
    }

    public static int GetReinforcementCount(ECampType campType)
    {
        return campType == ECampType.Red ? redReinforcementCount : blueReinforcementCount;
    }

    public static bool IsHero(int cfgId)
    {
        SoliderCfg soliderCfg = SoliderCfgConfig.Ins.SearchById(cfgId);
        return soliderCfg != null && soliderCfg.unitType == 101;
    }

    public static bool CanCreateUnit(int cfgId, ECampType campType, bool isReinforcement = false)
    {
        if (IsHero(cfgId))
        {
            return true;
        }

        return GetActiveCount(campType) < MaxActiveUnitCountPerCamp
            && (!isReinforcement || GetReinforcementCount(campType) < MaxActiveReinforcementCountPerCamp);
    }

    public static bool CanCreateUnitGroup(int cfgId, ECampType campType, int count, bool isReinforcement = false)
    {
        if (count <= 0 || IsHero(cfgId))
        {
            return count > 0;
        }

        return GetCreatableUnitCount(cfgId, campType, count, isReinforcement) >= count;
    }

    public static int GetCreatableUnitCount(int cfgId, ECampType campType, int requestedCount, bool isReinforcement = false)
    {
        if (requestedCount <= 0)
        {
            return 0;
        }

        if (IsHero(cfgId))
        {
            return requestedCount;
        }

        int totalRemaining = Mathf.Max(0, MaxActiveUnitCountPerCamp - GetActiveCount(campType));
        if (!isReinforcement)
        {
            return Mathf.Min(requestedCount, totalRemaining);
        }

        int reinforcementRemaining = Mathf.Max(0, MaxActiveReinforcementCountPerCamp - GetReinforcementCount(campType));
        return Mathf.Min(requestedCount, Mathf.Min(totalRemaining, reinforcementRemaining));
    }
    
    // 创建一个单位
    public static UnitLogicBase CreateUnit(
        int cfgId,
        Vector3 moveForward,
        ECampType campType, int index,
        bool isReinforcement = false)
    {
        if (!CanCreateUnit(cfgId, campType, isReinforcement))
        {
            return null;
        }

        if (!IsHero(cfgId))
        {
            AddActiveCount(campType);
            if (isReinforcement)
            {
                AddReinforcementCount(campType);
            }
        }
        UnitLogicBase unit = new UnitLogicBase(cfgId, getUId(campType), campType, moveForward, index, isReinforcement);
        return unit;
    }

    // 获取UnitCatch
    public static UnitLogicBase GetUnitCatch(int cfgId,
        Vector3 bornPoint,
        Vector3 moveForward,
        ECampType campType,
        bool isReinforcement = false)
    {
        if (!CanCreateUnit(cfgId, campType, isReinforcement))
        {
            return null;
        }

        int unitIndex = GetRecycleId(campType);
        if (unitIndex == -1)
        {
            return null;
            
        }
        UnitLogicBase unit = UnitManager.Instance.UnitList[unitIndex];
        if (!IsHero(cfgId))
        {
            AddActiveCount(campType);
            if (isReinforcement)
            {
                AddReinforcementCount(campType);
            }
        }
        unit.CycleUse(cfgId, getUId(campType), moveForward, isReinforcement);
        unit.Agenter.pos = bornPoint;
        unit.Agenter.prefVelocity = moveForward;
        unit.Agenter.velocity = moveForward;
        unit.Agenter.radius = unit.Prop.Radius;
        unit.Agenter.radiusObst = unit.Prop.Radius;
        unit.Agenter.maxNeighbors = 20;
        unit.Agenter.timeHorizon = 0.3f; /// 0.1f;   // 距离其他代理检查
        unit.Agenter.timeHorizonObst = 4f; // 速度越小，这个值越大，才不会穿透不可行走区域，距离障碍
        unit.Agenter.saveMaxSpeed = unit.Prop.MaxSpeed;
        unit.Agenter.maxSpeed = unit.Prop.MaxSpeed;
        SetAgentLayers(unit.Agenter, unit.SoliderCfg.myLayer, unit.SoliderCfg.ignoreLayer);
        return unit;
    }

    // 创建一个RVO智能体
    public static Agent  CreateAgent(Vector3 bornPoint, Vector3 forward, float radius, float maxSpeed, int myLayer, int[] ignoreLayers)
    {        
        Agent agent = Pool.Rent<Agent>();   
        agent.pos = bornPoint;
        agent.prefVelocity = forward;
        agent.velocity = forward;
        agent.radius = radius;
        agent.radiusObst = radius;
        agent.maxNeighbors = 20;
        agent.timeHorizon = 0.3f; //0.1f;   // 距离其他代理检查
        agent.timeHorizonObst = 4f; // 速度越小，这个值越大，才不会穿透不可行走区域，距离障碍
        agent.saveMaxSpeed = maxSpeed;
        agent.maxSpeed = maxSpeed;
        SetAgentLayers(agent, myLayer, ignoreLayers);
        return agent;
    }

    // 根据单位配置设置RVO占用层和忽略层，忽略层为0时不忽略任何层。
    private static void SetAgentLayers(Agent agent, int myLayer, int[] ignoreLayers)
    {
        agent.layerOccupation = ConvertConfigLayer(myLayer);
        agent.layerIgnore = ORCALayer.NONE;
        if (ignoreLayers == null)
        {
            return;
        }
        for (int i = 0; i < ignoreLayers.Length; i++)
        {
            agent.layerIgnore |= ConvertConfigLayer(ignoreLayers[i]);
        }
    }

    // 表格使用从1开始的层编号，转换成ORCA需要的位掩码；0表示不占用或不忽略任何层。
    private static ORCALayer ConvertConfigLayer(int configLayer)
    {
        if (configLayer <= 0 || configLayer > 31)
        {
            return ORCALayer.NONE;
        }

        return (ORCALayer)(1 << (configLayer - 1));
    }

    public static UnitLogicBase CreateWall(ECampType campType)
    {
        int wallCfgId = campType == ECampType.Red ? 10001 : 20001;
        int uid = 999999;
        int index = uid;
        if(campType == ECampType.Blue)
        {
            uid -= 1;
            index = uid;
        }
        UnitLogicBase unit = new UnitLogicBase(wallCfgId, uid, campType, Vector3.zero, index);
        unit.SetUnitType(EUnitType.Wall);
        return unit;
    }

    /// <summary>
    /// 创建阵营玩家逻辑对象。玩家逻辑不计入士兵数量，也不加入Agent和地图搜索。
    /// </summary>
    public static UnitLogicBase CreatePlayerLogic(ECampType campType)
    {
        int cfgId = campType == ECampType.Red ? 30001 : 30002;
        int uid = campType == ECampType.Red ? 30001 : 30002;
        int index = campType == ECampType.Red ? -1 : -2;
        return new UnitLogicBase(cfgId, uid, campType, Vector3.zero, index);
    }

    // 移除
    public static void RemoveFlyObjectLogic(FlyObjectLogicBase flyObjectLogic)
    {
        FlyObjectManager.Instance.RemoveFlyUnitImmediately(flyObjectLogic);
    }

    // 回收UnitIndex
    public static void RecycleId(ECampType campType, int recycleUnitIndex, bool isHero, bool isReinforcement = false)
    {
        if (!isHero)
        {
            RemoveActiveCount(campType);
            if (isReinforcement)
            {
                RemoveReinforcementCount(campType);
            }
        }
        if (campType == ECampType.Red)
        {
            redFreeCount = redFreeCount + 1;
            redFreeIndexList.Add(recycleUnitIndex);
        }
        else if (campType == ECampType.Blue) 
        {
            blueFreeCount = blueFreeCount + 1;
            blueFreeIndexList.Add(recycleUnitIndex);
        }        
    }

    // 获取回收的UnitIndex
    public static int GetRecycleId(ECampType campType)
    {
        if (campType == ECampType.Red)
        {
            if (redFreeCount > 0)
            {
                int removeIndex = redFreeCount - 1;
                int unitIndex = redFreeIndexList[removeIndex];
                redFreeIndexList.RemoveAt(removeIndex);
                redFreeCount = redFreeCount - 1;
                return unitIndex;
            }
        }
        else if (campType == ECampType.Blue)
        {
            if (blueFreeCount > 0)
            {
                int removeIndex = blueFreeCount - 1;
                int unitIndex = blueFreeIndexList[removeIndex];
                blueFreeIndexList.RemoveAt(removeIndex);
                blueFreeCount = blueFreeCount - 1;
                return unitIndex;
            }
        }
        return -1;
    }

    private static int getUId(ECampType campType)
    {
        if(campType == ECampType.Red)
        {
            return ++redUId;
        }
        return ++blueUId;
    }

    private static void AddActiveCount(ECampType campType)
    {
        if (campType == ECampType.Red)
        {
            redActiveCount++;
        }
        else
        {
            blueActiveCount++;
        }
    }

    private static void RemoveActiveCount(ECampType campType)
    {
        if (campType == ECampType.Red)
        {
            redActiveCount = Mathf.Max(0, redActiveCount - 1);
        }
        else
        {
            blueActiveCount = Mathf.Max(0, blueActiveCount - 1);
        }
    }

    private static void AddReinforcementCount(ECampType campType)
    {
        if (campType == ECampType.Red)
        {
            redReinforcementCount++;
        }
        else
        {
            blueReinforcementCount++;
        }
    }

    private static void RemoveReinforcementCount(ECampType campType)
    {
        if (campType == ECampType.Red)
        {
            redReinforcementCount = Mathf.Max(0, redReinforcementCount - 1);
        }
        else
        {
            blueReinforcementCount = Mathf.Max(0, blueReinforcementCount - 1);
        }
    }
}
