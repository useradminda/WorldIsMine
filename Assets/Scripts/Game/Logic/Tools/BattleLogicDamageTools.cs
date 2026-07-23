using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BattleLogicDamageTools
{
    public static void DoDamage(UnitLogicBase atkUnit, List<UnitLogicBase> beAtkedUnits, SkillLogicBase useSkill)
    {
        int damage = -useSkill.SkillCfg.damage;
        for (int i = 0; i < beAtkedUnits.Count; i++)
        {
            beAtkedUnits[i].Prop.ChangeHp(damage);
        }
    }
}
