using System.Collections.Generic;
using UnityEngine;

/// <summary>Teleports all enemies found in the skill area to their city door.</summary>
public class TransformToCityDoorSkill : SkillLogicBase
{
    public TransformToCityDoorSkill(UnitLogicBase ulb, SkillCfg skillCfg)
        : base(ulb, skillCfg)
    {
    }

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
