using UnityEngine;
using UnityEngine.UI;

/// <summary>直播间入口提示面板。</summary>
public class LiveRoomEnterPanel : BasePanel
{
    [SerializeField] private Text tipText;

    /// <summary>设置入口提示文字。</summary>
    public void SetTip(string content)
    {
        if (tipText != null)
        {
            tipText.text = content;
        }
    }

    /// <summary>触发进入直播间事件。</summary>
    public void EnterLiveRoom()
    {
        Close();
    }
}
