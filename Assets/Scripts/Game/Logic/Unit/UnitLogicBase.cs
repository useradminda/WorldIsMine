using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using System.Collections.Generic;
using System;
public class UnitLogicBase
{
    private Nebukam.ORCA.Agent agenter;
    public Nebukam.ORCA.Agent Agenter => agenter;

    private UnitProp prop;
    public UnitProp Prop => prop;

    private float3 targetPoint;
    public float3 TargetPoint => targetPoint;

    public ECampType CampType => campType;
    private ECampType campType;

    private SoliderCfg soliderCfg;
    public SoliderCfg SoliderCfg => soliderCfg;

    private SkillLogicBase normalSkill;
    public SkillLogicBase NormalSkill => normalSkill;

    private List<SkillLogicBase> skillList = new List<SkillLogicBase>();

    private StateMachine stateMachine;
    public StateMachine StateMachine => stateMachine;

    public Vector3 CurPos => Agenter.pos;

    public bool IsDead => Prop.Hp <= 0;

    public UnitLogicBase(int id, ECampType campType, float3 targetPoint)
    {
        stateMachine = new StateMachine(this);
        this.campType = campType;
        this.targetPoint = targetPoint;
        soliderCfg = SoliderCfgConfig.Ins.SearchById(id);
        if (soliderCfg == null)
            throw new InvalidOperationException($"Soldier config was not found. UnitId={id}");

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
        float3 toTarget = targetPoint - Agenter.pos;
        toTarget.y = 0f;
        float arrivalDistance = max(0.5f, Prop.Radius);
        if (lengthsq(toTarget) <= arrivalDistance * arrivalDistance)
        {
            Agenter.prefVelocity = new float3(0f, 0f, 0f);
            return;
        }

        Agenter.prefVelocity = normalize(toTarget) * agentSpeed;
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
            int skillId = soliderCfg.skill[i];
            SkillCfg skillCfg = SkillCfgConfig.Ins.SearchById(skillId);
            if (skillCfg == null)
            {
                Debug.LogError(
                    $"技能配置不存在。UnitId={soliderCfg.id}, SkillId={skillId}");
                continue;
            }

            SkillLogicBase skill = new SkillLogicBase(this, skillCfg);
            if (skill.BNormalSkill)
            {
                normalSkill = skill;
            }
            skillList.Add(skill);
        }
    }
}
