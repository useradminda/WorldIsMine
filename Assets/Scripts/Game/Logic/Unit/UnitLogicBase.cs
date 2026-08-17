using UnityEngine;
using System.Collections.Generic;

public class UnitLogicBase
{
    private Nebukam.ORCA.Agent agenter;
    public Nebukam.ORCA.Agent Agenter => agenter;

    private UnitProp prop;
    public UnitProp Prop => prop;

    private Vector3 moveForward;

    public bool DirtyForward = true;

    private Vector3 catchForward;
    public Vector3 TargetForward
    {
        get
        {
            Vector3 finalForward;
            if (NormalSkill.TargetList.Count > 0 && NormalSkill.TargetList[0] != null && NormalSkill.TargetList[0].IsDead == false)
            {
                finalForward = Vector3.Normalize(NormalSkill.TargetList[0].CurPos - NormalSkill.UnitLogic.CurPos);
            }
            else
            {
                finalForward = moveForward;
            }
            if (catchForward != finalForward)
            {
                DirtyForward = true;

                catchForward = finalForward;
            }
            return finalForward;
        }
    }

    private ECampType campType;
    public ECampType CampType => campType;

    private int campTypeInt;
    public int CampTypeInt => campTypeInt;

    private int otherCampTypeInt = -1;
    public int OtherCampTypeInt => otherCampTypeInt;

    private SoliderCfg soliderCfg;
    public SoliderCfg SoliderCfg => soliderCfg;

    private SkillLogicBase normalSkill;
    public SkillLogicBase NormalSkill => normalSkill;

    private List<SkillLogicBase> skillList = new List<SkillLogicBase>();

    private StateMachine stateMachine;
    public StateMachine StateMachine => stateMachine;

    public UnitView UnitView;

    public Vector3 CurPos => Agenter.pos;

    public bool IsDead => Prop.Hp <= 0;

    private int uid;
    public int UId => uid;

    private int index;
    public int Index => index;

    private bool isFree = true;

    public UnitLogicBase(int cfgId, int uid, ECampType campType, Vector3 moveForward, int index)
    {
        this.index = index;
        this.uid = uid;
       
        this.campType = campType;
        this.campTypeInt = (int)campType;
        this.otherCampTypeInt = campType == ECampType.Blue ? (int)ECampType.Red : (int)ECampType.Blue;
        this.moveForward = Vector3.Normalize(moveForward);
        soliderCfg = SoliderCfgConfig.Ins.SearchById(cfgId);
        initProp();
        initSkills();
        isFree = false;
    }

    // 回收使用
    public void CycleUse(int cfgId, int uid, Vector3 moveForward)
    {
        this.uid = uid;
        this.moveForward = Vector3.Normalize(moveForward);
        soliderCfg = SoliderCfgConfig.Ins.SearchById(cfgId);
        initProp();
        initSkills();
        isFree = false;
    }

    public void InitStateMachine()
    {
        stateMachine = new StateMachine(this);
    }

    // 绑定一个agent
    public void BindAgent(Nebukam.ORCA.Agent agenter)
    {
        this.agenter = agenter;
    }

    // 绑一个表现
    public void BindUnitView(UnitView unitView)
    {
        this.UnitView = unitView;
    }

    public void UnitUpdate(float dt)
    {
        if (isFree == true)
            return;
        if (Agenter == null)
        {
            Debug.LogError("严重错误当前单位的Agent智能体是空的");
            return;
        }
        if (stateMachine != null)
        {
            stateMachine.UpdateState(dt);
        }
    }

    public void ChangeHp(int damage)
    {
        Prop.ChangeHp(damage);
        if (IsDead)
            StateMachine.ChangeState(EStateTyep.Die);
    }
   

    public void MoveStop()
    {
        Agenter.navigationEnabled = false;
        Agenter.prefVelocity = Vector3.zero;
        Agenter.maxSpeed = 0;
    }

    public void TriggerMove()
    {
        Agenter.navigationEnabled = true;
        Agenter.collisionEnabled = true;
    }

    public void TriggerDie()
    {
        Agenter.navigationEnabled = false;
        Agenter.collisionEnabled = false;
        Agenter.prefVelocity = Vector3.zero;
        Agenter.maxSpeed = 0;
    }

    public void MoveForward()
    {
        float agentSpeed = Agenter.saveMaxSpeed;
        Agenter.maxSpeed = agentSpeed;
        Agenter.prefVelocity = TargetForward.normalized * SoliderCfg.moveSpeed;
    }

    public void Refuse()
    {
        isFree = true;
    }

    private void initProp()
    {
        prop = new UnitProp(soliderCfg.hp, soliderCfg.radius, soliderCfg.moveSpeed);
    }

    private void initSkills()
    {
        skillList.Clear();
        for (int i = 0; i < soliderCfg.skill.Length; i++)
        {
            SkillCfg skillCfg = SkillCfgConfig.Ins.SearchById(soliderCfg.skill[i]);
            SkillLogicBase skill = null;
            if (skillCfg.skillType == 1)
            {
                skill = new SkillCloseSkill(this, skillCfg);
            }
            else if(skillCfg.skillType == 2)
            {
                skill = new SkillRemoteSkill(this, skillCfg); // new SkillRemoteSkill(this, skillCfg);
            }
            if (skill.BNormalSkill)
            {
                normalSkill = skill;
            }
            skillList.Add(skill);
        }
    }
}
