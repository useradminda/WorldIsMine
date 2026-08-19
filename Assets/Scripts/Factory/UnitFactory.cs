
using Nebukam;
using Nebukam.ORCA;
using System.Collections.Generic;
using UnityEngine;

public static class UnitFactory
{
    private static int blueUId = 0;
    private static int redUId = 0;

    private static int redFreeCount = 0;
    private static List<int> redFreeIndexList = new List<int>();

    private static int blueFreeCount = 0;
    private static List<int> blueFreeIndexList = new List<int>();
    
    // 创建一个单位
    public static UnitLogicBase CreateUnit(
        int cfgId,
        Vector3 bornPoint,
        Vector3 moveForward,
        ECampType campType, int index)
    {
        UnitLogicBase unit = new UnitLogicBase(cfgId, getUId(campType), campType, moveForward, index);
        return unit;
    }

    // 获取UnitCatch
    public static UnitLogicBase GetUnitCatch(int cfgId,
        Vector3 bornPoint,
        Vector3 moveForward,
        ECampType campType)
    {
        int unitIndex = GetRecycleId(campType);
        if (unitIndex == -1)
        {
            return null;
            
        }
        UnitLogicBase unit = UnitManager.Instance.UnitList[unitIndex];
        unit.CycleUse(cfgId, getUId(campType), moveForward);
        unit.Agenter.pos = bornPoint;
        unit.Agenter.prefVelocity = moveForward;
        unit.Agenter.velocity = moveForward;
        unit.Agenter.radius = unit.Prop.Radius;
        unit.Agenter.radiusObst = unit.Prop.Radius;
        unit.Agenter.maxNeighbors = 20;
        unit.Agenter.timeHorizon = 0.1f;   // 距离其他代理检查
        unit.Agenter.timeHorizonObst = 4f; // 速度越小，这个值越大，才不会穿透不可行走区域，距离障碍
        unit.Agenter.saveMaxSpeed = unit.Prop.MaxSpeed;
        unit.Agenter.maxSpeed = unit.Prop.MaxSpeed;
        return unit;
    }

    // 创建一个RVO智能体
    public static Agent CreateAgent(Vector3 bornPoint, Vector3 forward, float radius, float maxSpeed)
    {        
        Agent agent = Pool.Rent<Agent>();   
        agent.pos = bornPoint;
        agent.prefVelocity = forward;
        agent.velocity = forward;
        agent.radius = radius;
        agent.radiusObst = radius;
        agent.maxNeighbors = 20;
        agent.timeHorizon = 0.1f;   // 距离其他代理检查
        agent.timeHorizonObst = 4f; // 速度越小，这个值越大，才不会穿透不可行走区域，距离障碍
        agent.saveMaxSpeed = maxSpeed;
        agent.maxSpeed = maxSpeed;
        return agent;
    }

    // 创建一个飞行物逻辑
    public static FlyObjectLogicBase CreateFlyObjectLogic(int flyObjectCfgId, Vector3 oriPos, Vector3 tarPos, UnitLogicBase atkUnitLogic, List<UnitLogicBase> targetLogicList, UnitLogicBase searchTargetLogic, SkillLogicBase skillLogic, int damage)
    {
        FlyObjectLogicBase flyObjectLogic = null;
        FlyObjectCfg flyObjectCfg = FlyObjectCfgConfig.Ins.SearchById(flyObjectCfgId);
        if (flyObjectCfg.flyType == "arrow")
        {
            flyObjectLogic = new ArrowFlyObject();
        }
        else if (flyObjectCfg.flyType == "area")
        {
            flyObjectLogic = new AreaFlyObject();
            FlyObjectManager.Instance.AddFlyUnitImmediately(flyObjectLogic);
        }
        else
        {
            flyObjectLogic = new FlyObjectLogicBase();
            FlyObjectManager.Instance.AddFlyUnitImmediately(flyObjectLogic);
        }
        flyObjectLogic.SetFlyObjectInfo(flyObjectCfg, oriPos, tarPos, atkUnitLogic, targetLogicList, searchTargetLogic, skillLogic, damage);
        return flyObjectLogic;
    }

    public static void RemoveFlyObjectLogic(FlyObjectLogicBase flyObjectLogic)
    {
        FlyObjectManager.Instance.RemoveFlyUnitImmediately(flyObjectLogic);
    }

    // 回收UnitIndex
    public static void RecycleId(ECampType campType, int recycleUnitIndex)
    {
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
}
