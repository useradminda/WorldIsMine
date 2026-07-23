using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using System.Collections.Generic;
public class UnitLogicBase
{
    private Nebukam.ORCA.Agent agenter;
    public Nebukam.ORCA.Agent Agenter => agenter;

    private UnitProp prop;
    public UnitProp Prop => prop;

    private float3 targetForward;

    public ECampType CampType => campType;
    private ECampType campType;

    private SoliderCfg soliderCfg;
    public SoliderCfg SoliderCfg => soliderCfg;

    private SkillLogicBase normalSkill;
    public SkillLogicBase NormalSkill => normalSkill;

    private List<SkillLogicBase> skillList = new List<SkillLogicBase>();

    private StateMachine stateMachine;
    public StateMachine StateMachine => stateMachine;

    public bool IsDead => Prop.Hp <= 0;

    public UnitLogicBase(int id, ECampType campType, float3 targetForward)
    {
        stateMachine = new StateMachine(this);
        this.campType = campType;
        this.targetForward = targetForward;
        soliderCfg = SoliderCfgConfig.Ins.SearchById(id);
        initProp();
        initSkills();
    }

    // 绑定一个agent
    public void BindAgent(Nebukam.ORCA.Agent agenter)
    {
        this.agenter = agenter;
    }

    public void UnitUpdate(float dt)
    {
        if (stateMachine != null)
        {
            stateMachine.UpdateState(dt);
        }
        if (Agenter == null)
        {
            Debug.LogError("当前单位的智能体是空的");
            return;
        }
        float agentSpeed = Agenter.saveMaxSpeed;  
        Agenter.maxSpeed = agentSpeed;     
        Agenter.prefVelocity = normalize(this.targetForward) * agentSpeed;
    }

    // 获取普工攻击范围
    public float GetNormalAttackRange()
    {
        return normalSkill.SkillRange;
    }

    public void BeAttack(UnitLogicBase damageFromUnit, int damgeValue)
    {
        if(IsDead == false)
        {
            prop.ChangeHp(damgeValue);
        }
    }

    private void initProp()
    {
        prop = new UnitProp(soliderCfg.hp, soliderCfg.radius, soliderCfg.moveSpeed);
    }

    private void initSkills()
    {
        for (int i = 0; i < soliderCfg.skill.Length; i++)
        {
            SkillCfg skillCfg = SkillCfgConfig.Ins.SearchById(i);
            SkillLogicBase skill = new SkillLogicBase(this, skillCfg);
            if (skill.BNormalSkill)
            {
                normalSkill = skill;
            }
            skillList.Add(skill);
        }
    }
}
