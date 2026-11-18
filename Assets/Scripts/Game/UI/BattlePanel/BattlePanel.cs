using UnityEngine;

/// <summary>战斗面板的 UI 聚合入口。</summary>
public class BattlePanel : BasePanel
{
    [SerializeField] private BattleScorePanel scorePanel;
    [SerializeField] private BattleTimerPanel timerPanel;
    [SerializeField] private BattleCityHpPanel cityHpPanel;
    [SerializeField] private BattleSoldierQueuePanel soldierQueuePanel;
    [SerializeField] private BattleHeroCallInfoPanel heroInfoPanel;
    [SerializeField] private BattleHeroOperateSkillPanel heroOperateSkillPanel;
    [SerializeField] private BattleSoldierCallPanel soldierCallPanel;

    /// <summary>当前战斗 UI 面板实例。</summary>
    public static BattlePanel Instance { get; private set; }

    /// <summary>注册战斗 UI 面板并订阅全局 UI 数据变化。</summary>
    private void Awake()
    {
        Instance = this;
        BattleScoreManager.Instance.Changed += OnScoreChanged;
        SetScore(BattleScoreManager.Instance.TotalScore);
        RefreshCityHp();
        RefreshWaitingSoldierCount();
    }

    /// <summary>从红蓝城门逻辑读取血量并刷新城门血量子面板。</summary>
    public void RefreshCityHp()
    {
        UnitLogicBase redWall = UnitManager.Instance.GetWallLogic(ECampType.Red);
        UnitLogicBase blueWall = UnitManager.Instance.GetWallLogic(ECampType.Blue);
        if (redWall != null && blueWall != null)
        {
            SetCityHp(redWall.Prop.Hp, redWall.Prop.MaxHp, blueWall.Prop.Hp, blueWall.Prop.MaxHp);
        }
    }

    /// <summary>解除全局 UI 数据事件并清理面板实例。</summary>
    private void OnDestroy()
    {
        BattleScoreManager.Instance.Changed -= OnScoreChanged;
        if (Instance == this) Instance = null;
    }

    /// <summary>响应积分池变化并转发给积分子面板。</summary>
    private void OnScoreChanged(int totalScore, int victoryPoint)
    {
        SetScore(totalScore);
    }
    /// <summary>设置全局积分池总数。</summary>
    public void SetScore(int totalScore)
    {
        if (scorePanel != null)
        {
            scorePanel.SetScore(totalScore);
        }
    }

    /// <summary>开始战斗倒计时。</summary>
    public void StartTimer(float seconds)
    {
        if (timerPanel != null) timerPanel.StartTimer(seconds);
    }

    /// <summary>暂停战斗倒计时。</summary>
    public void PauseTimer()
    {
        if (timerPanel != null) timerPanel.PauseTimer();
    }

    /// <summary>恢复战斗倒计时。</summary>
    public void ResumeTimer()
    {
        if (timerPanel != null) timerPanel.ResumeTimer();
    }

    /// <summary>设置红蓝双方城池血量。</summary>
    public void SetCityHp(float redCurrent, float redMax, float blueCurrent, float blueMax)
    {
        if (cityHpPanel != null) cityHpPanel.SetCityHp(redCurrent, redMax, blueCurrent, blueMax);
    }

    /// <summary>设置红蓝双方等待队列士兵数量。</summary>
    public void SetWaitingSoldierCount(int redCount, int blueCount)
    {
        if (soldierQueuePanel != null) soldierQueuePanel.SetWaitingSoldierCount(redCount, blueCount);
    }

    /// <summary>从单位管理器读取红蓝等待士兵数量并刷新子面板。</summary>
    public void RefreshWaitingSoldierCount()
    {
        SetWaitingSoldierCount(
            UnitManager.Instance.GetWaitingSoldierCount(ECampType.Red),
            UnitManager.Instance.GetWaitingSoldierCount(ECampType.Blue));
    }

    /// <summary>将一次英雄召唤提示加入对应阵营的队列。</summary>
    public void EnqueueHeroCall(ECampType campType, int heroCfgId, BattlePlayer player)
    {
        if (heroInfoPanel != null)
        {
            heroInfoPanel.EnqueueHero(campType, heroCfgId, player);
        }
    }

    /// <summary>将一次英雄技能释放提示加入对应阵营的队列。</summary>
    public void EnqueueHeroSkill(ECampType campType, int heroCfgId, BattlePlayer player, string skillName, Sprite heroIcon = null)
    {
        if (heroOperateSkillPanel != null)
        {
            heroOperateSkillPanel.EnqueueSkill(campType, heroCfgId, player, skillName, heroIcon);
        }
    }

    /// <summary>将一次玩家使用物品召唤士兵的提示加入对应阵营队列。</summary>
    public void EnqueueSoldierCall(ECampType campType, int soldierCfgId, int count, BattlePlayer player, string iconPath)
    {
        if (soldierCallPanel != null)
        {
            soldierCallPanel.EnqueueSoldier(campType, soldierCfgId, count, player, iconPath);
        }
    }
}
