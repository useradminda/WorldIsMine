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
    private float animationStartTime;
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
        animationStartTime = Time.time;
        frozenAnimationTime = 0f;
        recoveryAnimationTime = 0f;
        recoveryAnimationSpeed = 0f;
        recoverySlowRemainTime = 0f;
        remainStopTime = 0f;
        stopping = false;
        recovering = false;
        attackAnimationPlaying = true;
        actionFlow.SetAnimationSpeed(1f);
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
            Time.time - animationStartTime,
            0f,
            Mathf.Max(0.0001f, attackAnimationLength - 0.001f));
        recoveryAnimationTime = frozenAnimationTime;
        recoveryAnimationSpeed = 0f;
        recoverySlowRemainTime = 0f;
        remainStopTime = Mathf.Max(0f, stopDuration);
        stopping = true;
        actionFlow.FreezeAnimationAt(frozenAnimationTime);
    }

    private void Update()
    {
        if (!attackAnimationPlaying)
        {
            return;
        }

        if (unitLogic == null || unitLogic.IsDead || unitLogic.LogicRatio < 0.999f
            || unitLogic.StateMachine == null
            || unitLogic.StateMachine.GetCurrentState() == null
            || unitLogic.StateMachine.GetCurrentState().StateType != EStateTyep.Attack)
        {
            Cancel();
            return;
        }

        if (stopping)
        {
            remainStopTime -= Time.deltaTime;
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
            if (recoverySlowRemainTime > 0f)
            {
                recoverySlowRemainTime -= Time.deltaTime;
            }
            else
            {
                recoveryAnimationSpeed = Mathf.MoveTowards(
                    recoveryAnimationSpeed,
                    1f,
                    Mathf.Max(0f, recoveryAcceleration) * Time.deltaTime);
            }
            recoveryAnimationTime += Time.deltaTime * recoveryAnimationSpeed;

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

        if (Time.time - animationStartTime >= attackAnimationLength)
        {
            FinishAttackAnimation();
        }
    }

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
        actionFlow.SetAnimationSpeed(1f);

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
            actionFlow.SetAnimationSpeed(unitLogic.LogicRatio);
        }
        actionFlow = null;
        unitLogic = null;
    }
}
