using Nebukam;
using Nebukam.ORCA;
using System.Diagnostics;
using Unity.Mathematics;
public static class UnitFactory
{
    // 创建一个单位
    public static UnitBase CreateUnit(float3 bornPoint, float3 forward, float radius)
    {
        UnitBase unit = new UnitBase();
        Agent agent = CreateAgent(bornPoint, forward, radius, unit.Prop.MaxSpeed);
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
}
