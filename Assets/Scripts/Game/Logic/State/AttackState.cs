using System.Collections.Generic;
using UnityEngine;

public class AttackState : StateBase
{
    private const float AttackExitRangeBuffer = 0.3f;

    public override EStateTyep StateType { get { return EStateTyep.Attack; } }

    private SkillLogicBase normalSkill;
    private float attackTime;
    private float attackAnimationLength;
    private float attackEffectTime;
    private bool attackEffectTriggered;
    private bool attackEffectPending;
    private int effectSearchReqIndex = -1;
    private int targetSearchReqIndex = -1;
    private int targetNearestIndex = -1;
    private int targetRandomIndex = -1;
    private int nearestIndex = -1;
    private int randomIndex = -1;
    private Vector3 attackDirection;
    private readonly List<int> resultUnitIndexList = new List<int>();
    private readonly List<int> targetSearchResultList = new List<int>();

    public AttackState(UnitLogicBase ulb) : base(ulb)
    {
    }

    public override void EnterState(params object[] objects)
    {
        UnitLogic.TriggerMoveStop();
        UnitLogic.SetCombatRadius();
        UnitLogic.UnitView.EnterState(EStateTyep.Attack);
        normalSkill = UnitLogic.NormalSkill;
        effectSearchReqIndex = -1;
        targetSearchReqIndex = -1;
        targetSearchResultList.Clear();
        resultUnitIndexList.Clear();
        StartNormalAttack();
    }

    public override void UpdateState(float dt)
    {
        if (UnitLogic.IsDead || normalSkill == null)
        {
            return;
        }

        if (attackTime > 0f)
        {
            float deltaTime = dt * UnitLogic.LogicRatio;
            attackTime -= deltaTime;
            attackEffectTime -= deltaTime;
            if (!attackEffectTriggered && attackEffectTime <= 0f)
            {
                RequestNormalAttackEffect();
            }
            if (attackTime <= 0f)
            {
                attackTime = 0f;
                // 英雄的攻击动画由 AttackStopComponent 在动画真正结束时切换 wait。
                // 普通小兵保持原有的逻辑时间切换方式。
                if (!IsHeroNormalAttack())
                {
                    UnitLogic.UnitView.ActionFlowComponent.PlayAction(EActionType.wait);
                }
            }
            return;
        }

        if (attackEffectPending)
        {
            return;
        }

        if (UnitLogic.SearchTarget == null || !UnitLogic.IsTargetInAttackRange(UnitLogic.SearchTarget, normalSkill.SkillCfg.atkRange, AttackExitRangeBuffer))
        {
            RequestTargetSearch();
            return;
        }

        if (HasReadySkillInRange())
        {
            UnitLogic.StateMachine.ChangeState(EStateTyep.Skill);
            return;
        }

        if (normalSkill.CurCD <= 0f)
        {
            if (IsHeroNormalAttack() && UnitLogic.UnitView.AttackStopComp.IsAttackAnimationPlaying)
            {
                return;
            }
            StartNormalAttack();
        }
    }

    public override void LateUpdateState(float dt)
    {
        if (UnitLogic.IsDead || normalSkill == null)
        {
            return;
        }

        if (targetSearchReqIndex >= 0)
        {
            ResolveTargetSearch();
            return;
        }

        if (!attackEffectPending)
        {
            return;
        }

        if (normalSkill.SkillCfg.skillArea > 0)
        {
            if (effectSearchReqIndex < 0)
            {
                return;
            }
            resultUnitIndexList.Clear();
            MapCellManager.Instance.GetResult(effectSearchReqIndex, resultUnitIndexList, ref nearestIndex, ref randomIndex);
            effectSearchReqIndex = -1;
        }

        ExecuteNormalAttackEffect();
    }

    public override void ExitState()
    {
        if (UnitLogic.UnitView != null && IsHeroNormalAttack())
        {
            UnitLogic.UnitView.AttackStopComp.Cancel();
        }
        UnitLogic.RestoreAgentRadius();
        if (normalSkill != null)
        {
            normalSkill.SkillRelease();
        }
        normalSkill = null;
        attackTime = 0f;
        attackAnimationLength = 0f;
        attackEffectTime = 0f;
        attackEffectTriggered = false;
        attackEffectPending = false;
        effectSearchReqIndex = -1;
        targetSearchReqIndex = -1;
        targetSearchResultList.Clear();
        resultUnitIndexList.Clear();
    }

    private void RequestTargetSearch()
    {
        if (normalSkill == null || targetSearchReqIndex >= 0)
        {
            return;
        }
        targetSearchReqIndex = MapCellManager.Instance.RequestSearch(UnitLogic.CurPos, GetTargetSearchRange(), UnitLogic.OtherCampTypeInt);
        UnitLogic.TriggerMoveStop();
        UnitLogic.UnitView.ActionFlowComponent.PlayAction(EActionType.wait);
    }

    private float GetTargetSearchRange()
    {
        float searchRange = normalSkill.SkillSearchRange;
        for (int i = 0; i < UnitLogic.SkillList.Count; i++)
        {
            SkillLogicBase skill = UnitLogic.SkillList[i];
            if (skill.BNormalSkill == false)
            {
                searchRange = Mathf.Max(searchRange, skill.SkillSearchRange);
            }
        }
        return searchRange;
    }

    private void ResolveTargetSearch()
    {
        targetSearchResultList.Clear();
        MapCellManager.Instance.GetResult(targetSearchReqIndex, targetSearchResultList, ref targetNearestIndex, ref targetRandomIndex);
        targetSearchReqIndex = -1;
        UnitLogicBase target = targetNearestIndex >= 0 && targetNearestIndex < UnitManager.Instance.UnitList.Count
            ? UnitManager.Instance.UnitList[targetNearestIndex]
            : null;
        if (target != null && UnitLogic.IsTargetBlockedByEnemyWall(target))
        {
            target = UnitManager.Instance.GetOtherWallLogic(UnitLogic.CampType);
        }
        if (target == null || target.IsDead || target.CampType == UnitLogic.CampType)
        {
            UnitLogic.SetSearchTarget(null);
            UnitLogic.StateMachine.ChangeState(EStateTyep.Move);
            return;
        }
        UnitLogic.SetSearchTarget(target);
        if (HasReadySkillInRange())
        {
            UnitLogic.StateMachine.ChangeState(EStateTyep.Skill);
            return;
        }
        if (!UnitLogic.IsTargetInAttackRange(target, normalSkill.SkillCfg.atkRange, AttackExitRangeBuffer))
        {
            UnitLogic.StateMachine.ChangeState(EStateTyep.Move);
            return;
        }
        if (normalSkill.CurCD <= 0f)
        {
            StartNormalAttack();
        }
        else
        {
            UnitLogic.UnitView.ActionFlowComponent.PlayAction(EActionType.wait);
        }
    }

    private void StartNormalAttack()
    {
        if (normalSkill == null || UnitLogic.SearchTarget == null || UnitLogic.SearchTarget.IsDead)
        {
            attackTime = 0f;
            return;
        }

        if (IsHeroNormalAttack() && UnitLogic.UnitView.AttackStopComp.IsAttackAnimationPlaying)
        {
            return;
        }

        if (normalSkill.CurCD > 0f)
        {
            UnitLogic.UnitView.ActionFlowComponent.PlayAction(EActionType.wait);
            attackTime = 0f;
            return;
        }

        ActionFlow actionFlow = UnitLogic.UnitView.ActionFlowComponent;
        // 普通攻击从动作开始计时，避免红蓝单位的攻击动画长度差异改变实际攻击频率。
        normalSkill.StartAttackCooldown();
        actionFlow.PlayAction(string.IsNullOrWhiteSpace(normalSkill.SkillCfg.skillAnim)
            ? EActionType.attack.ToString()
            : normalSkill.SkillCfg.skillAnim);
        attackTime = actionFlow.CurrentActionData != null
            ? actionFlow.CurrentActionData.AnimLen
            : actionFlow.GetAnimLen(EActionType.attack);
        attackAnimationLength = attackTime;
        if (IsHeroNormalAttack())
        {
            UnitLogic.UnitView.AttackStopComp.BeginAttackAnimation(UnitLogic, attackAnimationLength);
        }
        attackEffectTime = attackTime * 0.5f;
        attackEffectTriggered = false;
        attackEffectPending = false;
        effectSearchReqIndex = -1;
        resultUnitIndexList.Clear();
        attackDirection = UnitLogic.SearchTarget.GetClosestPoint(UnitLogic.CurPos) - UnitLogic.CurPos;
        attackDirection.y = 0f;
        if (attackDirection.sqrMagnitude <= 0.0001f)
        {
            attackDirection = UnitLogic.TargetForward;
            attackDirection.y = 0f;
        }
        attackDirection.Normalize();
        UnitLogic.UnitView.SetForwardForce(attackDirection);
        if (attackTime <= 0f)
        {
            RequestNormalAttackEffect();
        }
    }

    /// <summary>请求普通攻击效果；范围普攻以当前主目标位置为圆心搜索附加目标。</summary>
    private void RequestNormalAttackEffect()
    {
        attackEffectTriggered = true;
        if (UnitLogic.SearchTarget == null || UnitLogic.SearchTarget.IsDead)
        {
            attackEffectPending = false;
            return;
        }

        attackEffectPending = true;
        if (normalSkill.SkillCfg.skillArea > 0)
        {
            effectSearchReqIndex = MapCellManager.Instance.RequestSearch(
                UnitLogic.SearchTarget.CurPos,
                normalSkill.SkillCfg.skillArea,
                UnitLogic.OtherCampTypeInt);
        }
    }

    private void ExecuteNormalAttackEffect()
    {
        if (UnitLogic.SearchTarget != null && !UnitLogic.SearchTarget.IsDead)
        {
            normalSkill.SkillDoEffect(UnitLogic.SearchTarget, resultUnitIndexList);
            if (!UnitLogic.IsDead && UnitLogic.UnitView != null
                && UnitLogic.SoliderCfg != null
                && UnitFactory.IsHero(UnitLogic.SoliderCfg.id))
            {
                UnitLogic.UnitView.AttackStopComp.TryStart(UnitLogic);
            }
        }
        attackEffectPending = false;
    }

    private bool HasReadySkillInRange()
    {
        for (int i = 0; i < UnitLogic.SkillList.Count; i++)
        {
            SkillLogicBase skill = UnitLogic.SkillList[i];
            if (!skill.BNormalSkill && !skill.IsTriggerBeforeDie && skill.CurCD <= 0f &&
                (skill.TargetsSelf || !skill.RequiresEnemyTarget ||
                 (skill.CanUseWithTarget(UnitLogic.SearchTarget) &&
                  UnitLogic.IsTargetInAttackRange(UnitLogic.SearchTarget, skill.SkillCfg.atkRange))))
            {
                return true;
            }
        }
        return false;
    }

    private bool IsHeroNormalAttack()
    {
        return UnitLogic.SoliderCfg != null
            && UnitFactory.IsHero(UnitLogic.SoliderCfg.id)
            && normalSkill != null
            && normalSkill.SkillCfg != null
            && normalSkill.SkillCfg.skillType != SkillFactory.RemoteSkillType;
    }

}
