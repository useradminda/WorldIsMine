using UnityEngine;

/// <summary>所有 UI 面板的基类。</summary>
public class BasePanel : MonoBehaviour
{
    /// <summary>面板是否处于打开状态。</summary>
    public bool IsOpen { get; private set; }

    /// <summary>初始化面板。</summary>
    public virtual void Init()
    {
    }

    /// <summary>打开面板。</summary>
    public virtual void Open()
    {
        IsOpen = true;
        gameObject.SetActive(true);
    }

    /// <summary>关闭面板。</summary>
    public virtual void Close()
    {
        IsOpen = false;
        gameObject.SetActive(false);
    }
}
