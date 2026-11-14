using System.Collections.Generic;
using UnityEngine;

/// <summary>反击螺旋：累计配置次数的受击后，按累计伤害比例反击周围敌人。</summary>
public class CounterSpiralBuffLogic : BuffLogicBase
{
    private const int DefaultAttackCountToTrigger = 5;
    private int receivedAttackCount;
    private int accumulatedDamage;
    private int attackCountToTrigger = DefaultAttackCountToTrigger;

    /// <summary>创建反击螺旋 Buff 逻辑，并绑定持有者与 Buff 管理器。</summary>
    public CounterSpiralBuffLogic(
        UnitLogicBase unitLogic,
        BuffLogicMachine buffLogicMachine,
        int cfgId)
        : base(unitLogic, buffLogicMachine, cfgId)
    {
    }

    /// <summary>Buff 生效时清空累计受击次数与伤害。</summary>
    public override void Enter()
    {
        receivedAttackCount = 0;
        accumulatedDamage = 0;
        attackCountToTrigger = GetAttackCountToTrigger();
    }

    /// <summary>记录受击伤害，并在达到配置次数后对持有者周围敌人造成反击伤害。</summary>
    public override int RecordDamage(int damage, bool isAttack)
    {
        if (!isAttack || mUnitLogic == null || mUnitLogic.IsDead)
        {
            return 0;
        }

        receivedAttackCount++;
        long currentDamage = damage < 0 ? -(long)damage : 0;
        long totalDamage = (long)accumulatedDamage + currentDamage;
        accumulatedDamage = totalDamage >= int.MaxValue ? int.MaxValue : (int)totalDamage;

        if (receivedAttackCount < attackCountToTrigger)
        {
            return 0;
        }

        int counterDamage = Mathf.RoundToInt(accumulatedDamage * GetCounterDamageRate());
        if (accumulatedDamage > 0)
        {
            counterDamage = Mathf.Max(1, counterDamage);
        }
        receivedAttackCount = 0;
        accumulatedDamage = 0;
        PlayTriggerEffect();
        DealCounterDamage(Mathf.Max(0, counterDamage));
        return 0;
    }

    /// <summary>Buff 退出时清空本轮累计数据。</summary>
    public override void Exit()
    {
        receivedAttackCount = 0;
        accumulatedDamage = 0;
    }

    /// <summary>读取 Buff 配置中的累计伤害反击比例。</summary>
    private float GetCounterDamageRate()
    {
        return BuffCfg == null ? 0f : Mathf.Max(0f, BuffCfg.value);
    }

    /// <summary>从 BuffParam 读取触发所需受击次数，配置无效时使用默认次数。</summary>
    private int GetAttackCountToTrigger()
    {
        if (BuffCfg != null && int.TryParse(BuffCfg.buffParam, out int configuredCount)
            && configuredCount > 0)
        {
            return configuredCount;
        }

        return DefaultAttackCountToTrigger;
    }

    /// <summary>读取绑定当前 Buff 的技能范围，作为反击螺旋的伤害半径。</summary>
    private float GetCounterDamageRadius()
    {
        for (int skillIndex = 0; skillIndex < mUnitLogic.SkillList.Count; skillIndex++)
        {
            SkillCfg skillCfg = mUnitLogic.SkillList[skillIndex].SkillCfg;
            if (skillCfg.buffId == null)
            {
                continue;
            }

            for (int buffIndex = 0; buffIndex < skillCfg.buffId.Length; buffIndex++)
            {
                if (skillCfg.buffId[buffIndex] == CfgId)
                {
                    return Mathf.Max(0f, skillCfg.skillArea);
                }
            }
        }

        return 0f;
    }

    /// <summary>对反击半径内所有存活敌方单位结算本次反击伤害。</summary>
    private void DealCounterDamage(int counterDamage)
    {
        float counterRadius = GetCounterDamageRadius();
        if (counterDamage <= 0 || counterRadius <= 0f)
        {
            return;
        }

        ECampType enemyCamp = mUnitLogic.CampType == ECampType.Red ? ECampType.Blue : ECampType.Red;
        List<UnitLogicBase> enemyUnits = UnitManager.Instance.GetCampUnitList(enemyCamp);
        float counterRadiusSqr = counterRadius * counterRadius;
        for (int i = 0; i < enemyUnits.Count; i++)
        {
            UnitLogicBase target = enemyUnits[i];
            if (target == null || target.IsDead)
            {
                continue;
            }

            Vector3 offset = target.CurPos - mUnitLogic.CurPos;
            offset.y = 0f;
            if (offset.sqrMagnitude > counterRadiusSqr)
            {
                continue;
            }

            BattleLogicDamageTools.DoDamage(
                mUnitLogic,
                target,
                -counterDamage,
                target.UId,
                string.Empty,
                target.CurPos);
        }
    }
}
