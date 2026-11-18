using System.Collections.Generic;
using UnityEngine;

/// <summary>英雄 HUD 管理器，负责英雄 HUD 的创建、跟随、缓存和回收。</summary>
public class BattleHeroHudPanel : MonoBehaviour
{
    [SerializeField] private BattleHeroHudItem hudPrefab;
    [SerializeField] private Transform hudRoot;
    [SerializeField] private float positionRefreshInterval;

    private readonly Queue<BattleHeroHudItem> cache = new Queue<BattleHeroHudItem>();
    private readonly Dictionary<UnitLogicBase, BattleHeroHudItem> activeHuds = new Dictionary<UnitLogicBase, BattleHeroHudItem>();

    /// <summary>创建或复用一个英雄 HUD。</summary>
    public BattleHeroHudItem CreateHud(UnitLogicBase heroLogic, int level, int experience, int maxExperience, int stars)
    {
        if (heroLogic == null || hudPrefab == null || activeHuds.ContainsKey(heroLogic)) return null;
        BattleHeroHudItem item = cache.Count > 0 ? cache.Dequeue() : Instantiate(hudPrefab, hudRoot);
        activeHuds.Add(heroLogic, item);
        item.Bind(heroLogic, level, experience, maxExperience, stars, positionRefreshInterval);
        return item;
    }

    /// <summary>回收指定英雄 HUD 并放回对象缓存。</summary>
    public void RemoveHud(UnitLogicBase heroLogic)
    {
        if (heroLogic == null || !activeHuds.TryGetValue(heroLogic, out BattleHeroHudItem item)) return;
        activeHuds.Remove(heroLogic);
        item.Unbind();
        cache.Enqueue(item);
    }

    /// <summary>刷新指定英雄 HUD 的当前血量。</summary>
    public void RefreshHp(UnitLogicBase heroLogic)
    {
        if (heroLogic != null && activeHuds.TryGetValue(heroLogic, out BattleHeroHudItem item)) item.RefreshHp();
    }

    /// <summary>设置指定英雄 HUD 的等级、经验和星级。</summary>
    public void SetExperience(UnitLogicBase heroLogic, int level, int experience, int maxExperience, int stars)
    {
        if (heroLogic != null && activeHuds.TryGetValue(heroLogic, out BattleHeroHudItem item))
        {
            item.SetExperience(level, experience, maxExperience, stars);
        }
    }

    /// <summary>刷新所有已激活英雄 HUD 的位置。</summary>
    public void RefreshPositions(float deltaTime)
    {
        foreach (BattleHeroHudItem item in activeHuds.Values) item.RefreshPosition(deltaTime);
    }

    /// <summary>
    /// 回收全部英雄 HUD，清理本场战斗的英雄引用。
    /// </summary>
    public void ClearAll()
    {
        foreach (BattleHeroHudItem item in activeHuds.Values)
        {
            item.Unbind();
            cache.Enqueue(item);
        }
        activeHuds.Clear();
    }
}
