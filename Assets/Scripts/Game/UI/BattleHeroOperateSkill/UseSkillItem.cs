using TMPro;
using UnityEngine.UI;
using UnityEngine;

/// <summary>英雄技能释放提示的纯显示组件，不负责队列和展示计时。</summary>
public class UseSkillItem : MonoBehaviour
{
    [SerializeField] private RawImage heroIcon;
    [SerializeField] private TextMeshProUGUI heroNameText;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI skillNameText;

    /// <summary>设置英雄、玩家和技能名称。</summary>
    public void SetData(SoliderCfg heroCfg, string heroName, string playerName, string skillName)
    {
        if (heroIcon != null)
        {
            heroIcon.texture = heroCfg != null ? UITextureFactory.Load(heroCfg.headImg) : null;
        }
        if (heroNameText != null) heroNameText.text = heroName;
        if (playerNameText != null) playerNameText.text = playerName;
        if (skillNameText != null) skillNameText.text = skillName;
    }

    /// <summary>清理当前技能提示内容。</summary>
    public void ClearData()
    {
        SetData(null, string.Empty, string.Empty, string.Empty);
    }
}

