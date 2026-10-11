using UnityEngine;
using UnityEngine.UI;

/// <summary>显示礼物、召唤和胜点规则说明。</summary>
public class GameRulePanel : BasePanel
{
    [SerializeField] private Text ruleText;

    /// <summary>设置规则说明文字。</summary>
    public void SetRule(string content)
    {
        if (ruleText != null)
        {
            ruleText.text = content;
        }
    }
}
