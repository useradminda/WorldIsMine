using UnityEngine;

public static class BattleLogicDamageTools
{
    //public static void DoDamage(UnitLogicBase atkUnit, List<UnitLogicBase> beAtkedUnits, SkillLogicBase useSkill)
    //{
    //    int damage = -useSkill.SkillCfg.damage;
    //    for (int i = 0; i < beAtkedUnits.Count; i++)
    //    {
    //        int atkType = atkUnit.SoliderCfg.unitType;
    //        int beAtkType = beAtkedUnits[i].SoliderCfg.unitType;
    //        int finalDamage = CalcFinalDamage(atkType, beAtkType, damage);
    //        beAtkedUnits[i].ChangeHp(finalDamage);
    //    }
    //}

    public static void DoDamage(UnitLogicBase atkUnit, UnitLogicBase beAtkedUnit, int finalDamage, int beAtkedUid)
    {
        // uid 为了unitlogicbase可能会被替换
        if (beAtkedUnit.UId == beAtkedUid)
        {
            beAtkedUnit.ChangeHp(finalDamage);
        }
    }

    public static int CalcFinalDamage(int atkType, int beAtkType, int baseDamage, float restrainValue)
    {
        if (IsRestrain(atkType, beAtkType))
        {
            return Mathf.RoundToInt(baseDamage * restrainValue);
        }
        return baseDamage;
    }

    public static bool IsRestrain(int atkType, int beAtkType)
    {
        // 攻城器/英雄不参与常规兵种克制
        if (atkType == 0 || beAtkType == 0) return false;
        if (atkType == 11 || beAtkType == 11) return false;
        if (atkType == 101 || beAtkType == 101) return false;

        if (atkType == 1 && beAtkType == 2) return true; // 刀克枪
        if (atkType == 2 && beAtkType == 3) return true; // 枪克骑
        if (atkType == 3 && beAtkType == 1) return true; // 骑克刀
        if (atkType == 101) return true;
        if (atkType == 11 && beAtkType == 1001) return true; // 攻城 克 墙

        return false;
    }

}
