using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public static class MeshSubMeshSplitter
{
    [MenuItem("Mesh工具/从选中场景物体合并SubMesh+生成图集")]
    static void MergeFromSceneGameObject()
    {
        GameObject selectGo = Selection.activeGameObject;
        if (selectGo == null)
        {
            EditorUtility.DisplayDialog("提示", "请在Hierarchy窗口选中角色GameObject", "确定");
            return;
        }

        SkinnedMeshRenderer smr = selectGo.GetComponent<SkinnedMeshRenderer>();
        if (smr == null || smr.sharedMesh == null)
        {
            EditorUtility.DisplayDialog("错误", "选中物体无SkinnedMeshRenderer或Mesh", "确定");
            return;
        }

        Mesh srcMesh = smr.sharedMesh;
        Material[] mats = smr.sharedMaterials;

        RunMergeLogic(srcMesh, mats);
    }

    [MenuItem("Mesh工具/从Project选中Mesh资源合并SubMesh+生成图集")]
    static void MergeFromProjectMeshAsset()
    {
        Mesh srcMesh = Selection.activeObject as Mesh;
        if (srcMesh == null)
        {
            EditorUtility.DisplayDialog("提示", "请在Project窗口选中Mesh资源", "确定");
            return;
        }

        GameObject tempObj = EditorUtility.CreateGameObjectWithHideFlags("Temp", HideFlags.HideAndDontSave);
        SkinnedMeshRenderer tempSMR = tempObj.AddComponent<SkinnedMeshRenderer>();
        tempSMR.sharedMesh = srcMesh;
        Material[] mats = tempSMR.sharedMaterials;
        Object.DestroyImmediate(tempObj);

        RunMergeLogic(srcMesh, mats);
    }

    static void RunMergeLogic(Mesh srcMesh, Material[] sourceMats)
    {
        int subMeshCount = srcMesh.subMeshCount;
        List<Texture2D> texList = new List<Texture2D>();

        for (int i = 0; i < subMeshCount; i++)
        {
            Texture2D tex = null;
            if (i < sourceMats.Length && sourceMats[i] != null)
            {
                tex = sourceMats[i].mainTexture as Texture2D;
            }

            if (tex == null)
            {
                Texture2D whiteTex = new Texture2D(1, 1);
                whiteTex.SetPixel(0, 0, Color.white);
                whiteTex.Apply();
                tex = whiteTex;
            }

            texList.Add(tex);
        }

        Rect[] uvRects;
        Texture2D atlas = PackTexturesToAtlas(texList, out uvRects);

        Mesh newSingleMesh = new Mesh();
        newSingleMesh.name = $"{srcMesh.name}_SingleSubMesh_Atlas";

        List<Vector3> allVerts = new List<Vector3>();
        List<Vector2> allUV0 = new List<Vector2>();
        List<Vector2> allUV2 = new List<Vector2>();
        List<Vector3> allNormals = new List<Vector3>();
        List<Vector4> allTangents = new List<Vector4>();
        List<BoneWeight> allBoneWeights = new List<BoneWeight>();
        List<int> allIndices = new List<int>();

        for (int subIdx = 0; subIdx < subMeshCount; subIdx++)
        {
            int[] subIndices = srcMesh.GetIndices(subIdx);
            Dictionary<int, int> oldToNewVertMap = new Dictionary<int, int>();
            Rect targetUV = uvRects[subIdx];

            foreach (int oldVertId in subIndices)
            {
                if (!oldToNewVertMap.ContainsKey(oldVertId))
                {
                    oldToNewVertMap[oldVertId] = allVerts.Count;
                    allVerts.Add(srcMesh.vertices[oldVertId]);

                    Vector2 originUV = srcMesh.uv[oldVertId];
                    Vector2 remapUV = new Vector2(
                        targetUV.x + originUV.x * targetUV.width,
                        targetUV.y + originUV.y * targetUV.height
                    );
                    allUV0.Add(remapUV);

                    if (srcMesh.uv2 != null && srcMesh.uv2.Length > 0)
                        allUV2.Add(srcMesh.uv2[oldVertId]);
                    allNormals.Add(srcMesh.normals[oldVertId]);
                    allTangents.Add(srcMesh.tangents[oldVertId]);
                    if (srcMesh.boneWeights != null && srcMesh.boneWeights.Length > 0)
                        allBoneWeights.Add(srcMesh.boneWeights[oldVertId]);
                }

                allIndices.Add(oldToNewVertMap[oldVertId]);
            }
        }

        newSingleMesh.vertices = allVerts.ToArray();
        newSingleMesh.uv = allUV0.ToArray();
        newSingleMesh.uv2 = allUV2.ToArray();
        newSingleMesh.normals = allNormals.ToArray();
        newSingleMesh.tangents = allTangents.ToArray();
        newSingleMesh.boneWeights = allBoneWeights.ToArray();

        if (srcMesh.bindposes != null && srcMesh.bindposes.Length > 0)
        {
            newSingleMesh.bindposes = (Matrix4x4[])srcMesh.bindposes.Clone();
        }

        newSingleMesh.SetIndices(allIndices.ToArray(), MeshTopology.Triangles, 0);
        newSingleMesh.RecalculateBounds();
        newSingleMesh.UploadMeshData(false);

        string assetPath = AssetDatabase.GetAssetPath(srcMesh);
        string dir = Path.GetDirectoryName(assetPath);
        string fileName = Path.GetFileNameWithoutExtension(assetPath);

        string meshSavePath = Path.Combine(dir, $"{fileName}_SingleSubMesh_Atlas.asset");
        string atlasSavePath = Path.Combine(dir, $"{fileName}_Atlas.png");

        AssetDatabase.CreateAsset(newSingleMesh, meshSavePath);
        File.WriteAllBytes(atlasSavePath, atlas.EncodeToPNG());
        AssetDatabase.ImportAsset(atlasSavePath);

        // 自动开启贴图读写
        TextureImporter importer = AssetImporter.GetAtPath(atlasSavePath) as TextureImporter;
        if (importer != null)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("完成",
            $"生成文件：\n网格：{meshSavePath}\n图集：{atlasSavePath}\n图集已开启Read/Write可读写",
            "确定");
    }

    private static Texture2D PackTexturesToAtlas(List<Texture2D> textures, out Rect[] uvRects)
    {
        List<Rect> rectList = new List<Rect>();

        // 你这里可以改成 1024 / 2048 / 4096
        int atlasSize = 1024;
        Texture2D atlas = new Texture2D(atlasSize, atlasSize, TextureFormat.RGBA32, false, true);

        int cursorX = 0;
        int rowMaxHeight = 0;

        foreach (var tex in textures)
        {
            int w = tex.width;
            int h = tex.height;

            // 如果当前行放不下，换行
            if (cursorX + w > atlasSize)
            {
                cursorX = 0;
                rowMaxHeight = 0;
            }

            // 按原始像素大小贴进图集，不拉伸
            atlas.SetPixels(cursorX, rowMaxHeight, w, h, tex.GetPixels());

            // 计算归一化UV，只使用实际贴图占有的区域
            rectList.Add(new Rect(
                (float)cursorX / atlasSize,
                (float)rowMaxHeight / atlasSize,
                (float)w / atlasSize,
                (float)h / atlasSize
            ));

            cursorX += w;

            if (h > rowMaxHeight)
            {
                rowMaxHeight = h;
            }
        }

        atlas.Apply();
        uvRects = rectList.ToArray();
        return atlas;
    }
}