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
    private ObstacleGroup dynamicObstacles = new ObstacleGroup();


    public void ManagerInit()
    {
        agents = new AgentGroup<Agent>();
        simulation = new ORCA();
        simulation.plane = Nebukam.Common.AxisPair.XZ;
        simulation.agents = Agents;
        simulation.dynamicObstacles = dynamicObstacles;
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
        if (simulation != null && simulation.TryComplete())
        {
           
        }
    }

    public void ManagerRefuse()
    {
       

    }

    public void ManagerDestroy()
    {
        agents.Clear();
        agents.Release();
        simulation.staticObstacles = null;
        simulation.dynamicObstacles = null;
        obstacles.Clear();
        dynamicObstacles.Clear(true);
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

    public Obstacle AddDynamicObstacle(List<Vector3> vertices)
    {
        if (vertices == null || vertices.Count < 2)
        {
            return null;
        }

        List<float3> obstacleVertices = new List<float3>(vertices.Count);
        for (int i = 0; i < vertices.Count; i++)
        {
            obstacleVertices.Add(vertices[i]);
        }

        return dynamicObstacles.Add(obstacleVertices, false, 10f);
    }

    public void RemoveDynamicObstacle(Obstacle obstacle)
    {
        if (obstacle == null)
        {
            return;
        }

        obstacle.Release();
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
     
