using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class SetPrefabInfoEditor : EditorWindow
{
    float targetHeight = 0;
    float targetScale = 1;


    [MenuItem("Tools/Prefab Height Tool")]
    static void Open()
    {
        GetWindow<SetPrefabInfoEditor>("Prefab Height");
    }


    private void OnGUI()
    {
        GUILayout.Label("设置Prefab高度", EditorStyles.boldLabel);


        targetHeight = EditorGUILayout.FloatField(
            "Y Height",
            targetHeight
        );

        GUILayout.Label("设置Prefab高度", EditorStyles.boldLabel);


        targetScale = EditorGUILayout.FloatField(
            "Scale",
            targetScale
        );


        if (GUILayout.Button("Apply Selected Prefabs"))
        {
            ApplyPrefabs();
        }
    }



    void ApplyPrefabs()
    {
        Object[] objects =
            Selection.GetFiltered(
                typeof(GameObject),
                SelectionMode.Assets
            );


        foreach (Object obj in objects)
        {
            string path =
                AssetDatabase.GetAssetPath(obj);


            if (!path.EndsWith(".prefab"))
                continue;


            ModifyPrefab(path);
        }


        AssetDatabase.SaveAssets();

        Debug.Log("Prefab Height Applied");
    }

    void ModifyPrefab(string path)
    {
        GameObject oriPrefab = PrefabUtility.LoadPrefabContents(path);

        GameObject newRoot = oriPrefab;

        // 没有子物体
        if (oriPrefab.transform.childCount == 0)
        {
            newRoot = new GameObject(oriPrefab.name);
            oriPrefab.transform.SetParent(newRoot.transform);
            oriPrefab.transform.localPosition =
                new Vector3(
                    0,
                    targetHeight,
                    0
                );
            oriPrefab.transform.localScale = new Vector3(targetScale, targetScale, targetScale );
            // 如果原Prefab有组件，
            // 不需要移动root，只创建结构
        }
        else
        {
            // 修改第一级子物体
            Transform child = oriPrefab.transform.GetChild(0);
            Vector3 pos = child.localPosition;
            pos.y = targetHeight;
            child.localPosition = pos;
            child.localScale = new Vector3(targetScale, targetScale, targetScale);
        }

        PrefabUtility.SaveAsPrefabAsset(
            newRoot,
            path
        );
        GameObject.DestroyImmediate(newRoot);
        //PrefabUtility.UnloadPrefabContents(newRoot);
    }
}
