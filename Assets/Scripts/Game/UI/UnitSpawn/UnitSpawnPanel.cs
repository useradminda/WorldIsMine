using UnityEngine;

/// <summary>显示当前可召唤兵种及其数量。</summary>
public class UnitSpawnPanel : BasePanel
{
    [SerializeField] private UnitSpawnItem[] unitItems;

    /// <summary>刷新兵种条目数量。</summary>
    public void SetUnitCount(int index, string unitName, Sprite icon, int count)
    {
        if (unitItems == null || index < 0 || index >= unitItems.Length || unitItems[index] == null)
        {
            return;
        }

        unitItems[index].SetData(unitName, icon, count);
    }
}
