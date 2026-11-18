using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 骑士出生冲锋状态。
/// 骑士创建后沿出生方向高速前进，检测到路径上的敌人或敌方城门后结束冲锋，交回普通移动和攻击流程。
/// </summary>
public class KnightChargeState : StateBase
{
    // 冲锋移动速度，单位为世界坐标每秒，可直接在代码中调整。
    private const float ChargeSpeed = 15f;
    // 冲锋期间的动画播放倍率，只影响表现速度，不修改逻辑速度和 LogicRatio。
    private const float ChargeAnimationSpeed = 1.8f;
    // 没有命中目标时的最大冲锋持续时间，单位为秒。
    private const float ChargeDuration = 5f;
    // 冲锋特效资源路径，可按项目资源实际路径修改。
    private const string ChargeEffectPrefab = "Effects/V1_Fx_骑士冲锋";

    private readonly List<int> searchResultList = new List<int>();
    private readonly List<UnitLogicBase> chargeTargets = new List<UnitLogicBase>();
    private Vector3 chargeDirection;
    private int searchRequestIndex = -1;
    private int nearestIndex = -1;
    private int randomIndex = -1;
    private GameObject chargeEffect;
    private KnightChargeSkill chargeSkill;
    private bool finishing;
    private float impactWaitRemain = 0.3f;
    private float chargeRemainTime;

    public override EStateTyep StateType => EStateTyep.KnightCharge;

    public KnightChargeState(UnitLogicBase unitLogic) : base(unitLogic)
    {
    }

    /// <summary>
    /// 初始化骑士冲锋方向、Agent 参数和冲锋特效。
    /// </summary>
    public override void EnterState(params object[] objects)
    {
        finishing = false;
        chargeRemainTime = ChargeDuration;
        searchRequestIndex = -1;
        searchResultList.Clear();
        chargeTargets.Clear();
        SkillCfg chargeSkillCfg = UnitLogic.SoliderCfg != null && UnitLogic.SoliderCfg.exSKillId > 0
            ? SkillCfgConfig.Ins.SearchById(UnitLogic.SoliderCfg.exSKillId)
            : null;
        chargeSkill = chargeSkillCfg != null
            ? SkillFactory.CreateSkillLogic(UnitLogic, chargeSkillCfg) as KnightChargeSkill
            : null;
        if (chargeSkill == null)
        {
            UnitLogic.StateMachine.ChangeState(EStateTyep.Move);
            return;
        }
        // 先清理复用单位可能残留的旧目标，再读取出生时的前进方向。
        UnitLogic.SetSearchTarget(null);
        chargeDirection = UnitLogic.TargetForward;
        chargeDirection.y = 0f;
        if (chargeDirection.sqrMagnitude <= 0.0001f)
        {
            chargeDirection = Vector3.forward;
        }
        chargeDirection.Normalize();
        UnitLogic.SetMoveForward(chargeDirection);
        UnitLogic.TriggerMove();
        UnitLogic.Agenter.collisionEnabled = false;
        UnitLogic.UnitView.EnterState(EStateTyep.Move);
        UnitLogic.UnitView.SetForwardForce(chargeDirection);
        ApplyChargeAnimationSpeed();
        CreateChargeEffect();
    }

    /// <summary>
    /// 推进骑士冲锋移动，搜索请求在 LateUpdateState 中每帧提交。
    /// </summary>
    public override void UpdateState(float dt)
    {
        if (UnitLogic.IsDead)
        {
            // 死亡时不再切回移动状态，只清理冲锋特效，等待死亡状态接管单位。
            RecycleChargeEffect();
            return;
        }

        if (finishing)
        {
            impactWaitRemain -= dt * UnitLogic.LogicRatio;
            UnitLogic.TriggerMoveStop();
            UnitLogic.Agenter.collisionEnabled = true;
            if (UnitLogic.UnitView != null)
            {
                UnitLogic.UnitView.ActionFlowComponent.SetAnimationSpeed(UnitLogic.LogicRatio);
                UnitLogic.UnitView.ActionFlowComponent.PlayAction(EActionType.wait);
            }
            if (impactWaitRemain <= 0f)
            {
                UnitLogic.StateMachine.ChangeState(EStateTyep.Move);
            }
            return;
        }

        chargeRemainTime -= dt * UnitLogic.LogicRatio;
        if (chargeRemainTime <= 0f)
        {
            UnitLogic.StateMachine.ChangeState(EStateTyep.Move);
            return;
        }

        ApplyChargeAnimationSpeed();

        // 冲锋直接推进 Agent 位置，绕过前排单位的 RVO 实际速度限制。
        UnitLogic.Agenter.navigationEnabled = false;
        UnitLogic.Agenter.collisionEnabled = false;
        UnitLogic.Agenter.prefVelocity = Vector3.zero;
        UnitLogic.Agenter.velocity = Vector3.zero;
        Vector3 nextPosition = (Vector3)UnitLogic.Agenter.pos
            + chargeDirection * ChargeSpeed * Mathf.Max(0f, UnitLogic.LogicRatio) * dt;
        UnitLogic.Agenter.pos = nextPosition;
        if (UnitLogic.UnitView != null)
        {
            // 同步表现位置，避免直接移动 Agent 后由旧插值起点产生回拉。
            UnitLogic.UnitView.SnapToPosition(nextPosition);
        }
        UnitLogic.UnitView.SetForwardForce(chargeDirection);

    }

    /// <summary>
    /// 读取前方搜索结果，命中敌人或敌方城门后结束冲锋。
    /// </summary>
    public override void LateUpdateState(float dt)
    {
        if (UnitLogic.IsDead)
        {
            RecycleChargeEffect();
            return;
        }

        if (finishing)
        {
            return;
        }

        if (searchRequestIndex >= 0)
        {
            searchResultList.Clear();
            MapCellManager.Instance.GetResult(searchRequestIndex, searchResultList, ref nearestIndex, ref randomIndex);
            searchRequestIndex = -1;

            List<UnitLogicBase> enemyTargets = CollectChargeHitTargets();
            if (TryGetReachedEnemyWall(out UnitLogicBase enemyWall) && !enemyTargets.Contains(enemyWall))
            {
                // 城门不在 MapCellManager 的士兵搜索表中，需要在到达边界时单独加入命中列表。
                enemyTargets.Add(enemyWall);
            }
            if (enemyTargets.Count > 0)
            {
                chargeSkill.ApplyChargeDamage(enemyTargets);
                BeginImpactWait();
                return;
            }
        }

        // 当前帧消费上一帧结果后，立即提交下一帧的异步搜索请求。
        RequestChargeTargetSearch();

        if (TryGetReachedEnemyWall(out UnitLogicBase reachedWall))
        {
            chargeTargets.Clear();
            chargeTargets.Add(reachedWall);
            chargeSkill.ApplyChargeDamage(chargeTargets);
            BeginImpactWait();
        }
    }

    /// <summary>
    /// 退出冲锋状态，恢复 Agent 碰撞并回收冲锋特效。
    /// </summary>
    public override void ExitState()
    {
        RestoreAnimationSpeed();
        RestoreAgent();
        RecycleChargeEffect();
        chargeDirection = Vector3.zero;
        searchRequestIndex = -1;
        searchResultList.Clear();
        chargeTargets.Clear();
        chargeSkill = null;
        finishing = false;
        impactWaitRemain = 0f;
        chargeRemainTime = 0f;
    }

    /// <summary>退出冲锋状态时恢复普通动画速度，并保留当前单位的 buff 倍率。</summary>
    private void RestoreAnimationSpeed()
    {
        if (UnitLogic.UnitView != null)
        {
            UnitLogic.UnitView.ActionFlowComponent.SetAnimationSpeed(UnitLogic.LogicRatio);
        }
    }

    /// <summary>
    /// 搜索骑士前方的小范围区域，避免遍历全场单位。
    /// </summary>
    private void RequestChargeTargetSearch()
    {
        float searchRadius = Mathf.Max(0.1f, chargeSkill.GetAreaSearchRadius());
        Vector3 searchCenter = UnitLogic.CurPos + chargeDirection * (searchRadius * 0.5f);
        searchRequestIndex = MapCellManager.Instance.RequestSearch(
            searchCenter,
            searchRadius,
            UnitLogic.OtherCampTypeInt);
    }

    /// <summary>
    /// 从搜索结果中收集冲锋方向上的全部敌方命中目标，城门也会参与伤害结算。
    /// </summary>
    private List<UnitLogicBase> CollectChargeHitTargets()
    {
        chargeTargets.Clear();
        for (int i = 0; i < searchResultList.Count; i++)
        {
            int unitIndex = searchResultList[i];
            if (unitIndex < 0 || unitIndex >= UnitManager.Instance.UnitList.Count)
            {
                continue;
            }

            UnitLogicBase candidate = UnitManager.Instance.UnitList[unitIndex];
            if (candidate == null || candidate.IsDead || candidate.CampType == UnitLogic.CampType
                || UnitLogic.IsTargetBlockedByEnemyWall(candidate))
            {
                continue;
            }

            Vector3 offset = candidate.GetClosestPoint(UnitLogic.CurPos) - UnitLogic.CurPos;
            offset.y = 0f;
            float forwardDistance = Vector3.Dot(offset, chargeDirection);
            if (forwardDistance < -0.1f)
            {
                continue;
            }

            chargeTargets.Add(candidate);
        }

        return chargeTargets;
    }

    /// <summary>
    /// 通过几何距离检测敌方城门，因为城门不是普通士兵，不在 Burst 空间搜索表中。
    /// </summary>
    private bool TryGetReachedEnemyWall(out UnitLogicBase enemyWall)
    {
        enemyWall = UnitManager.Instance.GetOtherWallLogic(UnitLogic.CampType);
        if (enemyWall == null || enemyWall.IsDead || enemyWall.CampType == UnitLogic.CampType)
        {
            enemyWall = null;
            return false;
        }

        Vector3 toWall = enemyWall.GetClosestPoint(UnitLogic.CurPos) - UnitLogic.CurPos;
        toWall.y = 0f;
        if (toWall.sqrMagnitude > 0.0001f && Vector3.Dot(toWall, chargeDirection) < 0f)
        {
            enemyWall = null;
            return false;
        }

        return UnitLogic.IsTargetInAttackRange(enemyWall, 0f);
    }

    /// <summary>
    /// 命中目标后进入短暂等待，随后再切换到普通移动状态。
    /// </summary>
    private void BeginImpactWait()
    {
        if (finishing)
        {
            return;
        }

        finishing = true;
        UnitLogic.SetSearchTarget(null);
        UnitLogic.TriggerMoveStop();
        UnitLogic.Agenter.collisionEnabled = true;
        RecycleChargeEffect();
        if (UnitLogic.UnitView != null)
        {
            UnitLogic.UnitView.ActionFlowComponent.PlayAction(EActionType.wait);
            UnitLogic.UnitView.ActionFlowComponent.SetAnimationSpeed(UnitLogic.LogicRatio);
        }
    }

    /// <summary>
    /// 恢复冲锋前的 Agent 移动和碰撞设置。
    /// </summary>
    private void RestoreAgent()
    {
        if (UnitLogic.Agenter == null || UnitLogic.IsDead)
        {
            return;
        }

        UnitLogic.Agenter.navigationEnabled = true;
        UnitLogic.Agenter.collisionEnabled = true;
        UnitLogic.Agenter.maxSpeed = UnitLogic.Agenter.saveMaxSpeed * UnitLogic.LogicRatio;
        UnitLogic.Agenter.prefVelocity = Vector3.zero;
        UnitLogic.Agenter.velocity = Vector3.zero;
    }

    /// <summary>应用冲锋动画倍率，并叠加当前单位的加速、减速和冻结逻辑倍率。</summary>
    private void ApplyChargeAnimationSpeed()
    {
        if (UnitLogic.UnitView != null)
        {
            UnitLogic.UnitView.ActionFlowComponent.SetAnimationSpeed(ChargeAnimationSpeed * UnitLogic.LogicRatio);
        }
    }

    /// <summary>
    /// 创建跟随骑士的冲锋特效。
    /// </summary>
    private void CreateChargeEffect()
    {
        if (UnitLogic.UnitView == null || string.IsNullOrWhiteSpace(ChargeEffectPrefab))
        {
            return;
        }

        chargeEffect = UnitViewFactory.CreateGob(
            ChargeEffectPrefab,
            UnitLogic.UnitView.transform.position,
            chargeDirection);
        if (chargeEffect == null)
        {
            return;
        }

        chargeEffect.transform.SetParent(UnitLogic.UnitView.transform, false);
        chargeEffect.transform.localPosition = Vector3.zero;
        chargeEffect.transform.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// 回收骑士冲锋特效，避免状态结束后特效继续残留。
    /// </summary>
    private void RecycleChargeEffect()
    {
        if (chargeEffect == null)
        {
            return;
        }

        UnitViewFactory.RemoveGob(ChargeEffectPrefab, chargeEffect);
        chargeEffect = null;
    }
}
