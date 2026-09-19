using UnityEngine;
using UnityEngine.UI;

/// <summary>单个阵营的等待出兵数量和进度条。</summary>
public class WaitingSpawnItem : MonoBehaviour
{
    [SerializeField] private Text countText;
    [SerializeField] private Slider countSlider;
    [SerializeField] private int sliderMaxValue = 3000;

    /// <summary>设置等待数量并同步 Slider。</summary>
    public void SetWaitingCount(int count)
    {
        int safeCount = Mathf.Max(0, count);
        if (countText != null)
        {
            countText.text = safeCount.ToString();
        }

        if (countSlider != null)
        {
            countSlider.maxValue = Mathf.Max(1, sliderMaxValue);
            countSlider.value = Mathf.Min(safeCount, countSlider.maxValue);
        }
    }
}
