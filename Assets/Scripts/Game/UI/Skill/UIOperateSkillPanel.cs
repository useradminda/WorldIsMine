using UnityEngine;
using System.Collections.Generic;

/// <summary>显示英雄释放全局技能信息的面板。</summary>
public class UIOperateSkillPanel : BasePanel
{
    [SerializeField] private UseSkillItem useSkillItemPrefab;
    [SerializeField] private Transform itemRoot;

    private readonly Queue<UseSkillItem> itemCache = new Queue<UseSkillItem>();
    private readonly List<UseSkillItem> activeItems = new List<UseSkillItem>();

    /// <summary>显示英雄头像、英雄名称、玩家名称和技能名称。</summary>
    public void ShowSkill(string heroName, string playerName, Sprite heroIcon, string skillName)
    {
        if (useSkillItemPrefab == null || itemRoot == null)
        {
            return;
        }

        UseSkillItem item = GetItem();
        item.SetData(heroIcon, heroName, playerName, skillName);
        activeItems.Add(item);
        item.StartDisplayTimer(RecycleItem);
        Open();
    }

    /// <summary>关闭面板并回收当前所有技能条目。</summary>
    public override void Close()
    {
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            RecycleItem(activeItems[i]);
        }

        activeItems.Clear();
        base.Close();
    }

    /// <summary>从缓存获取一个技能条目。</summary>
    private UseSkillItem GetItem()
    {
        UseSkillItem item;
        if (itemCache.Count > 0)
        {
            item = itemCache.Dequeue();
            item.gameObject.SetActive(true);
            return item;
        }

        item = Instantiate(useSkillItemPrefab, itemRoot);
        return item;
    }

    /// <summary>回收一个技能条目。</summary>
    private void RecycleItem(UseSkillItem item)
    {
        if (item == null)
        {
            return;
        }

        activeItems.Remove(item);
        item.ClearData();
        item.gameObject.SetActive(false);
        itemCache.Enqueue(item);
    }
}
