using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class ExtractGroundMesh
{
    [MenuItem("Tools/提取地面Mesh")]
    static void Extract()
    {
        GameObject go = Selection.activeGameObject;

        if (go == null)
        {
            Debug.LogError("请选择 tx_cdcj_01");
            return;
        }

        MeshFilter mf = go.GetComponent<MeshFilter>();

        if (mf == null)
        {
            Debug.LogError("没有 MeshFilter");
            return;
        }

        Mesh mesh = mf.sharedMesh;

        Vector3[] verts = mesh.vertices;
        Vector2[] uv = mesh.uv;
        Vector3[] normals = mesh.normals;
        int[] tris = mesh.triangles;

       
        //-----------------------------------------
        // 这里调节
        //-----------------------------------------
        float maxHeight = 0.3f;

        List<Vector3> newVerts = new();
        List<Vector2> newUv = new();
        List<Vector3> newNormals = new();
        List<int> newTris = new();

        Dictionary<int, int> vertMap = new();

        for (int i = 0; i < tris.Length; i += 3)
        {
            int i0 = tris[i];
            int i1 = tris[i + 1];
            int i2 = tris[i + 2];

            Vector3 v0 = verts[i0];
            Vector3 v1 = verts[i1];
            Vector3 v2 = verts[i2];

            //-----------------------------------------
            // 计算面法线
            //-----------------------------------------
            Vector3 faceNormal =
                Vector3.Cross(v1 - v0, v2 - v0).normalized;

            //-----------------------------------------
            // CDCJ高度轴是Z
            //-----------------------------------------
            bool keep =
    v0.z < 1.4f &&
    v1.z < 1.4f &&
    v2.z < 1.4f;

            // 切背景的
            //        bool keep =
            //v0.y > 42f &&
            //v1.y > 42f &&
            //v2.y > 42f;
            if (!keep)
                continue;

            AddVertex(i0);
            AddVertex(i1);
            AddVertex(i2);
        }

        Mesh newMesh = new Mesh();

        if (newVerts.Count > 65000)
            newMesh.indexFormat = IndexFormat.UInt32;

        newMesh.vertices = newVerts.ToArray();

        if (newUv.Count == newVerts.Count)
            newMesh.uv = newUv.ToArray();

        if (newNormals.Count == newVerts.Count)
            newMesh.normals = newNormals.ToArray();

        newMesh.triangles = newTris.ToArray();

        newMesh.RecalculateBounds();

        if (newNormals.Count == 0)
            newMesh.RecalculateNormals();

        string savePath =
            $"Assets/{go.name}_Ground.asset";

        AssetDatabase.DeleteAsset(savePath);

        AssetDatabase.CreateAsset(
            newMesh,
            savePath);

        AssetDatabase.SaveAssets();

        Debug.Log(
            $"完成，保留 {newTris.Count / 3} 个面");
        Debug.Log(savePath);

        //-------------------------------------------------
        void AddVertex(int oldIndex)
        {
            if (!vertMap.TryGetValue(
                    oldIndex,
                    out int newIndex))
            {
                newIndex = newVerts.Count;

                vertMap.Add(oldIndex, newIndex);

                newVerts.Add(
                    verts[oldIndex]);

                if (uv != null &&
                    uv.Length > oldIndex)
                {
                    newUv.Add(
                        uv[oldIndex]);
                }

                if (normals != null &&
                    normals.Length > oldIndex)
                {
                    newNormals.Add(
                        normals[oldIndex]);
                }
            }

            newTris.Add(newIndex);
        }
    }


}