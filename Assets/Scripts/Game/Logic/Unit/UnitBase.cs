using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;
public class UnitLogicBase
{
    private Nebukam.ORCA.Agent agenter;
    public Nebukam.ORCA.Agent Agenter => agenter;

    private UnitProp prop;
    public UnitProp Prop => prop;

    private float3 targetForward;

    public ECampType CampType => campType;
    private ECampType campType;



    public UnitLogicBase(ECampType campType, float3 targetForward)
    {
        this.campType = campType;
        this.targetForward = targetForward;
        prop = new UnitProp(1, 1);
    }

    // 绑定一个agent
    public void BindAgent(Nebukam.ORCA.Agent agenter)
    {
        this.agenter = agenter;
    }

    public void UnitUpdate()
    {
        if (Agenter == null)
        {
            Debug.LogError("当前单位的智能体是空的");
            return;
        }
        float agentSpeed = Agenter.saveMaxSpeed;  
        Agenter.maxSpeed = agentSpeed;     
        Agenter.prefVelocity = normalize(this.targetForward) * agentSpeed;
    }


}
