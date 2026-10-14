using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class UiTexturePackerWindow : EditorWindow
{
    private DefaultAsset selectedFolder;
    private readonly List<DefaultAsset> selectedFolders = new List<DefaultAsset>();
    private int padding = 2;
    private int maxSize = 4096;
    private readonly Dictionary<string, bool> readableStates = new Dictionary<string, bool>();

    [MenuItem("Tools/UI/TexturePacker")]
    private static void OpenWindow()
    {
        GetWindow<UiTexturePackerWindow>("UI TexturePacker");
    }

    [MenuItem("Assets/UI/生成选中文件夹图集", false, 2000)]
    private static void GenerateSelectedProjectFolders()
    {
        List<DefaultAsset> folders = CollectSelectedSpritesFolders();
        if (folders.Count == 0)
        {
            EditorUtility.DisplayDialog("生成失败", "请在 Project 中选择 Sprites 文件夹，或选择包含 Sprites 子文件夹的父目录。", "确定");
            return;
        }
        UiTexturePackerWindow generator = CreateInstance<UiTexturePackerWindow>();
        generator.selectedFolders.AddRange(folders);
        generator.GenerateSelectedFolders();
        DestroyImmediate(generator);
    }

    private static List<DefaultAsset> CollectSelectedSpritesFolders()
    {
        List<DefaultAsset> result = new List<DefaultAsset>();
        HashSet<string> addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        UnityEngine.Object[] objects = Selection.objects;
        for (int i = 0; i < objects.Length; i++)
        {
            string selectedPath = AssetDatabase.GetAssetPath(objects[i]);
            if (!AssetDatabase.IsValidFolder(selectedPath))
            {
                continue;
            }
            string spritesPath = Path.GetFileName(selectedPath).Equals("Sprites", StringComparison.OrdinalIgnoreCase) ? selectedPath : selectedPath.TrimEnd('/') + "/Sprites";
            if (!AssetDatabase.IsValidFolder(spritesPath) || !addedPaths.Add(spritesPath))
            {
                continue;
            }
            DefaultAsset folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(spritesPath);
            if (folder != null)
            {
                result.Add(folder);
            }
        }
        return result;
    }

    private void OnGUI()
    {
        DrawBatchUi();
        return;
        /*
        EditorGUILayout.HelpBox("选择一个 Sprites 文件夹，生成 PNG 图集和 TexturePacker .tpsheet 文件。", MessageType.Info);
        selectedFolder = (DefaultAsset)EditorGUILayout.ObjectField("Sprites 文件夹", selectedFolder, typeof(DefaultAsset), false);
        padding = EditorGUILayout.IntField("图集间距", padding);
        maxSize = EditorGUILayout.IntPopup("最大尺寸", maxSize, new[] { "1024", "2048", "4096", "8192" }, new[] { 1024, 2048, 4096, 8192 });

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(!IsSpritesFolder(selectedFolder)))
        {
            if (GUILayout.Button("生成当前文件夹图集", GUILayout.Height(30f)))
            {
                GenerateFolder(AssetDatabase.GetAssetPath(selectedFolder));
            }
        }

        if (GUILayout.Button("添加当前选中的多个 Sprites 文件夹", GUILayout.Height(26f)))
        {
            AddSelectedSpritesFolders();
        }

        for (int i = selectedFolders.Count - 1; i >= 0; i--)
        {
            if (selectedFolders[i] == null || !IsSpritesFolder(selectedFolders[i]))
            {
                selectedFolders.RemoveAt(i);
            }
        }

        for (int i = 0; i < selectedFolders.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(selectedFolders[i]));
            if (GUILayout.Button("移除", GUILayout.Width(50f)))
            {
                selectedFolders.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }

        using (new EditorGUI.DisabledScope(selectedFolders.Count == 0))
        {
            if (GUILayout.Button("生成已选择的多个图集", GUILayout.Height(30f)))
            {
                GenerateSelectedFolders();
            }
        }

        if (GUILayout.Button("扫描 Assets/UI 下全部 Sprites 文件夹", GUILayout.Height(30f)))
        {
            GenerateAllUiFolders();
        }
    }

        */
    }

    private void DrawBatchUi()
    {
        AddSelectedSpritesFolders();
        EditorGUILayout.HelpBox("将多个 Sprites 文件夹拖入上方区域，或在 Project 中多选后添加。最后统一生成图集。", MessageType.Info);
        Rect dropRect = GUILayoutUtility.GetRect(0f, 32f, GUILayout.ExpandWidth(true));
        GUI.Box(dropRect, "Sprites 文件夹（可连续拖入多个）", EditorStyles.objectField);
        HandleFolderDrop(dropRect);
        padding = EditorGUILayout.IntField("图集间距", padding);
        maxSize = EditorGUILayout.IntPopup("最大尺寸", maxSize, new[] { "1024", "2048", "4096", "8192" }, new[] { 1024, 2048, 4096, 8192 });
        for (int i = selectedFolders.Count - 1; i >= 0; i--)
        {
            if (selectedFolders[i] == null || !IsSpritesFolder(selectedFolders[i]))
            {
                selectedFolders.RemoveAt(i);
            }
        }
        for (int i = 0; i < selectedFolders.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(selectedFolders[i]));
            if (GUILayout.Button("移除", GUILayout.Width(50f)))
            {
                selectedFolders.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }
        using (new EditorGUI.DisabledScope(selectedFolders.Count == 0))
        {
            if (GUILayout.Button("生成已添加的多个图集", GUILayout.Height(30f)))
            {
                GenerateSelectedFolders();
            }
        }
    }

    private void HandleFolderDrop(Rect dropRect)
    {
        Event currentEvent = Event.current;
        if (!dropRect.Contains(currentEvent.mousePosition))
        {
            return;
        }
        if (currentEvent.type == EventType.DragUpdated || currentEvent.type == EventType.DragPerform)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (currentEvent.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                AddFolders(DragAndDrop.objectReferences);
            }
            currentEvent.Use();
        }
    }

    private void AddFolders(UnityEngine.Object[] objects)
    {
        for (int i = 0; i < objects.Length; i++)
        {
            DefaultAsset folder = objects[i] as DefaultAsset;
            if (IsSpritesFolder(folder) && !selectedFolders.Contains(folder))
            {
                selectedFolders.Add(folder);
            }
        }
    }

    private void AddSelectedSpritesFolders()
    {
        UnityEngine.Object[] objects = Selection.objects;
        for (int i = 0; i < objects.Length; i++)
        {
            if (!IsSpritesFolder(objects[i]) || selectedFolders.Contains(objects[i] as DefaultAsset))
            {
                continue;
            }
            selectedFolders.Add(objects[i] as DefaultAsset);
        }
    }

    private void GenerateSelectedFolders()
    {
        int generatedCount = 0;
        for (int i = 0; i < selectedFolders.Count; i++)
        {
            if (GenerateFolder(AssetDatabase.GetAssetPath(selectedFolders[i])))
            {
                generatedCount++;
            }
        }
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("生成完成", $"已生成 {generatedCount} 个 UI 图集。", "确定");
    }

    private static bool IsSpritesFolder(UnityEngine.Object folderObject)
    {
        if (folderObject == null)
        {
            return false;
        }
        string folderPath = AssetDatabase.GetAssetPath(folderObject);
        return AssetDatabase.IsValidFolder(folderPath) && Path.GetFileName(folderPath).Equals("Sprites", StringComparison.OrdinalIgnoreCase);
    }

    private void GenerateAllUiFolders()
    {
        string uiPath = "Assets/UI";
        if (!AssetDatabase.IsValidFolder(uiPath))
        {
            EditorUtility.DisplayDialog("生成失败", "Assets/UI 文件夹不存在。", "确定");
            return;
        }

        string fullUiPath = Path.GetFullPath(uiPath).Replace("/", Path.DirectorySeparatorChar.ToString());
        string[] folders = Directory.GetDirectories(fullUiPath, "Sprites", SearchOption.AllDirectories);
        int generatedCount = 0;
        for (int i = 0; i < folders.Length; i++)
        {
            string assetPath = ToAssetPath(folders[i]);
            if (GenerateFolder(assetPath))
            {
                generatedCount++;
            }
        }
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("生成完成", $"已生成 {generatedCount} 个 UI 图集。", "确定");
    }

    private bool GenerateFolder(string folderAssetPath)
    {
        if (!AssetDatabase.IsValidFolder(folderAssetPath) || !Path.GetFileName(folderAssetPath).Equals("Sprites", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string folderFullPath = Path.GetFullPath(folderAssetPath).Replace("/", Path.DirectorySeparatorChar.ToString());
        string[] imageFiles = Directory.GetFiles(folderFullPath, "*.png", SearchOption.TopDirectoryOnly);
        if (imageFiles.Length == 0)
        {
            Debug.LogWarning($"UI 图集跳过，没有找到 PNG：{folderAssetPath}");
            return false;
        }

        List<Texture2D> textures = new List<Texture2D>();
        List<string> spriteNames = new List<string>();
        List<string> importerPaths = new List<string>();
        try
        {
            for (int i = 0; i < imageFiles.Length; i++)
            {
                string assetPath = ToAssetPath(imageFiles[i]);
                Texture2D texture = LoadReadableTexture(assetPath);
                if (texture == null)
                {
                    continue;
                }
                textures.Add(texture);
                spriteNames.Add(Path.GetFileNameWithoutExtension(imageFiles[i]));
                importerPaths.Add(assetPath);
            }

            if (textures.Count == 0)
            {
                return false;
            }

            Texture2D atlas = new Texture2D(maxSize, maxSize, TextureFormat.RGBA32, false);
            Rect[] rects = atlas.PackTextures(textures.ToArray(), Mathf.Max(0, padding), maxSize, false);
            string folderName = Path.GetFileName(Path.GetDirectoryName(folderFullPath));
            string atlasName = folderName + "_Sprite";
            string outputFolderAssetPath = Path.GetDirectoryName(folderAssetPath).Replace("\\", "/");
            string atlasAssetPath = outputFolderAssetPath + "/" + atlasName + ".png";
            string atlasFullPath = Path.GetFullPath(atlasAssetPath).Replace("/", Path.DirectorySeparatorChar.ToString());
            int atlasWidth = atlas.width;
            int atlasHeight = atlas.height;
            File.WriteAllBytes(atlasFullPath, atlas.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(atlas);
            string sheetAssetPath = outputFolderAssetPath + "/" + atlasName + ".tpsheet";
            string sheetFullPath = Path.GetFullPath(sheetAssetPath).Replace("/", Path.DirectorySeparatorChar.ToString());
            File.WriteAllText(sheetFullPath, BuildSheetText(atlasName + ".png", atlasWidth, atlasHeight, spriteNames, rects), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(atlasAssetPath, ImportAssetOptions.ForceUpdate);
            SetAtlasSpriteImporter(atlasAssetPath);
            Debug.Log($"UI 图集生成完成：{atlasAssetPath}");
            return true;
        }
        finally
        {
            for (int i = 0; i < importerPaths.Count; i++)
            {
                TextureImporter importer = AssetImporter.GetAtPath(importerPaths[i]) as TextureImporter;
                if (importer != null && readableStates.TryGetValue(importerPaths[i], out bool oldReadable))
                {
                    importer.isReadable = oldReadable;
                    importer.SaveAndReimport();
                }
                AssetDatabase.ImportAsset(importerPaths[i], ImportAssetOptions.ForceUpdate);
            }
            readableStates.Clear();
        }
    }

    private Texture2D LoadReadableTexture(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            return null;
        }
        bool oldReadable = importer.isReadable;
        if (!readableStates.ContainsKey(assetPath))
        {
            readableStates.Add(assetPath, oldReadable);
        }
        if (!oldReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
    }

    private void SetAtlasSpriteImporter(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            return;
        }
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    private string BuildSheetText(string textureName, int width, int height, List<string> names, Rect[] rects)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("#");
        builder.AppendLine("# Sprite sheet data for Unity.");
        builder.AppendLine("#");
        builder.AppendLine(":format=40300");
        builder.AppendLine(":texture=" + textureName);
        builder.AppendLine($":size={width}x{height}");
        builder.AppendLine(":pivotpoints=enabled");
        builder.AppendLine(":borders=disabled");
        builder.AppendLine(":alphahandling=ClearTransparentPixels");
        for (int i = 0; i < names.Count; i++)
        {
            int x = Mathf.RoundToInt(rects[i].x * width);
            int y = Mathf.RoundToInt(rects[i].y * height);
            int spriteWidth = Mathf.RoundToInt(rects[i].width * width);
            int spriteHeight = Mathf.RoundToInt(rects[i].height * height);
            builder.AppendLine($"{names[i]};{x};{y};{spriteWidth};{spriteHeight}; 0.5;0.5; 0;0;0;0");
        }
        return builder.ToString();
    }

    private static string ToAssetPath(string fullPath)
    {
        string normalized = fullPath.Replace("\\", "/");
        string projectPath = Directory.GetParent(Application.dataPath).FullName.Replace("\\", "/");
        return normalized.StartsWith(projectPath + "/", StringComparison.OrdinalIgnoreCase) ? normalized.Substring(projectPath.Length + 1) : normalized;
    }
}
