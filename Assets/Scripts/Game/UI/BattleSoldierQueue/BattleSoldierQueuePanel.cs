using UnityEngine;
using TMPro;

/// <summary>显示红蓝双方等待队列中士兵数量的 UI 面板。</summary>
public class BattleSoldierQueuePanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI redWaitingSoldierText;
    [SerializeField] private TextMeshProUGUI blueWaitingSoldierText;

    /// <summary>同时设置红蓝双方等待队列中的士兵数量。</summary>
    public void SetWaitingSoldierCount(int redCount, int blueCount)
    {
        SetRedWaitingSoldierCount(redCount);
        SetBlueWaitingSoldierCount(blueCount);
    }

    /// <summary>设置红方等待队列士兵数量。</summary>
    public void SetRedWaitingSoldierCount(int count)
    {
        SetText(redWaitingSoldierText, count);
    }

    /// <summary>设置蓝方等待队列士兵数量。</summary>
    public void SetBlueWaitingSoldierCount(int count)
    {
        SetText(blueWaitingSoldierText, count);
    }

    /// <summary>设置单个等待数量文本。</summary>
    private void SetText(TextMeshProUGUI target, int value)
    {
        if (target != null) target.text = Mathf.Max(0, value).ToString();
    }
}
