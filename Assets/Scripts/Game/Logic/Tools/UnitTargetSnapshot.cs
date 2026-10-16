using System.Collections.Generic;

public static class UnitTargetSnapshot
{
    public static void Capture(List<int> unitIndexList, List<int> unitUidList)
    {
        unitUidList.Clear();
        for (int i = 0; i < unitIndexList.Count; i++)
        {
            int unitIndex = unitIndexList[i];
            UnitLogicBase unit = unitIndex >= 0 && unitIndex < UnitManager.Instance.UnitList.Count ? UnitManager.Instance.UnitList[unitIndex] : null;
            unitUidList.Add(unit != null ? unit.UId : -1);
        }
    }

    public static bool TryGet(List<int> unitIndexList, List<int> unitUidList, int resultIndex, out UnitLogicBase unit)
    {
        unit = null;
        if (resultIndex < 0 || resultIndex >= unitIndexList.Count || resultIndex >= unitUidList.Count)
        {
            return false;
        }
        int unitIndex = unitIndexList[resultIndex];
        if (unitIndex < 0 || unitIndex >= UnitManager.Instance.UnitList.Count)
        {
            return false;
        }
        unit = UnitManager.Instance.UnitList[unitIndex];
        return unit != null && !unit.IsDead && unit.UId == unitUidList[resultIndex];
    }
}
