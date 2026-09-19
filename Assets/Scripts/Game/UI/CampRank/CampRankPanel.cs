using System.Collections.Generic;
using UnityEngine;

/// <summary>显示红蓝双方玩家排行榜。</summary>
public class CampRankPanel : BasePanel
{
    [SerializeField] private CampRankItem itemPrefab;
    [SerializeField] private Transform redRoot;
    [SerializeField] private Transform blueRoot;
    private readonly Queue<CampRankItem> itemCache = new Queue<CampRankItem>();
    private readonly List<CampRankItem> activeItems = new List<CampRankItem>();

    /// <summary>清空并刷新一个阵营的排行榜。</summary>
    public void RefreshCamp(bool redCamp, IList<string> names, IList<int> victoryPoints, IList<Sprite> icons)
    {
        ClearItems();
        Transform root = redCamp ? redRoot : blueRoot;
        if (root == null || names == null || victoryPoints == null)
        {
            return;
        }

        for (int i = 0; i < names.Count && i < victoryPoints.Count; i++)
        {
            CampRankItem item = GetItem(root);
            Sprite icon = icons != null && i < icons.Count ? icons[i] : null;
            item.SetData(i + 1, names[i], victoryPoints[i], icon);
            activeItems.Add(item);
        }

        Open();
    }

    /// <summary>关闭并回收所有排行条目。</summary>
    public override void Close()
    {
        ClearItems();
        base.Close();
    }

    private CampRankItem GetItem(Transform root)
    {
        CampRankItem item = itemCache.Count > 0 ? itemCache.Dequeue() : Instantiate(itemPrefab, root);
        item.transform.SetParent(root, false);
        item.gameObject.SetActive(true);
        return item;
    }

    private void ClearItems()
    {
        for (int i = 0; i < activeItems.Count; i++)
        {
            activeItems[i].gameObject.SetActive(false);
            itemCache.Enqueue(activeItems[i]);
        }

        activeItems.Clear();
    }
}
