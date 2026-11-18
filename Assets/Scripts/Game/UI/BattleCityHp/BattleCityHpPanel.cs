using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>显示红蓝双方城池当前血量、最大血量和血量条的 UI 面板。</summary>
public class BattleCityHpPanel : MonoBehaviour
{
    [SerializeField] private Slider redCityHpSlider;
    [SerializeField] private Slider blueCityHpSlider;
    [SerializeField] private TextMeshProUGUI redCityHpText;
    [SerializeField] private TextMeshProUGUI blueCityHpText;

    private float redMaxHp;
    private float blueMaxHp;

    /// <summary>同时设置红蓝城池的当前血量和最大血量。</summary>
    public void SetCityHp(float redCurrent, float redMax, float blueCurrent, float blueMax)
    {
        SetRedCityMaxHp(redMax);
        SetBlueCityMaxHp(blueMax);
        SetRedCityHp(redCurrent);
        SetBlueCityHp(blueCurrent);
    }


    /// <summary>设置红方城池最大血量，并同步红方 Slider 上限。</summary>
    public void SetRedCityMaxHp(float maxHp)
    {
        redMaxHp = Mathf.Max(0f, maxHp);
        if (redCityHpSlider != null) redCityHpSlider.maxValue = redMaxHp;
    }

    /// <summary>设置蓝方城池最大血量，并同步蓝方 Slider 上限。</summary>
    public void SetBlueCityMaxHp(float maxHp)
    {
        blueMaxHp = Mathf.Max(0f, maxHp);
        if (blueCityHpSlider != null) blueCityHpSlider.maxValue = blueMaxHp;
    }

    /// <summary>设置红方城池当前血量。</summary>
    public void SetRedCityHp(float currentHp)
    {
        SetHp(redCityHpSlider, redCityHpText, currentHp, redMaxHp);
    }

    /// <summary>设置蓝方城池当前血量。</summary>
    public void SetBlueCityHp(float currentHp)
    {
        SetHp(blueCityHpSlider, blueCityHpText, currentHp, blueMaxHp);
    }

    /// <summary>设置单个城池的血量条和文本。</summary>
    private void SetHp(Slider slider, TextMeshProUGUI text, float current, float max)
    {
        float safeMax = Mathf.Max(0f, max);
        float safeCurrent = Mathf.Clamp(current, 0f, safeMax);
        if (slider != null)
        {
            slider.wholeNumbers = true;
            slider.maxValue = safeMax;
            slider.value = safeCurrent;
        }
        if (text != null)
        {
            text.text = $"{Mathf.CeilToInt(safeCurrent)}/{Mathf.CeilToInt(safeMax)}";
        }
    }
}
