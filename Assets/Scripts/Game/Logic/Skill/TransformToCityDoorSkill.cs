using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 城门传送技能类型。
/// 适用于把技能范围内的敌人传送到其所属阵营城门，常用于驱离或战场重置。
/// </summary>
public class TransformToCityDoorSkill : SkillLogicBase
{
    public TransformToCityDoorSkill(UnitLogicBase ulb, SkillCfg skillCfg)
        : base(ulb, skillCfg)
    {
    }

    /// <summary>
    /// 执行城门传送技能的实际效果，延迟由技能基类统一处理。
    /// </summary>
    public override void OnSkillDoEffect(UnitLogicBase target, List<int> resultUnitIndexList)
    {
        if (UnitLogic == null || UnitLogic.IsDead || BattleEngine.Instance == null)
        {
            return;
        }

        ECampType targetCamp = UnitLogic.CampType == ECampType.Red ? ECampType.Blue : ECampType.Red;
        Vector3 cityDoorPoint = BattleEngine.Instance.GetCityDoorPoint(targetCamp);
        Vector3 cityDoorForward = BattleEngine.Instance.GetCityDoorForward(targetCamp);
        if (cityDoorForward.sqrMagnitude <= 0.0001f)
        {
            cityDoorForward = Vector3.forward;
        }

        TeleportEnemy(target, targetCamp, cityDoorPoint, cityDoorForward);
        if (resultUnitIndexList == null)
        {
            return;
        }

        for (int i = 0; i < resultUnitIndexList.Count; i++)
        {
            int unitIndex = resultUnitIndexList[i];
            if (unitIndex < 0 || unitIndex >= UnitManager.Instance.UnitList.Count)
            {
                continue;
            }

            UnitLogicBase enemy = UnitManager.Instance.UnitList[unitIndex];
            if (enemy == target)
            {
                continue;
            }

            TeleportEnemy(enemy, targetCamp, cityDoorPoint, cityDoorForward);
        }
    }

    private void TeleportEnemy(UnitLogicBase enemy, ECampType targetCamp, Vector3 cityDoorPoint, Vector3 cityDoorForward)
    {
        if (enemy == null || enemy.IsDead || enemy.CampType != targetCamp || enemy.Agenter == null)
        {
            return;
        }

        StateBase currentState = enemy.StateMachine != null
            ? enemy.StateMachine.GetCurrentState()
            : null;
        if (currentState == null || currentState.StateType != EStateTyep.Skill)
        {
            enemy.SetSearchTarget(null);
        }
        enemy.TeleportTo(cityDoorPoint, cityDoorForward);
    }
}
