using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class LobbyPrefabBuilder
{
    private const string AtlasPath = "Assets/UI/04_Lobby/04_Lobby_Sprite.png";
    private const string PrefabPath = "Assets/UI/04_Lobby/04_Lobby.prefab";

    private struct ImageLayer
    {
        public string Name;
        public float X;
        public float Y;
        public float Width;
        public float Height;

        public ImageLayer(string name, float x, float y, float width, float height)
        {
            Name = name;
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    private static readonly ImageLayer[] Layers =
    {
        new ImageLayer("join_blue_top", 25, 44, 475, 152),
        new ImageLayer("join_red_top", 580, 44, 475, 152),
        new ImageLayer("health_strip_blue", 137, 185, 293, 38),
        new ImageLayer("health_strip_red", 650, 185, 293, 38),
        new ImageLayer("faction_blue", 15, 215, 92, 128),
        new ImageLayer("faction_red", 971, 215, 92, 128),
        new ImageLayer("timer_panel", 435, 16, 210, 239),
        new ImageLayer("icon_settings", 973, 357, 68, 68),
        new ImageLayer("icon_rank", 973, 450, 68, 68),
        new ImageLayer("ranking_panel", 746, 256, 218, 268),
        new ImageLayer("medal_gold_top", 767, 350, 38, 38),
        new ImageLayer("medal_silver_top", 767, 399, 38, 38),
        new ImageLayer("medal_bronze_top", 767, 448, 38, 38),
        new ImageLayer("join_blue_bottom", 35, 1639, 495, 166),
        new ImageLayer("join_red_bottom", 550, 1639, 495, 166),
        new ImageLayer("notification_panel_top", 161, 276, 560, 165),
        new ImageLayer("hero_blue", 61, 470, 193, 279),
        new ImageLayer("join_blue_middle", 240, 542, 475, 152),
        new ImageLayer("notification_panel_middle", 250, 795, 580, 172),
        new ImageLayer("medal_gold_result", 406, 1000, 270, 272),
        new ImageLayer("medal_silver_result", 157, 1100, 270, 272),
        new ImageLayer("medal_bronze_result", 655, 1100, 270, 272),
        new ImageLayer("btn_primary_gold", 320, 1499, 440, 108)
    };

    [MenuItem("Tools/UI/Lobby/Create 04_Lobby Prefab")]
    private static void CreatePrefab()
    {
        Sprite[] sprites = LoadSprites();
        if (sprites == null)
        {
            return;
        }

        Dictionary<string, Sprite> spriteMap = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        for (int i = 0; i < sprites.Length; i++)
        {
            spriteMap[sprites[i].name] = sprites[i];
        }

        GameObject root = new GameObject("04_Lobby", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        for (int i = 0; i < Layers.Length; i++)
        {
            ImageLayer layer = Layers[i];
            string spriteName = GetSpriteName(layer.Name);
            if (!spriteMap.TryGetValue(spriteName, out Sprite sprite))
            {
                Debug.LogWarning($"Lobby 图集找不到 Sprite：{spriteName}");
                continue;
            }

            GameObject imageObject = new GameObject(layer.Name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(root.transform, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(layer.X, -layer.Y);
            rect.sizeDelta = new Vector2(layer.Width, layer.Height);
            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        EditorGUIUtility.PingObject(Selection.activeObject);
        Debug.Log($"Lobby 预制体已生成：{PrefabPath}");
    }

    private static Sprite[] LoadSprites()
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(AtlasPath);
        List<Sprite> result = new List<Sprite>();
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite)
            {
                result.Add(sprite);
            }
        }

        if (result.Count == 0)
        {
            EditorUtility.DisplayDialog("生成失败", $"没有在图集中找到子 Sprite：{AtlasPath}", "确定");
            return null;
        }
        return result.ToArray();
    }

    private static string GetSpriteName(string layerName)
    {
        if (layerName.StartsWith("join_blue", StringComparison.Ordinal))
        {
            return "join_blue";
        }
        if (layerName.StartsWith("join_red", StringComparison.Ordinal))
        {
            return "join_red";
        }
        if (layerName.StartsWith("health_strip_blue", StringComparison.Ordinal))
        {
            return "health_strip_blue_decorated";
        }
        if (layerName.StartsWith("health_strip_red", StringComparison.Ordinal))
        {
            return "health_strip_red_decorated";
        }
        if (layerName.StartsWith("medal_gold", StringComparison.Ordinal))
        {
            return "medal_gold";
        }
        if (layerName.StartsWith("medal_silver", StringComparison.Ordinal))
        {
            return "medal_silver";
        }
        if (layerName.StartsWith("medal_bronze", StringComparison.Ordinal))
        {
            return "medal_bronze";
        }
        if (layerName.StartsWith("notification_panel", StringComparison.Ordinal))
        {
            return "notification_panel";
        }
        return layerName;
    }
}
