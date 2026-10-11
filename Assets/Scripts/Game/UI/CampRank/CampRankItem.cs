using UnityEngine;
using UnityEngine.UI;

/// <summary>阵营排行榜中的玩家条目。</summary>
public class CampRankItem : MonoBehaviour
{
    [SerializeField] private Text rankText;
    [SerializeField] private Text playerNameText;
    [SerializeField] private Text victoryPointText;
    [SerializeField] private Image playerIcon;

    /// <summary>设置玩家排行数据。</summary>
    public void SetData(int rank, string playerName, int victoryPoint, Sprite icon)
    {
        rankText.text = rank.ToString();
        playerNameText.text = playerName;
        victoryPointText.text = victoryPoint.ToString();
        playerIcon.sprite = icon;
    }
}
