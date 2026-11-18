using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单条士兵召唤提示 UI 组件，负责显示士兵名称、数量和头像。
/// 队列、展示时长和切换逻辑由 BattleSoldierCallPanel 统一管理。
/// </summary>
public class BattleSoldierCallInfoItem : MonoBehaviour
{
    [SerializeField] private RawImage soldierIcon;
    [SerializeField] private TextMeshProUGUI soldierNameText;

    [SerializeField] private TextMeshProUGUI playerNameText;

    /// <summary>设置士兵召唤提示内容。</summary>
    public void SetData(int soldierCfgId, SoliderCfg soldierCfg, int count, BattlePlayer player, string iconPath)
    {
        if (soldierNameText != null)
        {
            string soldierName = soldierCfg != null ? soldierCfg.name : soldierCfgId.ToString();
            soldierNameText.text = soldierName + "x" + Mathf.Max(0, count);
        }

        if (playerNameText != null)
        {
            playerNameText.text = player != null ? player.Name : string.Empty;
        }

        if (soldierIcon != null)
        {
            soldierIcon.texture = UITextureFactory.Load(iconPath);
        }
    }

    /// <summary>清理上一次士兵召唤提示数据。</summary>
    public void ClearData()
    {
        if (soldierIcon != null) soldierIcon.texture = null;
        if (soldierNameText != null) soldierNameText.text = string.Empty;
        if (playerNameText != null) playerNameText.text = string.Empty;
    }

    /// <summary>重新开关组件以重新播放挂载的 Animator 动效。</summary>
    public void RestartDisplay()
    {
        gameObject.SetActive(false);
        gameObject.SetActive(true);
    }
}
