using UnityEngine;

/// <summary>显示红蓝双方等待创建的士兵数量。</summary>
public class WaitingSpawnPanel : BasePanel
{
    [SerializeField] private WaitingSpawnItem redItem;
    [SerializeField] private WaitingSpawnItem blueItem;
    [SerializeField] private bool refreshEveryFrame = true;

    /// <summary>每帧刷新等待数量显示。</summary>
    private void Update()
    {
        if (refreshEveryFrame)
        {
            Refresh();
        }
    }

    /// <summary>立即刷新红蓝双方等待数量。</summary>
    public void Refresh()
    {
        UpdateItem(redItem, ECampType.Red);
        UpdateItem(blueItem, ECampType.Blue);
    }

    private void UpdateItem(WaitingSpawnItem item, ECampType campType)
    {
        if (item != null)
        {
            item.SetWaitingCount(UnitManager.Instance.GetWaitingUnitCount(campType));
        }
    }
}
