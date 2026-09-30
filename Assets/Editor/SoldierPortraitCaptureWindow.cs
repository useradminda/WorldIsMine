using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 在非 Play 状态下批量生成士兵预制体的透明背景正面图。
/// </summary>
public class SoldierPortraitCaptureWindow : EditorWindow
{
    private const string SoldierPrefabFolder = "Assets/Resources/Soldier";
    private const string OutputFolder = "Assets/headImg";

    private int imageSize = 512;
    private float padding = 1.15f;
    private float horizontalAngle;
    private float verticalAngle = 5f;
    private Vector2 scrollPosition;

    /// <summary>
    /// 打开士兵正面图生成窗口。
    /// </summary>
    [MenuItem("Tools/士兵正面图生成工具")]
    private static void OpenWindow()
    {
        SoldierPortraitCaptureWindow window = GetWindow<SoldierPortraitCaptureWindow>("士兵正面图");
        window.minSize = new Vector2(430f, 320f);
        window.Show();
    }

    /// <summary>
    /// 绘制编辑器工具界面。
    /// </summary>
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("士兵正面图批量生成", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "读取 Assets/Resources/Soldier 下的所有预制体，在临时预览场景中渲染。" +
            "不会修改预制体，也不需要进入 Play 状态。输出 PNG 会自动导入为 Sprite。",
            MessageType.Info);

        EditorGUILayout.Space(6f);
        imageSize = EditorGUILayout.IntPopup(
            "图片尺寸",
            imageSize,
            new[] { "256 × 256", "512 × 512", "1024 × 1024", "2048 × 2048" },
            new[] { 256, 512, 1024, 2048 });
        padding = EditorGUILayout.Slider("画面留白", padding, 1.02f, 1.8f);
        horizontalAngle = EditorGUILayout.Slider("水平角度", horizontalAngle, -180f, 180f);
        verticalAngle = EditorGUILayout.Slider("俯视角度", verticalAngle, -30f, 30f);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("预制体目录", SoldierPrefabFolder);
        EditorGUILayout.LabelField("输出目录", OutputFolder);

        int prefabCount = FindSoldierPrefabPaths().Count;
        EditorGUILayout.LabelField("检测到的预制体", prefabCount.ToString());

        EditorGUILayout.Space(12f);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button($"生成全部正面图（{prefabCount}）", GUILayout.Height(36f)))
            {
                CaptureAllPrefabs();
            }

            if (GUILayout.Button("生成 Project 中选中的士兵预制体", GUILayout.Height(30f)))
            {
                CaptureSelectedPrefabs();
            }
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorGUILayout.HelpBox("请退出 Play 状态后再生成图片。", MessageType.Warning);
        }

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("打开输出目录", GUILayout.Height(25f)))
        {
            EnsureOutputFolder();
            EditorUtility.RevealInFinder(Path.GetFullPath(OutputFolder));
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 批量生成 Soldier 目录下所有预制体的正面图。
    /// </summary>
    private void CaptureAllPrefabs()
    {
        List<string> prefabPaths = FindSoldierPrefabPaths();
        CapturePrefabPaths(prefabPaths);
    }

    /// <summary>
    /// 生成当前在 Project 窗口中选中的士兵预制体正面图。
    /// </summary>
    private void CaptureSelectedPrefabs()
    {
        List<string> prefabPaths = new List<string>();
        UnityEngine.Object[] selectedObjects = Selection.objects;

        for (int i = 0; i < selectedObjects.Length; i++)
        {
            string assetPath = AssetDatabase.GetAssetPath(selectedObjects[i]);
            if (!assetPath.StartsWith(SoldierPrefabFolder, StringComparison.Ordinal) ||
                !assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            prefabPaths.Add(assetPath);
        }

        if (prefabPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("没有可生成的预制体", "请在 Project 窗口选择 Soldier 目录下的预制体。", "确定");
            return;
        }

        CapturePrefabPaths(prefabPaths);
    }

    /// <summary>
    /// 按路径列表依次渲染预制体，并允许用户中途取消。
    /// </summary>
    private void CapturePrefabPaths(List<string> prefabPaths)
    {
        if (prefabPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("没有士兵预制体", $"没有在 {SoldierPrefabFolder} 找到预制体。", "确定");
            return;
        }

        EnsureOutputFolder();
        int successCount = 0;
        int failedCount = 0;

        try
        {
            for (int i = 0; i < prefabPaths.Count; i++)
            {
                string prefabPath = prefabPaths[i];
                bool cancelled = EditorUtility.DisplayCancelableProgressBar(
                    "正在生成士兵正面图",
                    prefabPath,
                    (float)i / prefabPaths.Count);
                if (cancelled)
                {
                    break;
                }

                try
                {
                    CapturePrefab(prefabPath);
                    successCount++;
                }
                catch (Exception exception)
                {
                    failedCount++;
                    Debug.LogError($"生成士兵正面图失败：{prefabPath}\n{exception}");
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            ConfigureGeneratedSprites();
            AssetDatabase.SaveAssets();
        }

        EditorUtility.DisplayDialog(
            "生成完成",
            $"成功：{successCount}\n失败：{failedCount}\n保存位置：{OutputFolder}",
            "确定");
    }

    /// <summary>
    /// 渲染一个预制体并保存为透明 PNG。
    /// </summary>
    private void CapturePrefab(string prefabPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            throw new InvalidOperationException("无法加载预制体。");
        }

        PreviewRenderUtility preview = new PreviewRenderUtility(true);
        Texture2D capturedTexture = null;

        try
        {
            ConfigurePreview(preview);
            GameObject instance = preview.InstantiatePrefabInScene(prefab);
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.SetActive(true);

            UpdateAnimatorPose(instance);
            Bounds bounds = CalculateRenderBounds(instance);

            preview.BeginStaticPreview(new Rect(0f, 0f, imageSize, imageSize));
            ConfigureCamera(preview.camera, bounds);
            preview.camera.Render();
            capturedTexture = preview.EndStaticPreview();

            if (capturedTexture == null)
            {
                throw new InvalidOperationException("预览相机没有生成图片。");
            }

            Texture2D normalizedTexture = NormalizePortrait(capturedTexture);
            string outputPath = Path.Combine(OutputFolder, prefab.name + ".png").Replace('\\', '/');
            File.WriteAllBytes(outputPath, normalizedTexture.EncodeToPNG());
            DestroyImmediate(normalizedTexture);
        }
        finally
        {
            if (capturedTexture != null)
            {
                DestroyImmediate(capturedTexture);
            }

            preview.Cleanup();
        }
    }

    /// <summary>
    /// 配置预览环境、透明背景和双灯光。
    /// </summary>
    private void ConfigurePreview(PreviewRenderUtility preview)
    {
        preview.camera.clearFlags = CameraClearFlags.SolidColor;
        preview.camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        preview.camera.allowHDR = false;
        preview.camera.allowMSAA = true;
        preview.camera.orthographic = true;
        preview.camera.nearClipPlane = 0.01f;
        preview.camera.farClipPlane = 1000f;

        preview.ambientColor = new Color(0.42f, 0.42f, 0.42f, 1f);
        preview.lights[0].intensity = 1.25f;
        preview.lights[0].transform.rotation = Quaternion.Euler(35f, 35f, 0f);
        preview.lights[1].intensity = 0.8f;
        preview.lights[1].transform.rotation = Quaternion.Euler(340f, 215f, 0f);
    }

    /// <summary>
    /// 让 Animator 停留并刷新到默认姿势，保证蒙皮包围盒有效。
    /// </summary>
    private void UpdateAnimatorPose(GameObject instance)
    {
        Animator[] animators = instance.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Update(0f);
        }
    }

    /// <summary>
    /// 合并所有有效 Renderer 的世界包围盒。
    /// </summary>
    private Bounds CalculateRenderBounds(GameObject instance)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds bounds = new Bounds(instance.transform.position, Vector3.one);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (!renderer.enabled || renderer is ParticleSystemRenderer)
            {
                continue;
            }

            if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
            {
                Mesh bakedMesh = new Mesh();
                try
                {
                    skinnedMeshRenderer.BakeMesh(bakedMesh);
                    EncapsulateLocalBounds(
                        bakedMesh.bounds,
                        skinnedMeshRenderer.transform.localToWorldMatrix,
                        ref bounds,
                        ref hasBounds);
                }
                finally
                {
                    DestroyImmediate(bakedMesh);
                }
            }
            else if (renderer is MeshRenderer)
            {
                MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    EncapsulateLocalBounds(
                        meshFilter.sharedMesh.bounds,
                        meshFilter.transform.localToWorldMatrix,
                        ref bounds,
                        ref hasBounds);
                }
                else
                {
                    EncapsulateWorldBounds(renderer.bounds, ref bounds, ref hasBounds);
                }
            }
            else
            {
                EncapsulateWorldBounds(renderer.bounds, ref bounds, ref hasBounds);
            }
        }

        if (!hasBounds)
        {
            throw new InvalidOperationException("预制体中没有可渲染的 Renderer。");
        }

        return bounds;
    }

    /// <summary>
    /// 将局部空间包围盒的八个顶点转换到世界空间后合并。
    /// </summary>
    private void EncapsulateLocalBounds(
        Bounds localBounds,
        Matrix4x4 localToWorldMatrix,
        ref Bounds totalBounds,
        ref bool hasBounds)
    {
        Vector3 center = localBounds.center;
        Vector3 extents = localBounds.extents;

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 localPoint = center + Vector3.Scale(extents, new Vector3(x, y, z));
                    Vector3 worldPoint = localToWorldMatrix.MultiplyPoint3x4(localPoint);
                    EncapsulateWorldPoint(worldPoint, ref totalBounds, ref hasBounds);
                }
            }
        }
    }

    /// <summary>
    /// 将 Renderer 提供的世界空间包围盒合并到最终范围。
    /// </summary>
    private void EncapsulateWorldBounds(Bounds worldBounds, ref Bounds totalBounds, ref bool hasBounds)
    {
        if (!hasBounds)
        {
            totalBounds = worldBounds;
            hasBounds = true;
            return;
        }

        totalBounds.Encapsulate(worldBounds);
    }

    /// <summary>
    /// 将一个世界坐标点合并到最终范围。
    /// </summary>
    private void EncapsulateWorldPoint(Vector3 worldPoint, ref Bounds totalBounds, ref bool hasBounds)
    {
        if (!hasBounds)
        {
            totalBounds = new Bounds(worldPoint, Vector3.zero);
            hasBounds = true;
            return;
        }

        totalBounds.Encapsulate(worldPoint);
    }

    /// <summary>
    /// 使用宽松的正交相机拍下完整模型，随后再按透明像素自动裁切。
    /// </summary>
    private void ConfigureCamera(Camera previewCamera, Bounds bounds)
    {
        float radius = Mathf.Max(bounds.extents.magnitude, 0.5f);
        float distance = radius * 4f + 10f;
        Vector3 cameraDirection = Quaternion.Euler(verticalAngle, horizontalAngle, 0f) * Vector3.forward;
        previewCamera.transform.position = bounds.center + cameraDirection * distance;
        previewCamera.transform.LookAt(bounds.center, Vector3.up);
        previewCamera.orthographicSize = radius * 2.25f;
        previewCamera.nearClipPlane = 0.01f;
        previewCamera.farClipPlane = distance + radius * 4f;
    }

    /// <summary>
    /// 按非透明像素区域重新居中缩放，消除模型包围盒不准确造成的偏移和裁头。
    /// </summary>
    private Texture2D NormalizePortrait(Texture2D sourceTexture)
    {
        Color32[] sourcePixels = sourceTexture.GetPixels32();
        int sourceWidth = sourceTexture.width;
        int sourceHeight = sourceTexture.height;
        int minX = sourceWidth;
        int minY = sourceHeight;
        int maxX = -1;
        int maxY = -1;
        Color32 backgroundColor = sourcePixels[0];

        for (int y = 0; y < sourceHeight; y++)
        {
            int rowOffset = y * sourceWidth;
            for (int x = 0; x < sourceWidth; x++)
            {
                if (IsBackgroundPixel(sourcePixels[rowOffset + x], backgroundColor))
                {
                    continue;
                }

                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }
        }

        if (maxX < minX || maxY < minY)
        {
            throw new InvalidOperationException("截图中没有检测到模型像素。");
        }

        int cropWidth = maxX - minX + 1;
        int cropHeight = maxY - minY + 1;
        Texture2D croppedTexture = new Texture2D(cropWidth, cropHeight, TextureFormat.RGBA32, false);

        float contentScale = 1f / Mathf.Max(padding, 1.01f);
        float scale = Mathf.Min(
            imageSize * contentScale / cropWidth,
            imageSize * contentScale / cropHeight);
        int targetWidth = Mathf.Max(1, Mathf.RoundToInt(cropWidth * scale));
        int targetHeight = Mathf.Max(1, Mathf.RoundToInt(cropHeight * scale));

        RenderTexture scaledRenderTexture = RenderTexture.GetTemporary(
            targetWidth,
            targetHeight,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.sRGB);
        RenderTexture previousRenderTexture = RenderTexture.active;
        Texture2D scaledTexture = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
        Texture2D outputTexture = new Texture2D(imageSize, imageSize, TextureFormat.RGBA32, false);

        try
        {
            Color32[] croppedPixels = new Color32[cropWidth * cropHeight];
            for (int y = 0; y < cropHeight; y++)
            {
                Array.Copy(
                    sourcePixels,
                    (minY + y) * sourceWidth + minX,
                    croppedPixels,
                    y * cropWidth,
                    cropWidth);

                for (int x = 0; x < cropWidth; x++)
                {
                    int pixelIndex = y * cropWidth + x;
                    if (IsBackgroundPixel(croppedPixels[pixelIndex], backgroundColor))
                    {
                        croppedPixels[pixelIndex].a = 0;
                    }
                }
            }

            croppedTexture.SetPixels32(croppedPixels);
            croppedTexture.Apply(false, false);
            Graphics.Blit(croppedTexture, scaledRenderTexture);
            RenderTexture.active = scaledRenderTexture;
            scaledTexture.ReadPixels(new Rect(0f, 0f, targetWidth, targetHeight), 0, 0, false);
            scaledTexture.Apply(false, false);

            Color32[] transparentPixels = new Color32[imageSize * imageSize];
            outputTexture.SetPixels32(transparentPixels);
            int targetX = (imageSize - targetWidth) / 2;
            int targetY = (imageSize - targetHeight) / 2;
            outputTexture.SetPixels32(targetX, targetY, targetWidth, targetHeight, scaledTexture.GetPixels32());
            outputTexture.Apply(false, false);
            return outputTexture;
        }
        catch
        {
            DestroyImmediate(outputTexture);
            throw;
        }
        finally
        {
            RenderTexture.active = previousRenderTexture;
            RenderTexture.ReleaseTemporary(scaledRenderTexture);
            DestroyImmediate(croppedTexture);
            DestroyImmediate(scaledTexture);
        }
    }

    /// <summary>
    /// 判断像素是否属于预览相机的黑色或透明背景。
    /// </summary>
    private bool IsBackgroundPixel(Color32 pixel, Color32 backgroundColor)
    {
        if (pixel.a <= 2)
        {
            return true;
        }

        int redDistance = Mathf.Abs(pixel.r - backgroundColor.r);
        int greenDistance = Mathf.Abs(pixel.g - backgroundColor.g);
        int blueDistance = Mathf.Abs(pixel.b - backgroundColor.b);
        bool sameAsBackground = redDistance <= 8 && greenDistance <= 8 && blueDistance <= 8;
        bool blackBackground = pixel.r <= 8 && pixel.g <= 8 && pixel.b <= 8;
        return sameAsBackground || blackBackground;
    }

    /// <summary>
    /// 查找并按路径稳定排序 Soldier 目录下的全部预制体。
    /// </summary>
    private List<string> FindSoldierPrefabPaths()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { SoldierPrefabFolder });
        List<string> prefabPaths = new List<string>(prefabGuids.Length);

        for (int i = 0; i < prefabGuids.Length; i++)
        {
            prefabPaths.Add(AssetDatabase.GUIDToAssetPath(prefabGuids[i]));
        }

        prefabPaths.Sort(StringComparer.Ordinal);
        return prefabPaths;
    }

    /// <summary>
    /// 确保图片输出目录存在。
    /// </summary>
    private void EnsureOutputFolder()
    {
        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            AssetDatabase.CreateFolder("Assets", "headImg");
        }
    }

    /// <summary>
    /// 将生成的 PNG 设置为透明、无压缩的 UI Sprite。
    /// </summary>
    private void ConfigureGeneratedSprites()
    {
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { OutputFolder });
        for (int i = 0; i < textureGuids.Length; i++)
        {
            string texturePath = AssetDatabase.GUIDToAssetPath(textureGuids[i]);
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
