using Nebukam.ORCA;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using ZTools;

// Rvo
public class RvoManager : Singleton<RvoManager>, IManager
{
    private AgentGroup<Agent> agents;
    public AgentGroup<Agent> Agents => agents;

    private ORCA simulation;
    private ObstacleGroup obstacles = new ObstacleGroup();


    public void ManagerInit()
    {
        agents = new AgentGroup<Agent>();
        simulation = new ORCA();
        simulation.plane = Nebukam.Common.AxisPair.XZ;
        simulation.agents = Agents;
    }

    public void SetBorderInfo(List<Vector3> borderList)
    {
        setBorderInfo(borderList);
    }

    public void ManagerUpdate(float dt)
    {
        if (simulation != null)
        {
            simulation.Schedule(Time.deltaTime);
        }
    }

    public void ManagerLateUpdate(float dt)
    { 
       
    }

    public void ManagerRefuse()
    {
       

    }

    public void ManagerDestroy()
    {
        agents.Clear();
        agents.Release();
        simulation.staticObstacles = null;
        obstacles.Clear();
        simulation.agents = null;
        simulation.DisposeAll();
        simulation = null;
    }

    public Agent AddAgentImmediately(Agent agent)
    {
        Nebukam.Common.IVertex a = (Nebukam.Common.IVertex)agent;
        Agents.Add(a);
        return agent;
    }

   

    // 设置边界
    private void setBorderInfo(List<Vector3> borderList)
    {
        if (borderList.Count <= 0)
            return;
        float3[] squarePoints = new float3[] {
              new float3(borderList[0].x, 0, borderList[0].z),
              new float3(borderList[1].x, 0, borderList[1].z),
              new float3(borderList[2].x, 0, borderList[2].z),
              new float3(borderList[3].x, 0, borderList[3].z),
           };
        obstacles.Add(squarePoints, false, 10);
        simulation.staticObstacles = obstacles;
    }
}    
     