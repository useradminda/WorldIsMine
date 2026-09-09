using System;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

namespace ZTools
{
    public static class UGUISpriteAtlasTool
    {
        private const string MenuPath = "Assets/生成 UGUI Sprite Atlas";

        [MenuItem(MenuPath, false, 19)]
        private static void Generate()
        {
            string folder = SelectedFolder();
            string path = CreateAtlas(folder);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            EditorGUIUtility.PingObject(Selection.activeObject);
            Debug.Log($"已生成 UGUI Sprite Atlas：{path}。仅收录 Texture Type 为 Sprite (2D and UI) 的图片（含子目录）。");
            if (EditorSettings.spritePackerMode != SpritePackerMode.SpriteAtlasV2 &&
                EditorSettings.spritePackerMode != SpritePackerMode.SpriteAtlasV2Build)
                Debug.LogWarning("请在 Project Settings > Editor > Sprite Atlas > Mode 中点击下拉框，选择 Sprite Atlas V2 - Enabled，以便在编辑器和构建中使用图集。");
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateGenerate()
        {
            return IsAssetFolder(SelectedFolder());
        }

        private static string SelectedFolder()
        {
            var guids = Selection.assetGUIDs;
            return guids.Length == 1 ? AssetDatabase.GUIDToAssetPath(guids[0]) : null;
        }

        private static bool IsAssetFolder(string path)
        {
            return path != null && path.StartsWith("Assets/", StringComparison.Ordinal) &&
                   AssetDatabase.IsValidFolder(path);
        }

        private static string CreateAtlas(string folder)
        {
            if (!IsAssetFolder(folder))
                throw new ArgumentException("请选择 Assets 下的一个文件夹。", nameof(folder));

            string path = AssetDatabase.GenerateUniqueAssetPath(folder + ".spriteatlasv2");
            var source = new SpriteAtlasAsset();
            try
            {
                source.Add(new[] { AssetDatabase.LoadAssetAtPath<DefaultAsset>(folder) });
                SpriteAtlasAsset.Save(source, path);
            }
            finally
            {
                Object.DestroyImmediate(source);
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as SpriteAtlasImporter;
            if (importer == null)
                throw new InvalidOperationException($"无法导入 Sprite Atlas：{path}");

            var packing = importer.packingSettings;
            packing.enableRotation = false;
            packing.enableTightPacking = false;
            packing.padding = 4;
            importer.packingSettings = packing;

            var texture = importer.textureSettings;
            texture.generateMipMaps = false;
            texture.readable = false;
            texture.filterMode = FilterMode.Bilinear;
            importer.textureSettings = texture;
            importer.includeInBuild = true;
            importer.SaveAndReimport();
            return path;
        }

        // Run via -executeMethod ZTools.UGUISpriteAtlasTool.SelfCheck or the Tools menu.
        [MenuItem("Tools/UGUI Sprite Atlas/Run Self Check")]
        public static void SelfCheck()
        {
            var previousSelection = Selection.objects;
            string root = "Assets/UGUIAtlasCheck_" + Guid.NewGuid().ToString("N");
            try
            {
                AssetDatabase.CreateFolder("Assets", root.Substring("Assets/".Length));
                AssetDatabase.CreateFolder(root, "Sprites");
                string folder = root + "/Sprites";
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folder);
                if (!ValidateGenerate() || SelectedFolder() != folder)
                    throw new InvalidOperationException("Folder selection self check failed.");
                string first = CreateAtlas(folder);
                string second = CreateAtlas(folder);
                var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(first);
                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(first);
                var packables = atlas.GetPackables();
                if (first == second || AssetDatabase.LoadAssetAtPath<SpriteAtlas>(second) == null ||
                    packables.Length != 1 || AssetDatabase.GetAssetPath(packables[0]) != folder ||
                    importer.packingSettings.enableRotation || importer.packingSettings.enableTightPacking ||
                    importer.packingSettings.padding != 4 || importer.textureSettings.generateMipMaps ||
                    importer.textureSettings.readable || !importer.includeInBuild ||
                    importer.textureSettings.filterMode != FilterMode.Bilinear ||
                    IsAssetFolder("Assets") || IsAssetFolder(first) || IsAssetFolder("Packages"))
                    throw new InvalidOperationException("UGUI Sprite Atlas self check failed.");
                Debug.Log("UGUI Sprite Atlas self check passed.");
            }
            finally
            {
                Selection.objects = previousSelection;
                AssetDatabase.DeleteAsset(root);
            }
        }
    }
}
