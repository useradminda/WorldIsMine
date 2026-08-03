using Nebukam;
using Nebukam.ORCA;
using System.Collections.Generic;

using UnityEngine;
public static class UnitFactory
{
    // 创建一个单位
    public static UnitLogicBase CreateUnit(
        int id,
        Vector3 bornPoint,
        Vector3 moveForward,
        ECampType campType)
    {
        moveForward.y = 0f;
        UnitLogicBase unit = new UnitLogicBase(id, campType, moveForward);
        Agent agent = CreateAgent(bornPoint, moveForward, unit.Prop.Radius, unit.Prop.MaxSpeed);
        unit.BindAgent(agent);
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
        return agent;
    }

    // 创建一个飞行物逻辑
    public static FlyObjectLogicBase CreateFlyObjectLogic(int flyObjectCfgId, Vector3 oriPos, Vector3 tarPos, UnitLogicBase atkUnitLogic, List<UnitLogicBase> targetLogicList, SkillLogicBase skillLogic)
    {
        FlyObjectLogicBase flyObjectLogic = null;
        FlyObjectCfg flyObjectCfg = FlyObjectCfgConfig.Ins.SearchById(flyObjectCfgId);
        if (flyObjectCfg.flyType == "arrow")
        {
            flyObjectLogic = new ArrowFlyObject();
        }
        else
        {
            flyObjectLogic = new FlyObjectLogicBase();
        }
        flyObjectLogic.SetFlyObjectInfo(flyObjectCfg, oriPos, tarPos, atkUnitLogic, targetLogicList, skillLogic);
        FlyObjectManager.Instance.AddUnit(flyObjectLogic);
        return flyObjectLogic;
    }
}
