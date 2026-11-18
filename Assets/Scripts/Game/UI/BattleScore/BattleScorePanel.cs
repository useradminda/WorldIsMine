using UnityEngine;
using TMPro;

/// <summary>显示全局积分池的 UI 面板，不区分红蓝双方积分。</summary>
public class BattleScorePanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI totalPointText;

    /// <summary>设置积分池总数，并按照全局积分规则格式化显示。</summary>
    public void SetScore(int totalScore)
    {
        if (totalPointText != null)
        {
            totalPointText.text = BattleScoreRule.FormatScore(totalScore);
        }
    }

}
