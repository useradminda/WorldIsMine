using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MaterialBatchEditorWindow : EditorWindow
{
    private class PropertyValue
    {
        public bool Apply;
        public string Name;
        public string DisplayName;
        public ShaderUtil.ShaderPropertyType Type;
        public float FloatValue;
        public Color ColorValue;
        public Vector4 VectorValue;
        public Texture TextureValue;
        public Vector2 TextureScale = Vector2.one;
        public Vector2 TextureOffset = Vector2.zero;
    }

    private readonly List<PropertyValue> propertyList = new List<PropertyValue>();
    private readonly List<Material> targetMaterialList = new List<Material>();

    private Material referenceMaterial;
    private Shader targetShader;
    private DefaultAsset targetFolder;
    private Vector2 propertyScroll;
    private Vector2 materialScroll;
    private string propertySearch = string.Empty;
    private bool showTargetMaterials;
    private bool applyEnableInstancing;
    private bool enableInstancing = true;
    private bool applyRenderQueue;
    private int renderQueue = -1;

    [MenuItem("Tools/Material Batch Editor")]
    public static void Open()
    {
        GetWindow<MaterialBatchEditorWindow>("Material Batch Editor");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("批量材质参数工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
        referenceMaterial = (Material)EditorGUILayout.ObjectField("参考材质", referenceMaterial, typeof(Material), false);
        if (EditorGUI.EndChangeCheck() && referenceMaterial != null)
        {
            targetShader = referenceMaterial.shader;
            RebuildPropertyList();
        }

        EditorGUI.BeginChangeCheck();
        targetShader = (Shader)EditorGUILayout.ObjectField("目标 Shader", targetShader, typeof(Shader), false);
        if (EditorGUI.EndChangeCheck())
        {
            RebuildPropertyList();
            FilterTargetMaterials();
        }

        targetFolder = (DefaultAsset)EditorGUILayout.ObjectField("材质文件夹", targetFolder, typeof(DefaultAsset), false);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("扫描文件夹"))
        {
            CollectFromFolder();
        }
        if (GUILayout.Button("扫描当前选择"))
        {
            CollectFromSelection();
        }
        if (GUILayout.Button("清空目标"))
        {
            targetMaterialList.Clear();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox($"匹配材质数量：{targetMaterialList.Count}", MessageType.Info);
        showTargetMaterials = EditorGUILayout.Foldout(showTargetMaterials, "目标材质列表", true);
        if (showTargetMaterials)
        {
            materialScroll = EditorGUILayout.BeginScrollView(materialScroll, GUILayout.MaxHeight(150f));
            for (int i = 0; i < targetMaterialList.Count; i++)
            {
                EditorGUILayout.ObjectField(targetMaterialList[i], typeof(Material), false);
            }
            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.Space();
        DrawGlobalSettings();
        EditorGUILayout.Space();
        DrawPropertySettings();
        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(targetShader == null || targetMaterialList.Count == 0 || !HasAnySettingToApply()))
        {
            if (GUILayout.Button("批量应用到匹配材质", GUILayout.Height(34f)))
            {
                ApplyToMaterials();
            }
        }
    }

    private void DrawGlobalSettings()
    {
        EditorGUILayout.LabelField("材质通用设置", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        applyEnableInstancing = EditorGUILayout.ToggleLeft("修改 GPU Instancing", applyEnableInstancing, GUILayout.Width(160f));
        using (new EditorGUI.DisabledScope(!applyEnableInstancing))
        {
            enableInstancing = EditorGUILayout.Toggle("启用", enableInstancing);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        applyRenderQueue = EditorGUILayout.ToggleLeft("修改 Render Queue", applyRenderQueue, GUILayout.Width(160f));
        using (new EditorGUI.DisabledScope(!applyRenderQueue))
        {
            renderQueue = EditorGUILayout.IntField(renderQueue);
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawPropertySettings()
    {
        EditorGUILayout.LabelField("Shader 参数", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        propertySearch = EditorGUILayout.TextField("搜索", propertySearch);
        if (GUILayout.Button("全选", GUILayout.Width(55f)))
        {
            SetAllPropertiesApply(true);
        }
        if (GUILayout.Button("全不选", GUILayout.Width(65f)))
        {
            SetAllPropertiesApply(false);
        }
        EditorGUILayout.EndHorizontal();

        propertyScroll = EditorGUILayout.BeginScrollView(propertyScroll);
        for (int i = 0; i < propertyList.Count; i++)
        {
            PropertyValue property = propertyList[i];
            if (!string.IsNullOrEmpty(propertySearch) && !property.Name.ToLowerInvariant().Contains(propertySearch.ToLowerInvariant()) && !property.DisplayName.ToLowerInvariant().Contains(propertySearch.ToLowerInvariant()))
            {
                continue;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            property.Apply = EditorGUILayout.ToggleLeft($"{property.DisplayName}  ({property.Name})", property.Apply);
            using (new EditorGUI.DisabledScope(!property.Apply))
            {
                DrawPropertyValue(property);
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawPropertyValue(PropertyValue property)
    {
        switch (property.Type)
        {
            case ShaderUtil.ShaderPropertyType.Color:
                property.ColorValue = EditorGUILayout.ColorField("值", property.ColorValue);
                break;
            case ShaderUtil.ShaderPropertyType.Vector:
                property.VectorValue = EditorGUILayout.Vector4Field("值", property.VectorValue);
                break;
            case ShaderUtil.ShaderPropertyType.Float:
            case ShaderUtil.ShaderPropertyType.Range:
                property.FloatValue = EditorGUILayout.FloatField("值", property.FloatValue);
                break;
            case ShaderUtil.ShaderPropertyType.TexEnv:
                property.TextureValue = (Texture)EditorGUILayout.ObjectField("贴图", property.TextureValue, typeof(Texture), false);
                property.TextureScale = EditorGUILayout.Vector2Field("Tiling", property.TextureScale);
                property.TextureOffset = EditorGUILayout.Vector2Field("Offset", property.TextureOffset);
                break;
        }
    }

    private void RebuildPropertyList()
    {
        propertyList.Clear();
        if (targetShader == null)
        {
            return;
        }

        Material sourceMaterial = referenceMaterial != null && referenceMaterial.shader == targetShader ? referenceMaterial : new Material(targetShader);
        bool destroySourceMaterial = sourceMaterial != referenceMaterial;
        int propertyCount = ShaderUtil.GetPropertyCount(targetShader);
        for (int i = 0; i < propertyCount; i++)
        {
            string propertyName = ShaderUtil.GetPropertyName(targetShader, i);
            ShaderUtil.ShaderPropertyType propertyType = ShaderUtil.GetPropertyType(targetShader, i);
            PropertyValue property = new PropertyValue
            {
                Name = propertyName,
                DisplayName = ShaderUtil.GetPropertyDescription(targetShader, i),
                Type = propertyType
            };
            LoadPropertyValue(sourceMaterial, property);
            propertyList.Add(property);
        }

        if (destroySourceMaterial)
        {
            DestroyImmediate(sourceMaterial);
        }
    }

    private static void LoadPropertyValue(Material material, PropertyValue property)
    {
        switch (property.Type)
        {
            case ShaderUtil.ShaderPropertyType.Color:
                property.ColorValue = material.GetColor(property.Name);
                break;
            case ShaderUtil.ShaderPropertyType.Vector:
                property.VectorValue = material.GetVector(property.Name);
                break;
            case ShaderUtil.ShaderPropertyType.Float:
            case ShaderUtil.ShaderPropertyType.Range:
                property.FloatValue = material.GetFloat(property.Name);
                break;
            case ShaderUtil.ShaderPropertyType.TexEnv:
                property.TextureValue = material.GetTexture(property.Name);
                property.TextureScale = material.GetTextureScale(property.Name);
                property.TextureOffset = material.GetTextureOffset(property.Name);
                break;
        }
    }

    private void CollectFromFolder()
    {
        targetMaterialList.Clear();
        if (targetFolder == null)
        {
            EditorUtility.DisplayDialog("提示", "请先选择一个材质文件夹。", "确定");
            return;
        }

        string folderPath = AssetDatabase.GetAssetPath(targetFolder);
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            EditorUtility.DisplayDialog("提示", "选择的对象不是有效文件夹。", "确定");
            return;
        }

        AddMaterialsFromFolder(folderPath);
        FilterTargetMaterials();
    }

    private void CollectFromSelection()
    {
        targetMaterialList.Clear();
        Object[] selection = Selection.objects;
        for (int i = 0; i < selection.Length; i++)
        {
            string path = AssetDatabase.GetAssetPath(selection[i]);
            if (AssetDatabase.IsValidFolder(path))
            {
                AddMaterialsFromFolder(path);
            }
            else if (selection[i] is Material material && !targetMaterialList.Contains(material))
            {
                targetMaterialList.Add(material);
            }
        }
        FilterTargetMaterials();
    }

    private void AddMaterialsFromFolder(string folderPath)
    {
        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { folderPath });
        for (int i = 0; i < materialGuids.Length; i++)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(materialGuids[i]));
            if (material != null && !targetMaterialList.Contains(material))
            {
                targetMaterialList.Add(material);
            }
        }
    }

    private void FilterTargetMaterials()
    {
        if (targetShader == null)
        {
            return;
        }

        for (int i = targetMaterialList.Count - 1; i >= 0; i--)
        {
            if (targetMaterialList[i] == null || targetMaterialList[i].shader != targetShader)
            {
                targetMaterialList.RemoveAt(i);
            }
        }
    }

    private void SetAllPropertiesApply(bool apply)
    {
        for (int i = 0; i < propertyList.Count; i++)
        {
            propertyList[i].Apply = apply;
        }
    }

    private bool HasAnySettingToApply()
    {
        if (applyEnableInstancing || applyRenderQueue)
        {
            return true;
        }
        for (int i = 0; i < propertyList.Count; i++)
        {
            if (propertyList[i].Apply)
            {
                return true;
            }
        }
        return false;
    }

    private void ApplyToMaterials()
    {
        if (!EditorUtility.DisplayDialog("确认批量修改", $"即将修改 {targetMaterialList.Count} 个材质，是否继续？", "应用", "取消"))
        {
            return;
        }

        Undo.RecordObjects(targetMaterialList.ToArray(), "Batch Edit Materials");
        for (int i = 0; i < targetMaterialList.Count; i++)
        {
            Material material = targetMaterialList[i];
            if (applyEnableInstancing)
            {
                material.enableInstancing = enableInstancing;
            }
            if (applyRenderQueue)
            {
                material.renderQueue = renderQueue;
            }
            for (int propertyIndex = 0; propertyIndex < propertyList.Count; propertyIndex++)
            {
                PropertyValue property = propertyList[propertyIndex];
                if (property.Apply && material.HasProperty(property.Name))
                {
                    ApplyPropertyValue(material, property);
                }
            }
            EditorUtility.SetDirty(material);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成", $"已修改 {targetMaterialList.Count} 个材质。", "确定");
    }

    private static void ApplyPropertyValue(Material material, PropertyValue property)
    {
        switch (property.Type)
        {
            case ShaderUtil.ShaderPropertyType.Color:
                material.SetColor(property.Name, property.ColorValue);
                break;
            case ShaderUtil.ShaderPropertyType.Vector:
                material.SetVector(property.Name, property.VectorValue);
                break;
            case ShaderUtil.ShaderPropertyType.Float:
            case ShaderUtil.ShaderPropertyType.Range:
                material.SetFloat(property.Name, property.FloatValue);
                break;
            case ShaderUtil.ShaderPropertyType.TexEnv:
                material.SetTexture(property.Name, property.TextureValue);
                material.SetTextureScale(property.Name, property.TextureScale);
                material.SetTextureOffset(property.Name, property.TextureOffset);
                break;
        }
    }
}
