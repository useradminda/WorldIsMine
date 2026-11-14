using UnityEngine;

/// <summary>
/// 英雄普攻动画表现组件。
/// AttackState 负责攻击逻辑，本组件负责命中卡刀、渐进加速恢复以及动画结束后切换 wait。
/// </summary>
public class AttackStopComponent : MonoBehaviour
{
    [SerializeField] private float stopDuration = 0.20f;
    // 恢复阶段的最高播放速度，从 1 倍速逐渐加速到这个值。
    // 恢复阶段的动画加速度。动画从 0 倍速开始，最终恢复到正常速度 1 倍。
    [SerializeField] private float recoveryAcceleration = 8f;
    // 停顿结束后的初始播放速度，之后再通过 recoveryAcceleration 加速到 1 倍。
    [SerializeField, Range(0f, 1f)] private float recoveryStartSpeed = 0.7f;
    // 停顿结束后保持 recoveryStartSpeed 的时间，结束后才开始加速。
    [SerializeField] private float recoverySlowDuration = 0.2f;

    private UnitLogicBase unitLogic;
    private ActionFlow actionFlow;
    private float attackAnimationLength;
    private float animationElapsedTime;
    private float frozenAnimationTime;
    private float recoveryAnimationTime;
    private float recoveryAnimationSpeed;
    private float recoverySlowRemainTime;
    private float remainStopTime;
    private bool attackAnimationPlaying;
    private bool stopping;
    private bool recovering;

    public bool IsAttackAnimationPlaying => attackAnimationPlaying;

    public void BeginAttackAnimation(UnitLogicBase attacker, float animationLength)
    {
        if (attacker == null || attacker.IsDead || animationLength <= 0f
            || attacker.UnitView == null || attacker.UnitView.ActionFlowComponent == null)
        {
            return;
        }

        unitLogic = attacker;
        actionFlow = attacker.UnitView.ActionFlowComponent;
        attackAnimationLength = animationLength;
        animationElapsedTime = 0f;
        frozenAnimationTime = 0f;
        recoveryAnimationTime = 0f;
        recoveryAnimationSpeed = 0f;
        recoverySlowRemainTime = 0f;
        remainStopTime = 0f;
        stopping = false;
        recovering = false;
        attackAnimationPlaying = true;
        // 卡刀动画从当前逻辑速度开始，保证动画、攻击状态和攻击冷却使用同一倍率。
        actionFlow.SetAnimationSpeed(attacker.LogicRatio);
    }

    /// <summary>普攻命中时调用，只处理动画卡刀，不参与伤害和逻辑状态。</summary>
    public void TryStart(UnitLogicBase attacker)
    {
        if (!attackAnimationPlaying || stopping || recovering
            || attacker == null || attacker != unitLogic || attacker.IsDead
            || attacker.LogicRatio < 0.999f || actionFlow == null)
        {
            return;
        }

        frozenAnimationTime = Mathf.Clamp(
            animationElapsedTime,
            0f,
            Mathf.Max(0.0001f, attackAnimationLength - 0.001f));
        recoveryAnimationTime = frozenAnimationTime;
        recoveryAnimationSpeed = 0f;
        recoverySlowRemainTime = 0f;
        remainStopTime = Mathf.Max(0f, stopDuration);
        stopping = true;
        actionFlow.FreezeAnimationAt(frozenAnimationTime);
    }

    /// <summary>按逻辑速度推进英雄普攻动画，并处理卡刀和恢复阶段。</summary>
    private void Update()
    {
        if (!attackAnimationPlaying)
        {
            return;
        }

        if (unitLogic == null || unitLogic.IsDead
            || unitLogic.StateMachine == null
            || unitLogic.StateMachine.GetCurrentState() == null
            || unitLogic.StateMachine.GetCurrentState().StateType != EStateTyep.Attack)
        {
            Cancel();
            return;
        }

        // 麻痹期间保留攻击和卡刀进度，解除后从原进度继续，避免取消流程后计时错位。
        if (unitLogic.LogicRatio <= 0f)
        {
            return;
        }

        if (stopping)
        {
            remainStopTime -= Time.deltaTime * unitLogic.LogicRatio;
            actionFlow.FreezeAnimationAt(frozenAnimationTime);
            if (remainStopTime > 0f)
            {
                return;
            }

            stopping = false;
            if (recoveryAnimationTime < attackAnimationLength)
            {
                recoveryAnimationSpeed = Mathf.Clamp01(recoveryStartSpeed);
                recoverySlowRemainTime = Mathf.Max(0f, recoverySlowDuration);
                recovering = true;
                actionFlow.FreezeAnimationAt(recoveryAnimationTime);
            }
            else
            {
                FinishAttackAnimation();
            }
            return;
        }

        if (recovering)
        {
            float logicDeltaTime = Time.deltaTime * unitLogic.LogicRatio;
            if (recoverySlowRemainTime > 0f)
            {
                recoverySlowRemainTime -= logicDeltaTime;
            }
            else
            {
                recoveryAnimationSpeed = Mathf.MoveTowards(
                    recoveryAnimationSpeed,
                    1f,
                    Mathf.Max(0f, recoveryAcceleration) * logicDeltaTime);
            }
            recoveryAnimationTime += logicDeltaTime * recoveryAnimationSpeed;

            if (recoveryAnimationTime >= attackAnimationLength)
            {
                recoveryAnimationTime = attackAnimationLength;
                actionFlow.FreezeAnimationAt(recoveryAnimationTime);
                recovering = false;
                FinishAttackAnimation();
            }
            else
            {
                // 每帧推进动画时间，避免反复修改 _AnimLen 导致动画跳帧。
                actionFlow.FreezeAnimationAt(recoveryAnimationTime);
            }
            return;
        }

        // 普通播放阶段累计逻辑动画时间，变速时仍与 AttackState 的攻击时长保持一致。
        animationElapsedTime += Time.deltaTime * unitLogic.LogicRatio;
        if (animationElapsedTime >= attackAnimationLength)
        {
            animationElapsedTime = attackAnimationLength;
            FinishAttackAnimation();
        }
    }

    /// <summary>结束卡刀流程，恢复当前速度并切换到等待动作。</summary>
    private void FinishAttackAnimation()
    {
        if (!attackAnimationPlaying)
        {
            return;
        }

        attackAnimationPlaying = false;
        stopping = false;
        recovering = false;
        remainStopTime = 0f;
        recoveryAnimationTime = 0f;
        recoveryAnimationSpeed = 0f;
        recoverySlowRemainTime = 0f;
        // 卡刀结束后恢复当前逻辑速度，避免速度 Buff 被重置。
        actionFlow.ResumeAnimation();
        actionFlow.SetAnimationSpeed(unitLogic != null ? unitLogic.LogicRatio : 1f);

        if (unitLogic != null && !unitLogic.IsDead
            && unitLogic.StateMachine != null
            && unitLogic.StateMachine.GetCurrentState() != null
            && unitLogic.StateMachine.GetCurrentState().StateType == EStateTyep.Attack
            && actionFlow.CurrentActionData != null
            && actionFlow.CurrentActionData.ActionType == EActionType.attack)
        {
            actionFlow.PlayAction(EActionType.wait);
        }

        actionFlow = null;
        unitLogic = null;
    }

    public void Cancel()
    {
        attackAnimationPlaying = false;
        stopping = false;
        recovering = false;
        remainStopTime = 0f;
        recoveryAnimationTime = 0f;
        recoveryAnimationSpeed = 0f;
        recoverySlowRemainTime = 0f;
        if (actionFlow != null && unitLogic != null && !unitLogic.IsDead)
        {
            actionFlow.ResumeAnimation();
            actionFlow.SetAnimationSpeed(unitLogic.LogicRatio);
        }
        actionFlow = null;
        unitLogic = null;
    }
}
