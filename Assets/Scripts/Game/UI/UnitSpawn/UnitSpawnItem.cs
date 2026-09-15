using UnityEngine;
using UnityEngine.UI;

/// <summary>兵种召唤列表中的兵种条目。</summary>
public class UnitSpawnItem : MonoBehaviour
{
    [SerializeField] private Image unitIcon;
    [SerializeField] private Text unitNameText;
    [SerializeField] private Text countText;

    /// <summary>设置兵种名称、图标和数量。</summary>
    public void SetData(string unitName, Sprite icon, int count)
    {
        unitNameText.text = unitName;
        unitIcon.sprite = icon;
        countText.text = count.ToString();
    }
}
