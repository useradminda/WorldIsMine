using Nebukam;
using Nebukam.ORCA;
using System.Collections.Generic;
using Unity.Mathematics;

public static class UnitFactory
{
    // 创建一个单位
    public static UnitLogicBase CreateUnit(int id, float3 bornPoint, float3 forward, ECampType campType)
    {
        UnitLogicBase unit = new UnitLogicBase(id, campType, forward);
        Agent agent = CreateAgent(bornPoint, forward, unit.Prop.Radius, unit.Prop.MaxSpeed);
        unit.BindAgent(agent);
        return unit;
    }

    // 创建一个RVO智能体
    public static Agent CreateAgent(float3 bornPoint, float3 forward, float radius, float maxSpeed)
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
    public static FlyObjectLogicBase CreateFlyObjectLogic(int flyObjectCfgId, float3 oriPos, float3 tarPos, UnitLogicBase atkUnitLogic, List<UnitLogicBase> beAtkUnitLogic, SkillLogicBase skillLogic)
    {
        FlyObjectLogicBase flyObjectLogic = new FlyObjectLogicBase();
        flyObjectLogic.SetFlyObjectInfo(flyObjectCfgId, oriPos, tarPos, atkUnitLogic, beAtkUnitLogic, skillLogic);
        return flyObjectLogic;
    }
}
