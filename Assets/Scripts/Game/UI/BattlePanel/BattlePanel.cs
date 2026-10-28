using UnityEngine;

public class BattlePanel : BasePanel
{
    [SerializeField] private BattleScorePanel scorePanel;
    [SerializeField] private BattleTimerPanel timerPanel;
    [SerializeField] private BattleCityHpPanel cityHpPanel;
    [SerializeField] private BattleSoldierQueuePanel soldierQueuePanel;

    public void SetScore(int redScore, int blueScore, int redVictoryPoint, int blueVictoryPoint)
    {
        if (scorePanel != null) scorePanel.SetScore(redScore, blueScore, redVictoryPoint, blueVictoryPoint);
    }

    public void StartTimer(float seconds)
    {
        if (timerPanel != null) timerPanel.StartTimer(seconds);
    }

    public void PauseTimer()
    {
        if (timerPanel != null) timerPanel.PauseTimer();
    }

    public void ResumeTimer()
    {
        if (timerPanel != null) timerPanel.ResumeTimer();
    }

    public void SetCityHp(float redCurrent, float redMax, float blueCurrent, float blueMax)
    {
        if (cityHpPanel != null) cityHpPanel.SetCityHp(redCurrent, redMax, blueCurrent, blueMax);
    }

    public void SetWaitingSoldierCount(int redCount, int blueCount)
    {
        if (soldierQueuePanel != null) soldierQueuePanel.SetWaitingSoldierCount(redCount, blueCount);
    }
}
