using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>技能释放提示中的英雄和玩家信息条目。</summary>
public class UseSkillItem : MonoBehaviour
{
    [SerializeField] private Image heroIcon;
    [SerializeField] private Text heroNameText;
    [SerializeField] private Text playerNameText;
    [SerializeField] private Text skillNameText;
    [SerializeField] private float displayTime = 3f;
    private Coroutine displayCoroutine;
    private Action<UseSkillItem> displayFinished;

    /// <summary>设置英雄、玩家和技能名称。</summary>
    public void SetData(Sprite heroIconSprite, string heroName, string playerName, string skillName)
    {
        if (heroIcon != null)
        {
            heroIcon.sprite = heroIconSprite;
        }

        if (heroNameText != null)
        {
            heroNameText.text = heroName;
        }

        if (playerNameText != null)
        {
            playerNameText.text = playerName;
        }

        if (skillNameText != null)
        {
            skillNameText.text = skillName;
        }
    }

    /// <summary>清理条目数据，准备重新放入缓存。</summary>
    public void ClearData()
    {
        StopDisplayTimer();
        SetData(null, string.Empty, string.Empty, string.Empty);
    }

    /// <summary>开始展示计时，计时结束后通知面板回收当前条目。</summary>
    public void StartDisplayTimer(Action<UseSkillItem> onFinished = null)
    {
        StopDisplayTimer();
        displayFinished = onFinished;
        displayCoroutine = StartCoroutine(DisplayTimer());
    }

    /// <summary>停止当前展示计时。</summary>
    public void StopDisplayTimer()
    {
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
            displayCoroutine = null;
        }

        displayFinished = null;
    }

    /// <summary>等待展示时间结束。</summary>
    private IEnumerator DisplayTimer()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, displayTime));
        displayCoroutine = null;
        Action<UseSkillItem> callback = displayFinished;
        displayFinished = null;
        if (callback != null)
        {
            callback(this);
        }
    }
}
