using System.Collections.Generic;
using UnityEngine;

/// <summary>战斗结果面板管理器，负责显示胜方和玩家排行。</summary>
public sealed class BattleResultPanel : MonoBehaviour
{
    /// <summary>当前场景中的结果面板单例。</summary>
    public static BattleResultPanel Instance { get; private set; }

    [SerializeField] private GameObject redVictoryObject;
    [SerializeField] private GameObject blueVictoryObject;
    [SerializeField] private BattleResultRankItem rankItemPrefab;
    [SerializeField] private Transform rankRoot;

    private readonly List<BattleResultRankItem> cachedItems = new List<BattleResultRankItem>();
    private readonly List<BattleResultRankItem> activeItems = new List<BattleResultRankItem>();

    /// <summary>初始化单例并隐藏结果面板。</summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Hide();
    }

    /// <summary>展示胜方标识并按照充值数量刷新排行。</summary>
    public void ShowResult(ECampType winnerCamp)
    {
        gameObject.SetActive(true);
        if (redVictoryObject != null) redVictoryObject.SetActive(winnerCamp == ECampType.Red);
        if (blueVictoryObject != null) blueVictoryObject.SetActive(winnerCamp == ECampType.Blue);
        ClearActiveItems();

        List<BattlePlayer> players = BattlePlayerManager.Instance.GetAllPlayers();
        players.Sort(ComparePlayers);
        for (int i = 0; i < players.Count; i++)
        {
            BattleResultRankItem item = GetOrCreateItem();
            if (item == null) continue;
            item.Bind(i + 1, players[i]);
            item.gameObject.SetActive(true);
            activeItems.Add(item);
        }
    }

    /// <summary>隐藏面板并回收排行项。</summary>
    public void Hide()
    {
        ClearActiveItems();
        if (redVictoryObject != null) redVictoryObject.SetActive(false);
        if (blueVictoryObject != null) blueVictoryObject.SetActive(false);
        gameObject.SetActive(false);
    }

    /// <summary>按照充值、瓜分积分、击杀数降序排列。</summary>
    private static int ComparePlayers(BattlePlayer left, BattlePlayer right)
    {
        int result = right.RechargeCount.CompareTo(left.RechargeCount);
        if (result != 0) return result;
        result = right.SharedScore.CompareTo(left.SharedScore);
        return result != 0 ? result : right.KillCount.CompareTo(left.KillCount);
    }

    /// <summary>获取或创建排行项。</summary>
    private BattleResultRankItem GetOrCreateItem()
    {
        for (int i = 0; i < cachedItems.Count; i++)
        {
            if (!cachedItems[i].gameObject.activeSelf) return cachedItems[i];
        }
        if (rankItemPrefab == null || rankRoot == null) return null;
        BattleResultRankItem item = Instantiate(rankItemPrefab, rankRoot);
        cachedItems.Add(item);
        return item;
    }

    /// <summary>隐藏当前已显示的排行项。</summary>
    private void ClearActiveItems()
    {
        for (int i = 0; i < activeItems.Count; i++)
        {
            if (activeItems[i] != null) activeItems[i].gameObject.SetActive(false);
        }
        activeItems.Clear();
    }
}
