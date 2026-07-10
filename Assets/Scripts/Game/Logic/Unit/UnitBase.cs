using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;
public class UnitBase
{
    private Nebukam.ORCA.Agent agenter;
    public Nebukam.ORCA.Agent Agenter => agenter;

    private UnitProp prop;
    public UnitProp Prop => prop;

    private float3 tarPos;

    public UnitBase()
    {
        prop = new UnitProp(1, 1);
    }

    // 绑定一个agent
    public void BindAgent(Nebukam.ORCA.Agent agenter)
    {
        this.agenter = agenter;
    }

    public void UpdateMove()
    {
        if (Agenter == null)
        {
            Debug.LogError("当前单位的智能体是空的");
            return;
        }
        float agentSpeed = Agenter.saveMaxSpeed;  
        Agenter.maxSpeed = agentSpeed;     
        Agenter.prefVelocity = normalize(tarPos - Agenter.pos) * agentSpeed;
    }
}
