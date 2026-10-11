using UnityEngine;
using UnityEngine.UI;

/// <summary>显示并控制对局倒计时。</summary>
public class BattleTimerPanel : BasePanel
{
    [SerializeField] private Text timerText;
    private float remainSeconds;
    private bool running;

    /// <summary>开始指定秒数的倒计时。</summary>
    public void StartTimer(float seconds)
    {
        remainSeconds = Mathf.Max(0f, seconds);
        running = true;
        RefreshText();
        Open();
    }

    /// <summary>暂停倒计时。</summary>
    public void PauseTimer()
    {
        running = false;
    }

    /// <summary>恢复倒计时。</summary>
    public void ResumeTimer()
    {
        running = true;
    }

    private void Update()
    {
        if (!running)
        {
            return;
        }

        remainSeconds = Mathf.Max(0f, remainSeconds - Time.deltaTime);
        RefreshText();
        if (remainSeconds <= 0f)
        {
            running = false;
        }
    }

    private void RefreshText()
    {
        if (timerText != null)
        {
            int totalSeconds = Mathf.CeilToInt(remainSeconds);
            timerText.text = string.Format("{0:00}:{1:00}", totalSeconds / 60, totalSeconds % 60);
        }
    }
}
