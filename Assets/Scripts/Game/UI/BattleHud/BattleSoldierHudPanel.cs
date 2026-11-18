using System.Collections.Generic;
using UnityEngine;

/// <summary>士兵组 HUD 管理器，负责组血量显示、平均位置跟随、缓存和回收。</summary>
public class BattleSoldierHudPanel : MonoBehaviour
{
    [SerializeField] private BattleSoldierHudItem hudPrefab;
    [SerializeField] private Transform hudRoot;
    [SerializeField] private float positionRefreshInterval;

    private readonly Queue<BattleSoldierHudItem> cache = new Queue<BattleSoldierHudItem>();
    private readonly Dictionary<long, BattleSoldierHudItem> activeHuds = new Dictionary<long, BattleSoldierHudItem>();
    private readonly Dictionary<long, BattlePlayer> groupPlayers = new Dictionary<long, BattlePlayer>();
    private long nextGroupId = 1;

    /// <summary>创建或复用一组士兵 HUD，并返回该组唯一 ID。</summary>
    public long CreateGroupHud(IList<UnitLogicBase> soldierLogics, BattlePlayer player, float refreshInterval)
    {
        if (soldierLogics == null || soldierLogics.Count == 0 || player == null || hudPrefab == null) return 0;
        long groupId = nextGroupId++;
        BattleSoldierHudItem item = cache.Count > 0 ? cache.Dequeue() : Instantiate(hudPrefab, hudRoot);
        activeHuds.Add(groupId, item);
        groupPlayers.Add(groupId, player);
        item.Bind(soldierLogics, player, refreshInterval > 0f ? refreshInterval : positionRefreshInterval);
        for (int i = 0; i < soldierLogics.Count; i++)
        {
            soldierLogics[i]?.SetSoldierHudGroupId(groupId);
        }
        return groupId;
    }

    /// <summary>将伤害同步到指定士兵组 HUD。</summary>
    public void ApplyDamage(long groupId, int damage)
    {
        if (!activeHuds.TryGetValue(groupId, out BattleSoldierHudItem item)) return;
        item.RefreshHp();
    }

    /// <summary>从指定士兵组 HUD 中移除死亡士兵。</summary>
    public void RemoveSoldier(long groupId, UnitLogicBase soldierLogic)
    {
        if (!activeHuds.TryGetValue(groupId, out BattleSoldierHudItem item)) return;
        item.RemoveSoldier(soldierLogic);
        if (!item.gameObject.activeSelf) RemoveGroup(groupId);
    }

    /// <summary>回收指定士兵组 HUD。</summary>
    public void RemoveGroup(long groupId)
    {
        if (!activeHuds.TryGetValue(groupId, out BattleSoldierHudItem item)) return;
        activeHuds.Remove(groupId);
        groupPlayers.Remove(groupId);
        item.Unbind();
        cache.Enqueue(item);
    }

    /// <summary>刷新所有已激活士兵组 HUD 的位置。</summary>
    public void RefreshPositions(float deltaTime)
    {
        foreach (BattleSoldierHudItem item in activeHuds.Values) item.RefreshPosition(deltaTime);
    }

    /// <summary>
    /// 回收全部士兵组 HUD，清理本场战斗的士兵组引用。
    /// </summary>
    public void ClearAll()
    {
        foreach (BattleSoldierHudItem item in activeHuds.Values)
        {
            item.Unbind();
            cache.Enqueue(item);
        }
        activeHuds.Clear();
        groupPlayers.Clear();
        nextGroupId = 1;
    }
}
