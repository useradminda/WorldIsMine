using TMPro;
using UnityEngine;

/// <summary>战斗结果排行中的单条玩家统计项。</summary>
public sealed class BattleResultRankItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI rechargeText;
    [SerializeField] private TextMeshProUGUI sharedScoreText;
    [SerializeField] private TextMeshProUGUI victoryPointText;
    [SerializeField] private TextMeshProUGUI killCountText;

    /// <summary>绑定排行名次和玩家本场战斗数据。</summary>
    public void Bind(int rank, BattlePlayer player)
    {
        if (rankText != null) rankText.text = rank.ToString();
        if (playerNameText != null) playerNameText.text = player.Name;
        if (rechargeText != null) rechargeText.text = player.RechargeCount.ToString();
        if (sharedScoreText != null) sharedScoreText.text = player.SharedScore.ToString();
        if (victoryPointText != null) victoryPointText.text = player.VictoryPoint.ToString();
        if (killCountText != null) killCountText.text = player.KillCount.ToString();
    }
}
