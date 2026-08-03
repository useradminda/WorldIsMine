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
            int atkType = atkUnit.SoliderCfg.unitType;
            int beAtkType = beAtkedUnits[i].SoliderCfg.unitType;
            int finalDamage = CalcFinalDamage(atkType, beAtkType, damage);
           // beAtkedUnits[i].Prop.ChangeHp(finalDamage);
        }
    }

    public static int CalcFinalDamage(int atkType, int beAtkType, int baseDamage)
    {
        if (IsRestrain(atkType, beAtkType))
        {
            return Mathf.RoundToInt(baseDamage * 1.5f);
        }
        return baseDamage;
    }

    public static bool IsRestrain(int atkType, int beAtkType)
    {
        // 攻城器/英雄不参与常规兵种克制
        if (atkType == 0 || beAtkType == 0) return false;
        if (atkType == 11 || beAtkType == 11) return false;
        if (atkType == 101 || beAtkType == 101) return false;

        // 1:刀, 2:枪, 3:骑
        if (atkType == 1 && beAtkType == 2) return true; // 刀克枪
        if (atkType == 2 && beAtkType == 3) return true; // 枪克骑
        if (atkType == 3 && beAtkType == 1) return true; // 骑克刀

        return false;
    }

}
