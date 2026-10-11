using UnityEngine;
using UnityEngine.UI;

/// <summary>显示红蓝双方积分和胜点。</summary>
public class BattleScorePanel : BasePanel
{
    [SerializeField] private Text redScoreText;
    [SerializeField] private Text blueScoreText;
    [SerializeField] private Text redVictoryPointText;
    [SerializeField] private Text blueVictoryPointText;

    /// <summary>刷新双方战绩。</summary>
    public void SetScore(int redScore, int blueScore, int redVictoryPoint, int blueVictoryPoint)
    {
        SetText(redScoreText, redScore);
        SetText(blueScoreText, blueScore);
        SetText(redVictoryPointText, redVictoryPoint);
        SetText(blueVictoryPointText, blueVictoryPoint);
    }

    private void SetText(Text target, int value)
    {
        if (target != null)
        {
            target.text = value.ToString();
        }
    }
}
