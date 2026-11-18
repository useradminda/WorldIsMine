using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗场景头顶 HUD 的单例总入口。
/// 外部只通过本类创建英雄 HUD、创建士兵组 HUD、更新组血量和回收 HUD。
/// </summary>
public class BattleHUDPanel : MonoBehaviour
{
    [SerializeField] private BattleHeroHudPanel heroHudPanel;
    [SerializeField] private BattleSoldierHudPanel soldierHudPanel;

    /// <summary>当前战斗 HUD 总管理器实例。</summary>
    public static BattleHUDPanel Instance { get; private set; }

    /// <summary>HUD 总管理器初始化并注册单例。</summary>
    private void Awake()
    {
        Instance = this;
        if (heroHudPanel == null) heroHudPanel = GetComponentInChildren<BattleHeroHudPanel>(true);
        if (soldierHudPanel == null) soldierHudPanel = GetComponentInChildren<BattleSoldierHudPanel>(true);
    }

    /// <summary>HUD 总管理器销毁时清理单例。</summary>
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>按指定刷新频率更新英雄和士兵 HUD 的跟随位置。</summary>
    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;
        heroHudPanel?.RefreshPositions(deltaTime);
        soldierHudPanel?.RefreshPositions(deltaTime);
    }

    /// <summary>创建或复用一个英雄头顶 HUD。</summary>
    public BattleHeroHudItem CreateHeroHud(UnitLogicBase heroLogic, int level = 1, int experience = 0, int maxExperience = 1, int stars = 0)
    {
        return heroHudPanel == null ? null : heroHudPanel.CreateHud(heroLogic, level, experience, maxExperience, stars);
    }

    /// <summary>回收指定英雄的头顶 HUD。</summary>
    public void RemoveHeroHud(UnitLogicBase heroLogic)
    {
        heroHudPanel?.RemoveHud(heroLogic);
    }

    /// <summary>刷新指定英雄 HUD 的当前血量。</summary>
    public void RefreshHeroHp(UnitLogicBase heroLogic)
    {
        heroHudPanel?.RefreshHp(heroLogic);
    }

    /// <summary>设置指定英雄 HUD 的等级、经验和星级。</summary>
    public void SetHeroExperience(UnitLogicBase heroLogic, int level, int experience, int maxExperience, int stars)
    {
        heroHudPanel?.SetExperience(heroLogic, level, experience, maxExperience, stars);
    }

    /// <summary>创建或复用一组士兵的总血量 HUD。</summary>
    public BattleSoldierHudItem CreateSoldierGroupHud(IList<UnitLogicBase> soldierLogics, BattlePlayer player, float refreshInterval = 0f)
    {
        return soldierHudPanel == null ? null : soldierHudPanel.CreateGroupHud(soldierLogics, player, refreshInterval);
    }

    /// <summary>从士兵组 HUD 中移除死亡或回收的士兵。</summary>
    public void RemoveSoldierFromGroup(BattlePlayer player, UnitLogicBase soldierLogic)
    {
        soldierHudPanel?.RemoveSoldier(player, soldierLogic);
    }

    /// <summary>将伤害同步到士兵组 HUD，并刷新组血量显示。</summary>
    public void ApplySoldierGroupDamage(BattlePlayer player, int damage)
    {
        soldierHudPanel?.ApplyDamage(player, damage);
    }

    /// <summary>直接回收一组士兵 HUD。</summary>
    public void RemoveSoldierGroupHud(BattlePlayer player)
    {
        soldierHudPanel?.RemoveGroup(player);
    }

    /// <summary>
    /// 清理本场战斗所有英雄和士兵 HUD。
    /// </summary>
    public void ClearAll()
    {
        heroHudPanel?.ClearAll();
        soldierHudPanel?.ClearAll();
    }
}
