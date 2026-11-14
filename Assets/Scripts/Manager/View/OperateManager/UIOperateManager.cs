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
    private const int GlobalStoneFlyObjectCfgId = 1001;
    private const int GlobalStoneSkillCfgId = 1001;
    private const int GlobalArrowBlueFlyObjectCfgId = 1002;
    private const int GlobalArrowBlueSkillCfgId = 1002;
    private const int GlobalArrowRedFlyObjectCfgId = 1003;
    private const int GlobalArrowRedSkillCfgId = 1003;
    private const int GlobalDragonBreathFlyObjectCfgId = 1004;
    private const int GlobalDragonBreathSkillCfgId = 1004;
    private const int GlobalAttackBuffId = 2;
    private const float ReferenceScreenWidth = 1280f;
    private const float ReferenceScreenHeight = 720f;
    private const float MinGuiScale = 1.25f;
    private const float MaxGuiScale = 3f;

    private readonly List<SoldierMenuData> redSoldiers = new List<SoldierMenuData>();
    private readonly List<SoldierMenuData> blueSoldiers = new List<SoldierMenuData>();
    private readonly List<SoldierMenuData> redHeroes = new List<SoldierMenuData>();
    private readonly List<SoldierMenuData> blueHeroes = new List<SoldierMenuData>();

    private Rect windowRect = new Rect(20f, 70f, 620f, 650f);
    private Vector2 redContentScrollPosition;
    private Vector2 blueContentScrollPosition;
    private string redSpawnCountText = "100";
    private string blueSpawnCountText = "100";
    private string redHeroSpawnCountText = "1";
    private string blueHeroSpawnCountText = "1";
    private string operationTip = string.Empty;
    private bool isMenuOpen;
    private bool configLoaded;
    private int activeTab;

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

        float guiScale = GetGuiScale();
        Matrix4x4 oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(guiScale, guiScale, 1f));
        float guiScreenWidth = Screen.width / guiScale;
        float guiScreenHeight = Screen.height / guiScale;

        string toggleText = isMenuOpen ? "关闭创建士兵" : "打开创建士兵";
        if (GUI.Button(new Rect(20f, 20f, 140f, 40f), toggleText))
        {
            isMenuOpen = !isMenuOpen;
        }

        if (!isMenuOpen)
        {
            GUI.matrix = oldMatrix;
            return;
        }

        windowRect.width = Mathf.Clamp(guiScreenWidth * 0.48f, 480f, 720f);
        windowRect.height = Mathf.Clamp(guiScreenHeight * 0.82f, 500f, 720f);
        windowRect.x = Mathf.Clamp(windowRect.x, 0f, Mathf.Max(0f, guiScreenWidth - windowRect.width));
        windowRect.y = Mathf.Clamp(windowRect.y, 0f, Mathf.Max(0f, guiScreenHeight - windowRect.height));
        windowRect = GUI.Window(GetInstanceID() + 1, windowRect, DrawTabbedWindow, "创建士兵");
        GUI.matrix = oldMatrix;
    }

    /// <summary>根据当前分辨率计算整个OnGUI工具界面的统一缩放倍率。</summary>
    private float GetGuiScale()
    {
        float widthScale = Screen.width / ReferenceScreenWidth;
        float heightScale = Screen.height / ReferenceScreenHeight;
        return Mathf.Clamp(Mathf.Min(widthScale, heightScale), MinGuiScale, MaxGuiScale);
    }

    /// <summary>绘制红方、蓝方和全局技能三个页签的统一窗口。</summary>
    private void DrawTabbedWindow(int windowId)
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Toggle(activeTab == 0, "红方", GUI.skin.button, GUILayout.Height(32f)))
        {
            activeTab = 0;
        }
        if (GUILayout.Toggle(activeTab == 1, "蓝方", GUI.skin.button, GUILayout.Height(32f)))
        {
            activeTab = 1;
        }
        if (GUILayout.Toggle(activeTab == 2, "全局技能", GUI.skin.button, GUILayout.Height(32f)))
        {
            activeTab = 2;
        }
        GUILayout.EndHorizontal();

        if (activeTab == 0)
        {
            DrawCampWindowContent(ECampType.Red, redSoldiers, redHeroes,
                ref redSpawnCountText, ref redHeroSpawnCountText, ref redContentScrollPosition);
        }
        else if (activeTab == 1)
        {
            DrawCampWindowContent(ECampType.Blue, blueSoldiers, blueHeroes,
                ref blueSpawnCountText, ref blueHeroSpawnCountText, ref blueContentScrollPosition);
        }
        else
        {
            DrawGlobalSkillButtons();
            if (!string.IsNullOrEmpty(operationTip))
            {
                GUILayout.Label(operationTip, tipStyle);
            }
        }

        GUI.DragWindow(new Rect(0f, 0f, windowRect.width - 45f, 24f));
    }

    /// <summary>绘制单个阵营窗口中的数量输入、士兵网格和英雄网格。</summary>
    private void DrawCampWindowContent(
        ECampType campType,
        List<SoldierMenuData> soldiers,
        List<SoldierMenuData> heroes,
        ref string soldierCountText,
        ref string heroCountText,
        ref Vector2 scrollPosition)
    {
        GUILayout.BeginVertical();
        scrollPosition = GUILayout.BeginScrollView(
            scrollPosition,
            false,
            true,
            GUILayout.Height(Mathf.Max(200f, windowRect.height - 90f)));
        if (!configLoaded)
        {
            GUILayout.Label("SoliderCfg.json 读取失败，请查看控制台日志。", tipStyle);
        }
        else
        {
            DrawSpawnCountInput("士兵出生数量", ref soldierCountText);
            GUILayout.Label("士兵", itemLabelStyle, GUILayout.Height(22f));
            DrawUnitMenuGrid(soldiers, campType, false);
            DrawSpawnCountInput("英雄出生数量", ref heroCountText);
            GUILayout.Label("英雄", itemLabelStyle, GUILayout.Height(22f));
            DrawUnitMenuGrid(heroes, campType, true);
        }
        GUILayout.EndScrollView();
        if (!string.IsNullOrEmpty(operationTip))
        {
            GUILayout.Label(operationTip, tipStyle);
        }
        GUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制红蓝双方的全局火球释放按钮。
    /// </summary>
    private void DrawGlobalSkillButtons()
    {
        GUILayout.Space(4f);
        GUILayout.Label("全局技能", campTitleStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("红方释放全局火球", GUILayout.Height(32f)))
        {
            CastGlobalStone(ECampType.Red);
        }
        if (GUILayout.Button("蓝方释放全局火球", GUILayout.Height(32f)))
        {
            CastGlobalStone(ECampType.Blue);
        }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("红方释放全局箭雨", GUILayout.Height(32f)))
        {
            CastGlobalArrow(ECampType.Red);
        }
        if (GUILayout.Button("蓝方释放全局箭雨", GUILayout.Height(32f)))
        {
            CastGlobalArrow(ECampType.Blue);
        }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("红方释放火龙吐息", GUILayout.Height(32f)))
        {
            CastGlobalDragonBreath(ECampType.Red);
        }
        if (GUILayout.Button("蓝方释放火龙吐息", GUILayout.Height(32f)))
        {
            CastGlobalDragonBreath(ECampType.Blue);
        }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("红方全军攻击强化", GUILayout.Height(32f)))
        {
            CastGlobalAttackBuff(ECampType.Red);
        }
        if (GUILayout.Button("蓝方全军攻击强化", GUILayout.Height(32f)))
        {
            CastGlobalAttackBuff(ECampType.Blue);
        }
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 使用对应阵营玩家配置的攻击力释放全局火球。
    /// </summary>
    private void CastGlobalStone(ECampType campType)
    {
        UnitLogicBase playerLogic = UnitManager.Instance.GetPlayerLogic(campType);
        if (playerLogic == null)
        {
            operationTip = $"{campType}玩家逻辑尚未创建。";
            return;
        }

        int damage = playerLogic.SoliderCfg.atk;
        bool castSuccess = GlobalSkillManager.Instance.ApplyGlobalStone(campType, GlobalStoneFlyObjectCfgId, GlobalStoneSkillCfgId, damage);
        string campName = campType == ECampType.Red ? "红方" : "蓝方";
        operationTip = castSuccess
            ? $"{campName}已释放全局火球，单发基础伤害：{damage}。"
            : $"{campName}全局火球释放失败。";
    }

    /// <summary>
    /// 释放指定阵营的全局箭雨。
    /// </summary>
    private void CastGlobalArrow(ECampType campType)
    {
        int flyObjectCfgId = campType == ECampType.Red ? GlobalArrowRedFlyObjectCfgId : GlobalArrowBlueFlyObjectCfgId;
        int skillCfgId = campType == ECampType.Red ? GlobalArrowRedSkillCfgId : GlobalArrowBlueSkillCfgId;
        bool castSuccess = GlobalSkillManager.Instance.ApplyGlobalArrow(campType, flyObjectCfgId, skillCfgId);
        string campName = campType == ECampType.Red ? "红方" : "蓝方";
        operationTip = castSuccess ? $"{campName}已释放全局箭雨。" : $"{campName}全局箭雨释放失败。";
    }

    /// <summary>
    /// 绘制指定阵营的出兵数量输入框。
    /// </summary>
    private void CastGlobalDragonBreath(ECampType campType)
    {
        bool castSuccess = GlobalSkillManager.Instance.ApplyGlobalDragonBreath(campType, GlobalDragonBreathFlyObjectCfgId, GlobalDragonBreathSkillCfgId);
        string campName = campType == ECampType.Red ? "Red" : "Blue";
        operationTip = castSuccess ? $"{campName} dragon breath cast successfully." : $"{campName} dragon breath cast failed.";
    }

    private void CastGlobalAttackBuff(ECampType campType)
    {
        bool castSuccess = GlobalSkillManager.Instance.ApplyCampAttackBuff(campType, GlobalAttackBuffId);
        string campName = campType == ECampType.Red ? "红方" : "蓝方";
        operationTip = castSuccess ? $"{campName}已获得全军攻击强化。" : $"{campName}全军攻击强化释放失败。";
    }

    private void DrawSpawnCountInput(string label, ref string countText)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(100f));
        countText = GUILayout.TextField(countText, 5, GUILayout.Width(90f));
        GUILayout.Label($"范围 {MinSpawnCount}-{MaxSpawnCount}");
        GUILayout.EndHorizontal();
    }
    /// <summary>
    /// 绘制同一类别的四列单位网格，避免英雄需要横向拖动才能选择。
    /// </summary>
    private void DrawUnitMenuGrid(List<SoldierMenuData> units, ECampType campType, bool isHero)
    {
        float cardWidth = Mathf.Max(90f, (windowRect.width - 40f) / 4f);
        float contentWidth = Mathf.Max(80f, cardWidth - 10f);
        for (int i = 0; i < units.Count; i++)
        {
            if (i % 4 == 0)
            {
                GUILayout.BeginHorizontal();
            }

            SoldierMenuData soldier = units[i];
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(cardWidth), GUILayout.Height(64f));
            GUILayout.Label(soldier.DisplayName, itemLabelStyle, GUILayout.Width(contentWidth), GUILayout.Height(26f));
            if (GUILayout.Button("生成", GUILayout.Width(contentWidth), GUILayout.Height(28f)))
            {
                SpawnSoldier(soldier.ConfigId, soldier.DisplayName, campType, isHero);
            }
            GUILayout.EndVertical();

            if (i % 4 == 3 || i == units.Count - 1)
            {
                GUILayout.EndHorizontal();
            }
        }
    }

    /// <summary>
    /// 根据当前输入数量提交一组士兵创建请求。
    /// </summary>
    private void SpawnSoldier(int configId, string displayName, ECampType campType, bool isHero)
    {
        string countText;
        if (campType == ECampType.Red)
        {
            countText = isHero ? redHeroSpawnCountText : redSpawnCountText;
        }
        else
        {
            countText = isHero ? blueHeroSpawnCountText : blueSpawnCountText;
        }

        if (!TryGetSpawnCount(ref countText, out int spawnCount))
        {
            operationTip = $"数量必须是 {MinSpawnCount}-{MaxSpawnCount} 之间的整数。";
            return;
        }

        if (campType == ECampType.Red)
        {
            if (isHero)
            {
                redHeroSpawnCountText = countText;
            }
            else
            {
                redSpawnCountText = countText;
            }
        }
        else
        {
            if (isHero)
            {
                blueHeroSpawnCountText = countText;
            }
            else
            {
                blueSpawnCountText = countText;
            }
        }

        BattleEngine.Instance.CreateUnit(configId, campType, spawnCount);
        operationTip = $"已提交：{displayName} × {spawnCount}";
    }

    /// <summary>
    /// 解析指定阵营的出兵数量，并限制到有效范围。
    /// </summary>
    private bool TryGetSpawnCount(ref string countText, out int spawnCount)
    {
        if (!int.TryParse(countText, out spawnCount))
        {
            return false;
        }

        spawnCount = Mathf.Clamp(spawnCount, MinSpawnCount, MaxSpawnCount);
        countText = spawnCount.ToString();
        return true;
    }
    /// <summary>
    /// 从 SoliderCfg 配置中建立红蓝双方的士兵菜单。
    /// </summary>
    private void LoadSoldierConfigs()
    {
        redSoldiers.Clear();
        blueSoldiers.Clear();
        redHeroes.Clear();
        blueHeroes.Clear();

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
                bool isHero = UnitFactory.IsHero(config.id);
                if (campType == ECampType.Red && isHero)
                {
                    redHeroes.Add(menuData);
                }
                else if (campType == ECampType.Blue && isHero)
                {
                    blueHeroes.Add(menuData);
                }
                else if (campType == ECampType.Red)
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

        if (configId >= 100 && configId < 200
            || configId >= 1000 && configId < 2000
            || configId >= 10000 && configId < 20000)
        {
            campType = ECampType.Red;
            return true;
        }

        if (configId >= 200 && configId < 300
            || configId >= 2000 && configId < 3000
            || configId >= 20000 && configId < 30000)
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
