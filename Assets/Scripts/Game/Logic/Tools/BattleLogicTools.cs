using System.Collections.Generic;

public static class BattleLogicTools
{
     // 查询结果
    private static IEnumerable<KDInfo> queryResult = new List<KDInfo>();
    private static List<UnitLogicBase> unitLogicList = new List<UnitLogicBase>();
    
    public static List<UnitLogicBase> SearchNotMyCampUnits(float x, float z, float range, ECampType campType, bool single = false)
    {
        unitLogicList.Clear();
        AillieoUtils.Vector2 searchPos = new AillieoUtils.Vector2(x, z);
        queryResult = KDTreeManager.Instance.mKDTree.QueryInRange(searchPos, range, single);
        foreach (KDInfo value in queryResult)
        {
            if (value.UB.CampType != campType && value.UB.IsDead == false)
            {
                unitLogicList.Add(value.UB);
            }
        }
        return unitLogicList;
    }

    //
    public static List<UnitLogicBase> SearchMyCampUnits(float x, float z, float range, ECampType campType, bool single = false)
    {
        unitLogicList.Clear();
        AillieoUtils.Vector2 searchPos = new AillieoUtils.Vector2(x, z);
        queryResult = KDTreeManager.Instance.mKDTree.QueryInRange(searchPos, range, single);
        foreach (KDInfo value in queryResult)
        {
            if (value.UB.CampType == campType && value.UB.IsDead == false)
            {
                unitLogicList.Add(value.UB);
            }
        }
        return unitLogicList;
    }

    public static List<UnitLogicBase> GetAllMyCampUnits()
    {
        unitLogicList.Clear();
        return unitLogicList;
    }

    public static List<UnitLogicBase> GetAllUnitEnemyCampUnits() 
    {
        unitLogicList.Clear();
        return unitLogicList;
    }

}
