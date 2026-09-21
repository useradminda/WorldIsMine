using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 使用 OnGUI 绘制调试出兵菜单。
/// </summary>
public class UIOperateManager : MonoBehaviour
{
    private const int MinSpawnCount = 1;
    private const int MaxSpawnCount = 3000;

    private readonly List<SoldierMenuData> redSoldiers = new List<SoldierMenuData>();
    private readonly List<SoldierMenuData> blueSoldiers = new List<SoldierMenuData>();

    private Rect windowRect = new Rect(20f, 70f, 760f, 300f);
    private Vector2 redScrollPosition;
    private Vector2 blueScrollPosition;
    private string spawnCountText = "100";
    private string operationTip = string.Empty;
    private bool isMenuOpen;
    private bool configLoaded;

    private GUIStyle campTitleStyle;
    private GUIStyle itemLabelStyle;
    private GUIStyle tipStyle;

    /// <summary>
    /// 缓存配置数据，避免 OnGUI 每次执行时重新读取 JSON。
    /// </summary>
    private void Awake()
    {
        LoadSoldierConfigs();
    }

    /// <summary>
    /// 绘制菜单开关以及出兵窗口。
    /// </summary>
    private void OnGUI()
    {
        EnsureStyles();

        string toggleText = isMenuOpen ? "关闭创建士兵" : "打开创建士兵";
        if (GUI.Button(new Rect(20f, 20f, 140f, 40f), toggleText))
        {
            isMenuOpen = !isMenuOpen;
        }

        if (!isMenuOpen)
        {
            return;
        }

        float windowWidth = Mathf.Clamp(Screen.width - 40f, 420f, 900f);
        windowRect.width = windowWidth;
        windowRect.x = Mathf.Clamp(windowRect.x, 0f, Mathf.Max(0f, Screen.width - windowRect.width));
        windowRect.y = Mathf.Clamp(windowRect.y, 0f, Mathf.Max(0f, Screen.height - windowRect.height));
        windowRect = GUI.Window(GetInstanceID(), windowRect, DrawSpawnWindow, "创建士兵");
    }

    /// <summary>
    /// 绘制完整的士兵创建窗口。
    /// </summary>
    private void DrawSpawnWindow(int windowId)
    {
        GUILayout.BeginVertical();
        DrawSpawnCountInput();

        if (!configLoaded)
        {
            GUILayout.Label("SoliderCfg.json 读取失败，请查看控制台日志。", tipStyle);
        }
        else
        {
            DrawCampMenu("红色阵营", ECampType.Red, redSoldiers, ref redScrollPosition, Color.red);
            DrawCampMenu("蓝色阵营", ECampType.Blue, blueSoldiers, ref blueScrollPosition, new Color(0.25f, 0.65f, 1f));
        }

        if (!string.IsNullOrEmpty(operationTip))
        {
            GUILayout.Label(operationTip, tipStyle);
        }

        GUILayout.EndVertical();
        GUI.DragWindow(new Rect(0f, 0f, windowRect.width - 45f, 24f));
    }

    /// <summary>
    /// 绘制并校验单次创建数量输入框。
    /// </summary>
    private void DrawSpawnCountInput()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("生成数量：", GUILayout.Width(75f));
        spawnCountText = GUILayout.TextField(spawnCountText, 5, GUILayout.Width(90f));
        GUILayout.Label($"允许范围 {MinSpawnCount}-{MaxSpawnCount}");

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("关闭", GUILayout.Width(70f)))
        {
            isMenuOpen = false;
        }
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制一个阵营的横向士兵列表。
    /// </summary>
    private void DrawCampMenu(
        string title,
        ECampType campType,
        List<SoldierMenuData> soldiers,
        ref Vector2 scrollPosition,
        Color titleColor)
    {
        Color oldColor = campTitleStyle.normal.textColor;
        campTitleStyle.normal.textColor = titleColor;
        GUILayout.Label(title, campTitleStyle);
        campTitleStyle.normal.textColor = oldColor;

        scrollPosition = GUILayout.BeginScrollView(
            scrollPosition,
            false,
            false,
            GUILayout.Height(82f));
        GUILayout.BeginHorizontal();

        for (int i = 0; i < soldiers.Count; i++)
        {
            SoldierMenuData soldier = soldiers[i];
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(155f), GUILayout.Height(60f));
            GUILayout.Label(soldier.DisplayName, itemLabelStyle, GUILayout.Width(145f));
            if (GUILayout.Button("生成", GUILayout.Width(145f), GUILayout.Height(26f)))
            {
                SpawnSoldier(soldier.ConfigId, soldier.DisplayName, campType);
            }
            GUILayout.EndVertical();
        }

        GUILayout.EndHorizontal();
        GUILayout.EndScrollView();
    }

    /// <summary>
    /// 根据当前输入数量提交一组士兵创建请求。
    /// </summary>
    private void SpawnSoldier(int configId, string displayName, ECampType campType)
    {
        if (!TryGetSpawnCount(out int spawnCount))
        {
            operationTip = $"数量必须是 {MinSpawnCount}-{MaxSpawnCount} 之间的整数。";
            return;
        }

        BattleEngine.Instance.CreateUnit(configId, campType, spawnCount);
        operationTip = $"已提交：{displayName} × {spawnCount}";
    }

    /// <summary>
    /// 解析数量输入，并将超出范围的数值限制到有效范围。
    /// </summary>
    private bool TryGetSpawnCount(out int spawnCount)
    {
        if (!int.TryParse(spawnCountText, out spawnCount))
        {
            return false;
        }

        spawnCount = Mathf.Clamp(spawnCount, MinSpawnCount, MaxSpawnCount);
        spawnCountText = spawnCount.ToString();
        return true;
    }

    /// <summary>
    /// 从 SoliderCfg.json 对应的配置单例中建立红蓝双方菜单数据。
    /// </summary>
    private void LoadSoldierConfigs()
    {
        redSoldiers.Clear();
        blueSoldiers.Clear();

        try
        {
            List<SoliderCfg> configs = SoliderCfgConfig.Ins.ConfigDataList;
            for (int i = 0; i < configs.Count; i++)
            {
                SoliderCfg config = configs[i];
                if (!TryGetCampByConfigId(config.id, out ECampType campType))
                {
                    continue;
                }

                SoldierMenuData menuData = new SoldierMenuData(config.id, config.name);
                if (campType == ECampType.Red)
                {
                    redSoldiers.Add(menuData);
                }
                else
                {
                    blueSoldiers.Add(menuData);
                }
            }

            configLoaded = true;
        }
        catch (Exception exception)
        {
            configLoaded = false;
            Debug.LogError($"读取 SoliderCfg.json 失败：{exception}");
        }
    }

    /// <summary>
    /// 按当前配置表的 ID 规则识别阵营，同时排除城墙配置。
    /// </summary>
    private bool TryGetCampByConfigId(int configId, out ECampType campType)
    {
        campType = ECampType.Red;

        if (configId >= 100 && configId < 200 || configId >= 1000 && configId < 2000)
        {
            campType = ECampType.Red;
            return true;
        }

        if (configId >= 200 && configId < 300 || configId >= 2000 && configId < 3000)
        {
            campType = ECampType.Blue;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 初始化 OnGUI 样式并复用，避免反复创建 GUIStyle。
    /// </summary>
    private void EnsureStyles()
    {
        if (campTitleStyle != null)
        {
            return;
        }

        campTitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            fontStyle = FontStyle.Bold
        };
        itemLabelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            clipping = TextClipping.Clip
        };
        tipStyle = new GUIStyle(GUI.skin.label)
        {
            normal =
            {
                textColor = Color.yellow
            }
        };
    }

    /// <summary>
    /// 缓存菜单展示文本，避免 OnGUI 中重复拼接字符串。
    /// </summary>
    private sealed class SoldierMenuData
    {
        public readonly int ConfigId;
        public readonly string DisplayName;

        /// <summary>
        /// 创建一条士兵菜单数据。
        /// </summary>
        public SoldierMenuData(int configId, string soldierName)
        {
            ConfigId = configId;
            DisplayName = $"{configId}  {soldierName}";
        }
    }
}
