using System.Collections.Generic;
using UnityEngine;
using ZTools;

/// <summary>英雄头顶信息 UI 的创建、查找和缓存管理器。</summary>
public class UnitWorldNamePanel : MonoSingleton<UnitWorldNamePanel>
{
    [SerializeField] private UnitWorldNameItem itemPrefab;
    [SerializeField] private Transform itemRoot;
    private readonly Queue<UnitWorldNameItem> itemCache = new Queue<UnitWorldNameItem>();
    private readonly Dictionary<Transform, UnitWorldNameItem> activeItems = new Dictionary<Transform, UnitWorldNameItem>();

    /// <summary>创建或复用一个英雄头顶信息组件。</summary>
    public UnitWorldNameItem Show(Transform headTarget, string playerName, string heroName, int level, int stars)
    {
        if (headTarget == null || itemPrefab == null)
        {
            return null;
        }

        UnitWorldNameItem item;
        if (!activeItems.TryGetValue(headTarget, out item))
        {
            item = itemCache.Count > 0 ? itemCache.Dequeue() : Instantiate(itemPrefab, itemRoot);
            item.gameObject.SetActive(true);
            activeItems.Add(headTarget, item);
        }

        item.Bind(headTarget);
        item.SetData(playerName, heroName, level, stars);
        return item;
    }

    /// <summary>回收指定英雄的头顶信息组件。</summary>
    public void Recycle(Transform headTarget)
    {
        UnitWorldNameItem item;
        if (headTarget == null || !activeItems.TryGetValue(headTarget, out item))
        {
            return;
        }

        activeItems.Remove(headTarget);
        item.Unbind();
        item.gameObject.SetActive(false);
        itemCache.Enqueue(item);
    }

    /// <summary>回收指定的头顶信息组件。</summary>
    public void Recycle(UnitWorldNameItem item)
    {
        if (item != null && item.FollowTarget != null)
        {
            Recycle(item.FollowTarget);
        }
    }
}
