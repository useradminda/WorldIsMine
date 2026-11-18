using System.Collections.Generic;

/// <summary>
/// 所有技能逻辑的基类。
/// 负责保存技能配置、施法者引用，并提供目标判定、伤害和 Buff 结算等通用能力。
/// </summary>
public class SkillLogicBase
{
    public float SkillSearchRange => SkillCfg.searchRange;

    private readonly UnitLogicBase unitLogic;
    public UnitLogicBase UnitLogic => unitLogic;

    private readonly SkillCfg skillCfg;
    public SkillCfg SkillCfg => skillCfg;

    public bool BNormalSkill => skillCfg.normal == 1;

    /// <summary>技能是否需要战斗目标后才能释放。</summary>
    public bool RequiresTarget => skillCfg != null && skillCfg.needTarget != 0;
    public bool TargetsSelf => skillCfg != null && skillCfg.needTarget == 2;
    public bool RequiresEnemyTarget => skillCfg != null && (skillCfg.needTarget == 1 || skillCfg.needTarget == 3);
    public bool RequiresHeroTarget => skillCfg != null && skillCfg.needTarget == 3;
    public virtual bool IsTriggerBeforeDie => false;

    public virtual bool CanUseWithTarget(UnitLogicBase target)
    {
        return target != null && (BNormalSkill || target.UnitType != EUnitType.Wall);
    }

    /// <summary>
    /// 是否需要为范围技能执行一次地图初筛。
    /// </summary>
    public virtual bool RequiresAreaSearch => skillCfg != null && skillCfg.skillArea > 0f;

    /// <summary>
    /// 范围初筛中心点，默认使用施法者当前位置。
    /// </summary>
    public virtual UnityEngine.Vector3 GetAreaSearchPosition()
    {
        return unitLogic.CurPos;
    }

    /// <summary>
    /// 范围初筛半径，默认使用技能表中的范围。
    /// </summary>
    public virtual float GetAreaSearchRadius()
    {
        return skillCfg != null ? skillCfg.skillArea : 0f;
    }

    private float curCD;
    private bool attackCooldownStarted;
    public float CurCD => curCD;

    private readonly List<UnitLogicBase> targetList = new List<UnitLogicBase>();
    public List<UnitLogicBase> TargetList => targetList;

    private bool delayedEffectPending;
    private float delayedEffectTime;
    private UnitLogicBase delayedTarget;
    private int delayedTargetIndex = -1;
    private int delayedTargetUid = -1;
    private readonly List<int> delayedResultUnitIndexList = new List<int>();
    private readonly List<int> delayedResultUnitUidList = new List<int>();

    public string skillGUID;

    public SkillLogicBase(UnitLogicBase ulb, SkillCfg skillCfg)
    {
        skillGUID = System.Guid.NewGuid().ToString();
        unitLogic = ulb;
        this.skillCfg = skillCfg;
    }

    /// <summary>
    /// 更新技能冷却，并统一处理配置中的技能效果触发延迟和目标快照校验。
    /// </summary>
    public virtual void SkillUpdate(float dt)
    {
        if (unitLogic == null)
        {
            return;
        }

        if (delayedEffectPending)
        {
            if (unitLogic.IsDead)
            {
                ClearDelayedEffect();
                return;
            }

            delayedEffectTime -= dt * unitLogic.LogicRatio;
            if (delayedEffectTime <= 0f)
            {
                ExecuteDelayedEffect();
            }
        }

        if (unitLogic.IsDead)
        {
            return;
        }
        if (curCD > 0f)
        {
            curCD -= dt * unitLogic.LogicRatio;
        }
    }

    /// <summary>
    /// 进入技能效果流程；立即播放释放特效和重置冷却，实际效果按配置决定立即或延迟执行。
    /// </summary>
    public void SkillDoEffect(UnitLogicBase target, List<int> resultUnitIndexList)
    {
        PlayStartEffect();
        bool cooldownStartedBeforeEffect = attackCooldownStarted;
        attackCooldownStarted = false;
        if (!cooldownStartedBeforeEffect)
        {
            SkillResetCD();
        }

        float triggerSkillTime = GetTriggerSkillTime();
        if (triggerSkillTime <= 0f)
        {
            OnSkillDoEffect(target, resultUnitIndexList);
            return;
        }

        delayedEffectPending = true;
        delayedEffectTime = triggerSkillTime;
        CaptureDelayedTarget(target);
        CaptureDelayedResult(resultUnitIndexList);
    }

    /// <summary>
    /// 释放技能状态中保存的目标列表。
    /// </summary>
    public virtual void SkillRelease()
    {
        targetList.Clear();
    }

    /// <summary>
    /// 清理技能在本场战斗中的目标、延迟效果和冷却状态。
    /// </summary>
    public void ClearBattleRuntime()
    {
        SkillRelease();
        ClearDelayedEffect();
        curCD = 0f;
        attackCooldownStarted = false;
    }

    /// <summary>
    /// 执行延迟结束后的技能效果，并过滤已经被回收复用的单位槽位。
    /// </summary>
    private void ExecuteDelayedEffect()
    {
        UnitLogicBase target = ResolveDelayedTarget();
        List<int> resultUnitIndexList = ResolveDelayedResult();
        ClearDelayedEffect();
        OnSkillDoEffect(target, resultUnitIndexList);
    }

    /// <summary>
    /// 保存延迟技能的主目标对象、槽位和唯一身份编号。
    /// </summary>
    private void CaptureDelayedTarget(UnitLogicBase target)
    {
        delayedTarget = target;
        delayedTargetIndex = target != null ? target.Index : -1;
        delayedTargetUid = target != null ? target.UId : -1;
    }

    /// <summary>
    /// 保存延迟技能的范围目标槽位，并同步记录每个单位的唯一身份编号。
    /// </summary>
    private void CaptureDelayedResult(List<int> resultUnitIndexList)
    {
        delayedResultUnitIndexList.Clear();
        delayedResultUnitUidList.Clear();
        if (resultUnitIndexList == null)
        {
            return;
        }

        for (int i = 0; i < resultUnitIndexList.Count; i++)
        {
            int unitIndex = resultUnitIndexList[i];
            delayedResultUnitIndexList.Add(unitIndex);

            UnitLogicBase unit = unitIndex >= 0 && unitIndex < UnitManager.Instance.UnitList.Count
                ? UnitManager.Instance.UnitList[unitIndex]
                : null;
            delayedResultUnitUidList.Add(unit != null ? unit.UId : -1);
        }
    }

    /// <summary>
    /// 校验延迟技能主目标仍然是释放时的同一个单位。
    /// </summary>
    private UnitLogicBase ResolveDelayedTarget()
    {
        if (delayedTarget == null || delayedTarget.IsDead || delayedTarget.UId != delayedTargetUid)
        {
            return null;
        }

        if (delayedTargetIndex >= 0
            && delayedTargetIndex < UnitManager.Instance.UnitList.Count
            && UnitManager.Instance.UnitList[delayedTargetIndex] != delayedTarget)
        {
            return null;
        }

        return delayedTarget;
    }

    /// <summary>
    /// 过滤延迟期间已经死亡、回收或被新单位复用的范围目标槽位。
    /// </summary>
    private List<int> ResolveDelayedResult()
    {
        List<int> resultUnitIndexList = new List<int>(delayedResultUnitIndexList.Count);
        for (int i = 0; i < delayedResultUnitIndexList.Count; i++)
        {
            int unitIndex = delayedResultUnitIndexList[i];
            if (unitIndex < 0 || unitIndex >= UnitManager.Instance.UnitList.Count)
            {
                continue;
            }

            UnitLogicBase unit = UnitManager.Instance.UnitList[unitIndex];
            if (unit == null || unit.IsDead || unit.UId != delayedResultUnitUidList[i])
            {
                continue;
            }

            resultUnitIndexList.Add(unitIndex);
        }

        return resultUnitIndexList;
    }

    /// <summary>
    /// 清除尚未触发的技能效果和目标快照。
    /// </summary>
    private void ClearDelayedEffect()
    {
        delayedEffectPending = false;
        delayedEffectTime = 0f;
        delayedTarget = null;
        delayedTargetIndex = -1;
        delayedTargetUid = -1;
        delayedResultUnitIndexList.Clear();
        delayedResultUnitUidList.Clear();
    }

    /// <summary>
    /// 获取配置中的技能效果触发延迟时间。
    /// </summary>
    private float GetTriggerSkillTime()
    {
        if (SkillCfg == null)
        {
            return 0f;
        }

        return SkillCfg.trigerSkillTime;
    }

    public virtual bool TryTriggerBeforeDie()
    {
        return false;
    }

    public virtual void OnSkillDoEffect(UnitLogicBase target, List<int> resultUnitIndexList)
    {
        if (unitLogic == null || unitLogic.IsDead || skillCfg == null)
        {
            return;
        }

        if (skillCfg.needTarget == 2)
        {
            // needTarget=2 表示技能目标固定为施法者自身，直接使用施法者，避免外部传入错误目标。
            ApplyBuffs(unitLogic);
            return;
        }

        if (target == null || target.IsDead || (!BNormalSkill && target.UnitType == EUnitType.Wall))
        {
            return;
        }
        int attackDamage = GetAttackDamageSnapshot();
        BattleLogicDamageTools.DoDamage(unitLogic, target, GetFinalDamage(attackDamage, target), target.UId, skillCfg.dieType, target.GetClosestPoint(unitLogic.CurPos));
        ApplyBuffs(target);
        if (skillCfg.skillArea > 0)
        {
            if (resultUnitIndexList != null)
            {
                for (int i = 0; i < resultUnitIndexList.Count; i++)
                {
                    int unitIndex = resultUnitIndexList[i];
                    if (unitIndex < 0 || unitIndex >= UnitManager.Instance.UnitList.Count || unitIndex == target.Index)
                    {
                        continue;
                    }
                    UnitLogicBase tarUnitLogic = UnitManager.Instance.UnitList[unitIndex];
                    if (tarUnitLogic == null || tarUnitLogic.IsDead || tarUnitLogic.CampTypeInt == unitLogic.CampTypeInt)
                    {
                        continue;
                    }
                    BattleLogicDamageTools.DoDamage(unitLogic, tarUnitLogic, GetFinalDamage(attackDamage, tarUnitLogic), tarUnitLogic.UId, skillCfg.dieType, tarUnitLogic.GetClosestPoint(unitLogic.CurPos));
                    ApplyBuffs(tarUnitLogic);
                }
            }

            // 范围技能的主目标仍需是士兵或英雄，但范围内的敌方城门也参与一次范围伤害。
            TryDamageEnemyWallInArea(GetAreaSearchPosition(), skillCfg.skillArea, attackDamage);
        }
    }

    /// <summary>
    /// 判断敌方城门是否位于范围技能区域内，并执行一次城门伤害。
    /// </summary>
    protected void TryDamageEnemyWallInArea(UnityEngine.Vector3 center, float radius, int attackDamage)
    {
        UnitLogicBase enemyWall = UnitManager.Instance.GetOtherWallLogic(unitLogic.CampType);
        if (enemyWall == null || enemyWall.IsDead)
        {
            return;
        }

        UnityEngine.Vector3 wallPoint = enemyWall.GetClosestPoint(center);
        UnityEngine.Vector3 offset = wallPoint - center;
        offset.y = 0f;
        if (offset.sqrMagnitude > radius * radius)
        {
            return;
        }

        BattleLogicDamageTools.DoDamage(
            unitLogic,
            enemyWall,
            GetFinalDamage(attackDamage, enemyWall),
            enemyWall.UId,
            skillCfg.dieType,
            wallPoint);
    }

    public void ApplyBuffs(UnitLogicBase targetUnit)
    {
        if (targetUnit == null || targetUnit.IsDead || skillCfg.buffId == null)
        {
            return;
        }
        for (int i = 0; i < skillCfg.buffId.Length; i++)
        {
            int buffId = skillCfg.buffId[i];
            if (buffId > 0)
            {
                targetUnit.AddBuff(buffId);
            }
        }
    }

    public void SkillResetCD()
    {
        curCD = skillCfg.cd;
        attackCooldownStarted = false;
    }

    /// <summary>
    /// 在普通攻击动作开始时启动技能冷却，避免不同攻击动画长度导致双方攻击频率不一致。
    /// </summary>
    public void StartAttackCooldown()
    {
        curCD = skillCfg.cd;
        attackCooldownStarted = true;
    }

    public void SkillClearCD()
    {
        curCD = 0f;
    }

    public int GetAttackDamageSnapshot()
    {
        return unitLogic.GetAttackDamage(skillCfg.skillDamageRate);
    }

    protected int GetFinalDamage(int attackDamage, UnitLogicBase target)
    {
        return -BattleLogicDamageTools.CalcFinalDamage(unitLogic.SoliderCfg.unitType, target.SoliderCfg.unitType, attackDamage, unitLogic.SoliderCfg.restrainValue);
    }

    protected virtual void PlayStartEffect()
    {
        if (skillCfg.startEffect == null)
        {
            return;
        }

        // 技能开始特效全部挂在释放者身上，保证数组中的多个特效同步跟随并独立回收。
        for (int i = 0; i < skillCfg.startEffect.Length; i++)
        {
            string effectPath = skillCfg.startEffect[i];
            if (string.IsNullOrWhiteSpace(effectPath))
            {
                continue;
            }

            unitLogic.UnitView.PlayNoAttachEffect(effectPath, 3f);
        }
    }
}
