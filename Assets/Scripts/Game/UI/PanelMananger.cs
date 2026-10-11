using System.Collections.Generic;
using UnityEngine;
using ZTools;

/// <summary>UI 面板的创建、缓存、查找和开关管理器。</summary>
public class PanelMananger : MonoSingleton<PanelMananger>
{
    private readonly Dictionary<System.Type, BasePanel> panelCatch = new Dictionary<System.Type, BasePanel>();

    /// <summary>创建或获取指定类型的面板。</summary>
    public T CreatePanel<T>(T prefab, Transform parent = null) where T : BasePanel
    {
        T panel = GetPanel<T>();
        if (panel != null)
        {
            return panel as T;
        }

        if (prefab == null)
        {
            return null;
        }

        panel = Instantiate(prefab, parent);
        panel.Init();
        panelCatch[typeof(T)] = panel;
        return panel;
    }

    /// <summary>获取已经创建的指定类型面板。</summary>
    public T GetPanel<T>() where T : BasePanel
    {
        BasePanel panel;
        if (panelCatch.TryGetValue(typeof(T), out panel))
        {
            return panel as T;
        }

        return null;
    }

    /// <summary>删除并清理指定类型面板。</summary>
    public void RemovePanel<T>() where T : BasePanel
    {
        BasePanel panel = GetPanel<T>();
        if (panel != null)
        {
            panelCatch.Remove(typeof(T));
            Destroy(panel.gameObject);
        }
    }

    /// <summary>打开指定面板。</summary>
    public bool OpenPanel<T>() where T : BasePanel
    {
        T panel = GetPanel<T>();
        if (panel == null)
        {
            return false;
        }

        panel.Open();
        return true;
    }

    /// <summary>关闭指定面板。</summary>
    public bool ClosePanel<T>() where T : BasePanel
    {
        T panel = GetPanel<T>();
        if (panel == null)
        {
            return false;
        }

        panel.Close();
        return true;
    }
}
