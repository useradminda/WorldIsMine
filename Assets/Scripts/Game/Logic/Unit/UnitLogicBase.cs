using UnityEngine;
using System.Collections.Generic;


public class UnitLogicBase
{
    private Nebukam.ORCA.Agent agenter;
    public Nebukam.ORCA.Agent Agenter => agenter;

    private UnitProp prop;
    public UnitProp Prop => prop;

    private BattlePlayer ownerPlayer;
    /// <summary>当前单位所属玩家，单位循环复用时会重新绑定。</summary>
    public BattlePlayer OwnerPlayer => ownerPlayer;
    /// <summary>当前单位所属玩家的简写访问属性。</summary>
    public BattlePlayer Player => ownerPlayer;
    /// <summary>英雄或士兵读取的完整玩家信息，包括 UID、名称、头像、胜点和队形。</summary>
    public BattlePlayer PlayerInfo => ownerPlayer;

    /// <summary>重新设置单位所属玩家，适用于特殊召唤或英雄重生时切换归属。</summary>
    public void SetOwnerPlayer(BattlePlayer player)
    {
        ownerPlayer = player;
    }

    /// <summary>
    /// 清理单位在本场战斗中的状态、Buff、搜索目标和玩家归属。
    /// </summary>
    public void ClearBattleRuntime()
    {
        stateMachine?.ExitState();
        buffLogicMachine?.Die();
        prop?.SetMaxHp(0, false);
        for (int i = 0; i < skillList.Count; i++)
        {
            skillList[i]?.ClearBattleRuntime();
        }
        searchTarget = null;
        searchTargetUId = -1;
        ownerPlayer = null;
        soldierHudGroupId = 0;
        UnitView = null;
        recycleIdQueued = false;
        crossedBattleCenter = false;
        externalPullDirection = Vector3.zero;
        externalPullMultiplier = 0f;
        externalPullRemainTime = 0f;
    }

    private Vector3 moveForward;

    public bool DirtyForward = true;

    private UnitLogicBase searchTarget;
    private int searchTargetUId;

    private Vector3 catchForward;
    public Vector3 TargetForward
    {
        get
        {
            Vector3 finalForward;
            if (searchTarget != null && searchTarget.IsDead == false && searchTarget.UId == searchTargetUId)
            {
                finalForward = Vector3.Normalize(searchTarget.GetClosestPoint(CurPos) - CurPos);
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

    public List<SkillLogicBase> SkillList => skillList;

    private StateMachine stateMachine;
    public StateMachine StateMachine => stateMachine;

    private BuffLogicMachine buffLogicMachine = new BuffLogicMachine();
    public BuffLogicMachine BuffLogicMachine => buffLogicMachine;

    public UnitView UnitView;

    public Vector3 CurPos
    {
        get
        {
            if(UnitType == EUnitType.Wall)
            {
                return UnitManager.Instance.GetWallCenter(CampType);
            }
            return Agenter.pos;
        }
    }

    public bool IsDead => Prop.Hp <= 0;

    public UnitLogicBase SearchTarget => searchTarget != null && searchTarget.IsDead == false && searchTarget.UId == searchTargetUId ? searchTarget : null;

    private int uid;
    public int UId => uid;

    private int index;
    public int Index => index;

    private int attackBuffValue;
    private bool recycleIdQueued;
    private bool crossedBattleCenter;
    private bool isReinforcement;
    private long soldierHudGroupId;
    private Vector3 externalPullDirection;
    private float externalPullMultiplier;
    private float externalPullRemainTime;
    public bool IsReinforcement => isReinforcement;
    public bool HasCrossedBattleCenter => crossedBattleCenter;
    public long SoldierHudGroupId => soldierHudGroupId;

    private float logicRatio = 1;
    public float LogicRatio
    {
        get
        {
            if (logicRatio <= 0f)
                return 0f;
            return logicRatio;
        }
    }

    private EUnitType eUnitType = EUnitType.Solider;
    public EUnitType UnitType => eUnitType;

    public UnitLogicBase(int cfgId, int uid, ECampType campType, Vector3 moveForward, int index, bool isReinforcement = false, BattlePlayer ownerPlayer = null)
    {
        this.index = index;
        this.uid = uid;
       
        this.campType = campType;
        this.campTypeInt = (int)campType;
        this.otherCampTypeInt = campType == ECampType.Blue ? (int)ECampType.Red : (int)ECampType.Blue;
        this.moveForward = Vector3.Normalize(moveForward);
        this.isReinforcement = isReinforcement;
        soldierHudGroupId = 0;
        // 重生或循环复用没有传入新玩家时，保留原召唤者；首次创建才使用阵营默认玩家。
        this.ownerPlayer = ownerPlayer ?? BattlePlayerManager.Instance.GetCampPlayer(campType);
        soliderCfg = SoliderCfgConfig.Ins.SearchById(cfgId);
        initProp();
        initSkills();
    }

    // 回收使用
    public void CycleUse(int cfgId, int uid, Vector3 moveForward, bool isReinforcement = false, BattlePlayer ownerPlayer = null)
    {
        buffLogicMachine.Die();
        soldierHudGroupId = 0;
        recycleIdQueued = false;
        crossedBattleCenter = false;
        this.uid = uid;
        this.moveForward = Vector3.Normalize(moveForward);
        searchTarget = null;
        searchTargetUId = -1;
        catchForward = Vector3.zero;
        DirtyForward = true;
        this.isReinforcement = isReinforcement;
        this.ownerPlayer = ownerPlayer ?? this.ownerPlayer ?? BattlePlayerManager.Instance.GetCampPlayer(campType);
        soliderCfg = SoliderCfgConfig.Ins.SearchById(cfgId);
        logicRatio = 1f;
        attackBuffValue = 0;
        externalPullDirection = Vector3.zero;
        externalPullMultiplier = 0f;
        externalPullRemainTime = 0f;
        initProp();
        initSkills();
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
        if (SoliderCfg != null && SoliderCfg.atkAnimTime > 0f)
        {
            UnitView.ActionFlowComponent.SetActionAnimationLength(EActionType.attack, SoliderCfg.atkAnimTime);
        }
    }

    public void RecycleAfterDeathView()
    {
        if (recycleIdQueued)
        {
            return;
        }
        if (UnitFactory.IsHero(SoliderCfg.id))
        {
            BattleHUDPanel.Instance?.RemoveHeroHud(this);
        }
        else if (soldierHudGroupId > 0)
        {
            BattleHUDPanel.Instance?.RemoveSoldierFromGroup(soldierHudGroupId, this);
            soldierHudGroupId = 0;
        }
        recycleIdQueued = true;
        UnitFactory.RecycleId(CampType, Index, UnitFactory.IsHero(SoliderCfg.id), IsReinforcement);
    }

    /// <summary>
    /// 记录该士兵所属的玩家召唤组 HUD 编号。
    /// </summary>
    public void SetSoldierHudGroupId(long groupId)
    {
        soldierHudGroupId = groupId;
    }

    public void UnitUpdate(float dt)
    {
        if (UnitType == EUnitType.Solider)
        {
            if (Agenter == null)
            {
                Debug.LogError("严重错误当前单位的Agent智能体是空的");
                return;
            }
            if (stateMachine != null)
            {
                stateMachine.UpdateState(dt);
            }
            ApplyExternalPull(dt);
            if (buffLogicMachine != null)
            {
                buffLogicMachine.UpdateBuffMachine(dt);
            }
            if (skillList.Count > 0)
            {
                for(int i = 0; i < skillList.Count; i++)
                {
                    skillList[i].SkillUpdate(dt);
                }
            }

            BattleCenterStoneController.Instance?.CheckUnitCrossing(this);
        }
    }

    public void MarkCrossedBattleCenter()
    {
        crossedBattleCenter = true;
    }

    public void UnitLateUpdate(float dt)
    {
        if (UnitType == EUnitType.Solider && stateMachine != null)
        {
            stateMachine.LateUpdateState(dt);
            ClampAgentInsideRvoBorder();
        }
    }

    /// <summary>
    /// 在RVO完成本帧计算后校正越过边界的单位位置，避免单位被挤出河道或场地范围。
    /// </summary>
    private void ClampAgentInsideRvoBorder()
    {
        if (IsDead || Agenter == null || !Agenter.navigationEnabled)
        {
            return;
        }

        Vector3 clampedPosition = RvoManager.Instance.ClampPositionInsideBorder(Agenter.pos, Agenter.radiusObst);
        if ((clampedPosition - (Vector3)Agenter.pos).sqrMagnitude > 0.0001f)
        {
            Agenter.pos = clampedPosition;
            Agenter.velocity = Vector3.zero;
        }
    }

    public int ChangeHp(int damage, string dieType, Vector3 beHitPoint)
    {
        if (IsDead)
        {
            return 0;
        }

        bool isAttack = damage < 0;
        if (damage < 0 && Prop.Hp + damage <= 0)
        {
            TryTriggerBeforeDieSkill();
        }
        BuffLogicMachine.ModifyDamage(ref damage);
        int counterDamage = BuffLogicMachine.RecordDamage(damage, isAttack);
        Prop.ChangeHp(damage);
        if (IsDead && BuffLogicMachine.TryPreventDeath())
        {
            SetSearchTarget(null);
            return counterDamage;
        }
        if (UnitType == EUnitType.Wall)
        {
            // 城门没有状态机和死亡动作，但每次受击仍走 UnitView 的闪白逻辑。
            if (UnitView != null)
            {
                UnitView.BeHitSlash(beHitPoint);
            }
            if (IsDead)
            {
                UnitManager.Instance.NotifyWallDestroyed(CampType);
            }

            return counterDamage;
        }

        if (UnitView != null)
        {
            UnitView.BeHitSlash();
        }
        if (IsDead && StateMachine != null)
            StateMachine.ChangeState(EStateTyep.Die, dieType, beHitPoint);

        return counterDamage;
    }

    public void KillNow(string dieType, Vector3 beHitPoint)
    {
        if (IsDead)
        {
            return;
        }

        Prop.ChangeHp(-Prop.Hp);
        SetSearchTarget(null);
        if (UnitView != null)
        {
            UnitView.BeHitSlash();
        }
        if (StateMachine != null)
        {
            StateMachine.ChangeState(EStateTyep.Die, dieType, beHitPoint);
        }
    }

    private bool TryTriggerBeforeDieSkill()
    {
        if (SkillList.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < SkillList.Count; i++)
        {
            SkillLogicBase skill = SkillList[i];
            if (!skill.IsTriggerBeforeDie)
            {
                continue;
            }

            if (skill.TryTriggerBeforeDie())
            {
                return true;
            }
        }

        return false;
    }
   
    public void TriggerMoveStop()
    {
        Agenter.navigationEnabled = false;
       // Agenter.collisionEnabled = false;
        Agenter.prefVelocity = Vector3.zero;
        Agenter.maxSpeed = 0;
    }

    public void TriggerMove()
    {
        RestoreAgentRadius();
        Agenter.navigationEnabled = true;
        Agenter.collisionEnabled = true;
    }

    /// <summary>
    /// 立即将单位逻辑、Agent 和表现模型同步到目标位置。
    /// </summary>
    public void TeleportTo(Vector3 position, Vector3 forward)
    {
        if (Agenter == null)
        {
            return;
        }

        Vector3 flatForward = forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude <= 0.0001f)
        {
            flatForward = Vector3.forward;
        }
        flatForward.Normalize();

        Agenter.pos = position;
        Agenter.prefVelocity = flatForward * SoliderCfg.moveSpeed * LogicRatio;
        Agenter.velocity = Agenter.prefVelocity;
        Agenter.navigationEnabled = true;
        Agenter.collisionEnabled = true;
        if (UnitView != null)
        {
            UnitView.SnapToPosition(position);
            UnitView.SetForwardForce(flatForward);
        }
    }

    public void SetCombatRadius()
    {
        float radius = Prop.Radius * Mathf.Clamp(RvoEscapeConfig.CombatRadiusScale, 0.1f, 1f);
        Agenter.radius = radius;
        Agenter.radiusObst = radius;
    }

    public void RestoreAgentRadius()
    {
        Agenter.radius = Prop.Radius;
        Agenter.radiusObst = Prop.Radius;
    }

    public void TriggerDie()
    {
        buffLogicMachine.Die();
        externalPullRemainTime = 0f;
        Agenter.navigationEnabled = false;
        Agenter.collisionEnabled = false;
        Agenter.prefVelocity = Vector3.zero;
        Agenter.maxSpeed = 0;
    }

    public void AgentMoveForward()
    {
        AgentMoveForward(Vector3.zero);
    }

    public void AgentMoveForward(Vector3 lateralDirection)
    {
        float agentSpeed = Agenter.saveMaxSpeed * LogicRatio;
        Agenter.maxSpeed = agentSpeed;
        Vector3 forward = TargetForward;
        forward.y = 0f;
        forward.Normalize();
        if (lateralDirection.sqrMagnitude > 0.0001f)
        {
            lateralDirection.y = 0f;
            lateralDirection.Normalize();
            float lateralWeight = Mathf.Max(0f, RvoEscapeConfig.LateralWeight);
            Vector3 escapeForward = forward + lateralDirection * lateralWeight;
            if (Vector3.Dot(escapeForward, forward) <= 0.2f)
            {
                escapeForward = forward + lateralDirection;
            }
            forward = escapeForward.normalized;
        }
        Agenter.prefVelocity = forward * SoliderCfg.moveSpeed * LogicRatio;
    }

    /// <summary>根据配置ID创建并添加Buff。</summary>
    public bool AddBuff(int buffCfgId, float duration = -1f)
    {
        if (IsDead || UnitType != EUnitType.Solider)
        {
            return false;
        }

        BuffLogicBase buffLogic = BuffLogicFactory.Create(
            buffCfgId,
            this,
            buffLogicMachine);
        return buffLogicMachine.AddBuff(buffLogic, duration);
    }

    /// <summary>修改单位逻辑速度百分比。</summary>
    public void ChangeLogicRatio(float addValue)
    {
        logicRatio += addValue;
    }

    /// <summary>Buff生效或退出时修改累计攻击增量，保留负值以支持减攻击Buff的正确恢复。</summary>
    public void ChangeAttackBuffValue(int value)
    {
        attackBuffValue += value;
    }

    /// <summary>根据单位基础攻击力、累计攻击增量和技能倍率计算技能基础伤害。</summary>
    public int GetAttackDamage(float skillDamageRate)
    {
        int finalAttack = Mathf.Max(0, SoliderCfg.atk + attackBuffValue);
        return System.Convert.ToInt32(Mathf.Max(0, finalAttack * skillDamageRate));
    }

    public void SetSearchTarget(UnitLogicBase logic)
    {
        searchTarget = logic;
        if (logic == null)
            searchTargetUId = -1;
        else
            searchTargetUId = logic.UId;
    }

    /// <summary>
    /// 提交一个短时外部牵引请求，供黑洞等范围控制效果在下一帧RVO计算前使用。
    /// </summary>
    public void SetExternalPull(Vector3 direction, float speedMultiplier, float duration)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        externalPullDirection = direction.normalized;
        externalPullMultiplier = Mathf.Max(0f, speedMultiplier);
        externalPullRemainTime = Mathf.Max(externalPullRemainTime, duration);
    }

    /// <summary>立即清理黑洞等技能施加的外部牵引，并停止残留的 Agent 位移。</summary>
    public void ClearExternalPull()
    {
        externalPullDirection = Vector3.zero;
        externalPullMultiplier = 0f;
        externalPullRemainTime = 0f;
        if (Agenter == null || IsDead)
        {
            return;
        }

        Agenter.prefVelocity = Vector3.zero;
        Agenter.velocity = Vector3.zero;
        Agenter.maxSpeed = Agenter.saveMaxSpeed * LogicRatio;
    }

    /// <summary>
    /// 将尚未过期的外部牵引请求写入Agent期望速度，确保本帧RVO使用牵引方向。
    /// </summary>
    private void ApplyExternalPull(float dt)
    {
        if (externalPullRemainTime <= 0f)
        {
            return;
        }

        externalPullRemainTime -= dt;
        if (externalPullRemainTime <= 0f || IsDead || Agenter == null)
        {
            externalPullRemainTime = 0f;
            externalPullMultiplier = 0f;
            if (Agenter != null && !IsDead)
            {
                Agenter.prefVelocity = Vector3.zero;
                Agenter.velocity = Vector3.zero;
                Agenter.maxSpeed = Agenter.saveMaxSpeed * LogicRatio;
            }
            return;
        }

        float effectiveSpeed = Agenter.saveMaxSpeed * externalPullMultiplier * LogicRatio;
        Agenter.navigationEnabled = true;
        Agenter.collisionEnabled = true;
        Agenter.maxSpeed = effectiveSpeed;
        Agenter.prefVelocity = externalPullDirection * effectiveSpeed;
    }

    public void SetMoveForward(Vector3 forward)
    {
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        moveForward = forward.normalized;
        catchForward = Vector3.zero;
        DirtyForward = true;
    }

    public void AgentMoveForward(Vector3 forward, float speedMultiplier)
    {
        if (Agenter == null)
        {
            return;
        }

        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        forward.Normalize();
        float safeMultiplier = Mathf.Max(0f, speedMultiplier);
        float effectiveSpeed = Agenter.saveMaxSpeed * safeMultiplier * LogicRatio;
        Agenter.navigationEnabled = true;
        Agenter.collisionEnabled = true;
        Agenter.maxSpeed = effectiveSpeed;
        Agenter.prefVelocity = forward * effectiveSpeed;
    }

    // 设置类型
    public void SetUnitType(EUnitType unitType)
    {
        eUnitType = unitType;
    }

    // 获取判断最近的点
    public Vector3 GetClosestPoint(Vector3 attackerPosition)
    {
        if (UnitType != EUnitType.Wall)
        {
            return CurPos;
        }
        Vector3 wallCenter = UnitManager.Instance.GetWallCenter(CampType);
        Vector3 wallSize = UnitManager.Instance.GetWallSize();
        Vector3 point = new Vector3
        (
            Mathf.Clamp(
                attackerPosition.x,
                wallCenter.x - wallSize.x * 0.5f,
                wallCenter.x + wallSize.x * 0.5f
            ),
            attackerPosition.y,
            Mathf.Clamp(
                attackerPosition.z,
                wallCenter.z - wallSize.z * 0.5f,
                wallCenter.z + wallSize.z * 0.5f
            )
        );
        return point;
    }

    /// <summary>
    /// 判断目标是否在攻击范围内。普通单位按双方圆形半径计算，城墙按墙面最近点计算。
    /// </summary>
    public bool IsTargetInAttackRange(
        UnitLogicBase target,
        float attackRange,
        float extraRange = 0f)
    {
        if (target == null || target.IsDead)
        {
            return false;
        }

        Vector3 targetPoint = target.GetClosestPoint(CurPos);
        Vector3 offset = targetPoint - CurPos;
        offset.y = 0f;

        float effectiveRange = Mathf.Max(0f, attackRange)
            + Mathf.Max(0f, Prop.Radius)
            + Mathf.Max(0f, extraRange);

        if (target.UnitType != EUnitType.Wall)
        {
            effectiveRange += Mathf.Max(0f, target.Prop.Radius);
        }

        return offset.sqrMagnitude <= effectiveRange * effectiveRange;
    }

    /// <summary>
    /// 判断目标是否位于敌方存活城墙后方，避免单位为了追击目标穿过敌方城门。
    /// </summary>
    public bool IsTargetBlockedByEnemyWall(UnitLogicBase target)
    {
        if (target == null || target.IsDead || target.UnitType == EUnitType.Wall || target.CampType == CampType)
        {
            return false;
        }

        UnitLogicBase enemyWall = UnitManager.Instance.GetOtherWallLogic(CampType);
        if (enemyWall == null || enemyWall.IsDead)
        {
            return false;
        }

        // 索敌阻挡判断使用城墙逻辑点，不使用城门模型出生点，避免模型点偏移导致城内单位被误判为城外单位。
        Vector3 wallCenter = UnitManager.Instance.GetWallCenter(enemyWall.CampType);
        Vector3 wallSize = UnitManager.Instance.GetWallSize();
        float expand = Mathf.Max(0f, Prop.Radius) + BattleDefine.SpawnUnitPadding;
        float halfDepth = wallSize.z * 0.5f + expand;
        Vector3 wallForward = enemyWall.CampType == ECampType.Red ? Vector3.forward : Vector3.back;
        float targetDepth = Vector3.Dot(target.CurPos - wallCenter, wallForward);
        // 目标只要位于敌方城墙内部，就不能作为普通索敌目标；不再依赖攻击者当前是否已经在城墙内部。
        return targetDepth < -halfDepth;
    }

    private void initProp()
    {
        prop = new UnitProp(soliderCfg.hp, soliderCfg.radius, soliderCfg.moveSpeed);
    }

    private void initSkills()
    {
        normalSkill = null;
        skillList.Clear();
        for (int i = 0; i < soliderCfg.skill.Length; i++)
        {
            SkillCfg skillCfg = SkillCfgConfig.Ins.SearchById(soliderCfg.skill[i]);
            if (skillCfg != null)
            {
                SkillLogicBase skill = SkillFactory.CreateSkillLogic(this, skillCfg);
                if (skill == null)
                {
                    continue;
                }

                if (skill.BNormalSkill)
                    normalSkill = skill;
                skillList.Add(skill);
            }
        }
    }
}
