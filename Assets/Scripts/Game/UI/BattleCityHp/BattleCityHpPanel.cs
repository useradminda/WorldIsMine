using UnityEngine;
using UnityEngine.UI;

public class BattleCityHpPanel : MonoBehaviour
{
    [SerializeField] private Slider redCityHpSlider;
    [SerializeField] private Slider blueCityHpSlider;
    [SerializeField] private Text redCityHpText;
    [SerializeField] private Text blueCityHpText;

    public void SetCityHp(float redCurrent, float redMax, float blueCurrent, float blueMax)
    {
        SetHp(redCityHpSlider, redCityHpText, redCurrent, redMax);
        SetHp(blueCityHpSlider, blueCityHpText, blueCurrent, blueMax);
    }

    private void SetHp(Slider slider, Text text, float current, float max)
    {
        float safeMax = Mathf.Max(0f, max);
        float safeCurrent = Mathf.Clamp(current, 0f, safeMax);
        if (slider != null)
        {
            slider.maxValue = safeMax;
            slider.value = safeCurrent;
        }
        if (text != null)
        {
            text.text = $"{Mathf.CeilToInt(safeCurrent)}/{Mathf.CeilToInt(safeMax)}";
        }
    }
}
