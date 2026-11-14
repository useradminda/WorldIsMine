using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色网格切片工具，按三角形中心的随机空间分区生成多个碎片，并保留原始 UV 与法线。
/// 适用于冰冻角色死亡时的碎裂表现，也可以被其他需要网格碎片的死亡效果复用。
/// </summary>
public static class MeshSplitUtility
{
    private const int MaxPooledPieceCount = 256;
    private const int MaxPooledRootCount = 32;
    private const int MaxPooledBakedMeshCount = 16;
    private static readonly Stack<GameObject> PiecePool = new Stack<GameObject>();
    private static readonly Stack<GameObject> RootPool = new Stack<GameObject>();
    private static readonly Stack<Mesh> BakedMeshPool = new Stack<Mesh>();
    private static readonly Dictionary<MaterialCacheKey, Material> MaterialCache = new Dictionary<MaterialCacheKey, Material>();

    /// <summary>
    /// 从本地对象池获取碎片根节点，用于承载本次死亡表现生成的全部碎片。
    /// </summary>
    public static GameObject AcquirePiecesRoot(string objectName, Vector3 position)
    {
        GameObject root = null;
        while (RootPool.Count > 0 && root == null)
        {
            root = RootPool.Pop();
        }

        if (root == null)
        {
            root = new GameObject(objectName);
        }
        else
        {
            root.name = objectName;
        }

        root.transform.SetParent(null, false);
        root.transform.position = position;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        root.SetActive(true);
        return root;
    }

    /// <summary>
    /// 将碎片根节点放回本地对象池，超过缓存上限时直接销毁。
    /// </summary>
    public static void RecyclePiecesRoot(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        root.transform.SetParent(null, false);
        root.SetActive(false);
        if (RootPool.Count < MaxPooledRootCount)
        {
            RootPool.Push(root);
            return;
        }

        UnityEngine.Object.Destroy(root);
    }

    /// <summary>
    /// 创建指定数量的角色网格碎片。SkinnedMeshRenderer 会先烘焙为当前帧网格，避免碎片继续受骨骼动画驱动。
    /// </summary>
    public static List<MeshSplitPiece> CreatePieces(GameObject sourceRoot, Transform piecesRoot, int pieceCount, Shader shardShader, int randomSeed)
    {
        List<MeshSplitPiece> pieces = new List<MeshSplitPiece>();
        if (sourceRoot == null || piecesRoot == null || shardShader == null)
        {
            return pieces;
        }

        System.Random random = new System.Random(randomSeed);
        bool hasPrimarySource = false;
        MeshFilter[] meshFilters = sourceRoot.GetComponentsInChildren<MeshFilter>(false);
        for (int i = 0; i < meshFilters.Length; i++)
        {
            MeshFilter meshFilter = meshFilters[i];
            MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
            if (meshRenderer == null || meshFilter.sharedMesh == null)
            {
                continue;
            }

            CreateSourcePieces(
                meshFilter.sharedMesh,
                meshFilter.transform.localToWorldMatrix,
                meshRenderer.sharedMaterials,
                piecesRoot,
                hasPrimarySource ? 1 : pieceCount,
                shardShader,
                random,
                pieces);
            hasPrimarySource = true;
        }

        SkinnedMeshRenderer[] skinnedRenderers = sourceRoot.GetComponentsInChildren<SkinnedMeshRenderer>(false);
        for (int i = 0; i < skinnedRenderers.Length; i++)
        {
            SkinnedMeshRenderer skinnedRenderer = skinnedRenderers[i];
            if (skinnedRenderer.sharedMesh == null)
            {
                continue;
            }

            Mesh bakedMesh = AcquireBakedMesh();
            bakedMesh.name = skinnedRenderer.name + "_SplitBaked";
            skinnedRenderer.BakeMesh(bakedMesh);
            CreateSourcePieces(
                bakedMesh,
                skinnedRenderer.transform.localToWorldMatrix,
                skinnedRenderer.sharedMaterials,
                piecesRoot,
                hasPrimarySource ? 1 : pieceCount,
                shardShader,
                random,
                pieces);
            hasPrimarySource = true;
            RecycleBakedMesh(bakedMesh);
        }

        return pieces;
    }

    /// <summary>
    /// 从本地对象池获取用于 SkinnedMeshRenderer 烘焙的临时网格。
    /// </summary>
    private static Mesh AcquireBakedMesh()
    {
        Mesh mesh = null;
        while (BakedMeshPool.Count > 0 && mesh == null)
        {
            mesh = BakedMeshPool.Pop();
        }

        if (mesh == null)
        {
            mesh = new Mesh();
        }
        else
        {
            mesh.Clear();
        }

        return mesh;
    }

    /// <summary>
    /// 将 SkinnedMeshRenderer 烘焙临时网格放回本地对象池。
    /// </summary>
    private static void RecycleBakedMesh(Mesh mesh)
    {
        mesh.Clear();
        if (BakedMeshPool.Count < MaxPooledBakedMeshCount)
        {
            BakedMeshPool.Push(mesh);
            return;
        }

        UnityEngine.Object.Destroy(mesh);
    }

    /// <summary>
    /// 将单个源网格按随机空间种子分组，并创建对应的碎片对象。
    /// </summary>
    private static void CreateSourcePieces(
        Mesh sourceMesh,
        Matrix4x4 localToWorld,
        Material[] sourceMaterials,
        Transform piecesRoot,
        int pieceCount,
        Shader shardShader,
        System.Random random,
        List<MeshSplitPiece> pieces)
    {
        Vector3[] sourceVertices = sourceMesh.vertices;
        int[] sourceTriangles = sourceMesh.triangles;
        if (!sourceMesh.isReadable || sourceVertices == null || sourceTriangles == null || sourceTriangles.Length < 3)
        {
            return;
        }

        Vector2[] sourceUvs = sourceMesh.uv;
        Vector3[] sourceNormals = sourceMesh.normals;
        int triangleCount = sourceTriangles.Length / 3;
        int actualPieceCount = Mathf.Clamp(pieceCount, 1, triangleCount);
        Vector3[] triangleCenters = new Vector3[triangleCount];
        for (int i = 0; i < triangleCount; i++)
        {
            int triangleOffset = i * 3;
            Vector3 a = localToWorld.MultiplyPoint3x4(sourceVertices[sourceTriangles[triangleOffset]]);
            Vector3 b = localToWorld.MultiplyPoint3x4(sourceVertices[sourceTriangles[triangleOffset + 1]]);
            Vector3 c = localToWorld.MultiplyPoint3x4(sourceVertices[sourceTriangles[triangleOffset + 2]]);
            triangleCenters[i] = (a + b + c) / 3f;
        }

        int[] seedTriangles = SelectSeedTriangles(triangleCenters, actualPieceCount, random);
        List<int>[] triangleGroups = new List<int>[actualPieceCount];
        for (int i = 0; i < actualPieceCount; i++)
        {
            triangleGroups[i] = new List<int>();
            triangleGroups[i].Add(seedTriangles[i]);
        }

        for (int i = 0; i < triangleCount; i++)
        {
            if (ContainsSeed(seedTriangles, i))
            {
                continue;
            }

            int nearestGroup = 0;
            float nearestDistance = float.MaxValue;
            for (int group = 0; group < seedTriangles.Length; group++)
            {
                float distance = (triangleCenters[i] - triangleCenters[seedTriangles[group]]).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestGroup = group;
                }
            }

            triangleGroups[nearestGroup].Add(i);
        }

        for (int group = 0; group < triangleGroups.Length; group++)
        {
            CreatePiece(
                sourceMesh,
                localToWorld,
                sourceMaterials,
                sourceVertices,
                sourceUvs,
                sourceNormals,
                sourceTriangles,
                triangleGroups[group],
                triangleCenters[seedTriangles[group]],
                piecesRoot,
                shardShader,
                pieces);
        }
    }

    /// <summary>
    /// 根据三角形中心选择不重复的随机种子，保证每个碎片至少包含一个三角形。
    /// </summary>
    private static int[] SelectSeedTriangles(Vector3[] centers, int count, System.Random random)
    {
        int[] seeds = new int[count];
        HashSet<int> selected = new HashSet<int>();
        for (int i = 0; i < count; i++)
        {
            int seed;
            do
            {
                seed = random.Next(centers.Length);
            }
            while (!selected.Add(seed));

            seeds[i] = seed;
        }

        return seeds;
    }

    /// <summary>
    /// 判断三角形是否已经作为某个碎片的种子使用。
    /// </summary>
    private static bool ContainsSeed(int[] seeds, int triangleIndex)
    {
        for (int i = 0; i < seeds.Length; i++)
        {
            if (seeds[i] == triangleIndex)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 创建一个碎片对象，复制顶点、法线、UV 和三角形索引，确保原始纹理在碎片上保持正确映射。
    /// </summary>
    private static void CreatePiece(
        Mesh sourceMesh,
        Matrix4x4 localToWorld,
        Material[] sourceMaterials,
        Vector3[] sourceVertices,
        Vector2[] sourceUvs,
        Vector3[] sourceNormals,
        int[] sourceTriangles,
        List<int> triangleGroup,
        Vector3 worldCenter,
        Transform piecesRoot,
        Shader shardShader,
        List<MeshSplitPiece> pieces)
    {
        GameObject pieceObject = AcquirePieceObject(sourceMesh.name + "_IceShard", out MeshFilter meshFilter, out MeshRenderer meshRenderer);
        pieceObject.transform.SetParent(piecesRoot, true);
        pieceObject.transform.position = worldCenter;
        pieceObject.transform.rotation = Quaternion.identity;
        pieceObject.transform.localScale = Vector3.one;

        Mesh pieceMesh = meshFilter.sharedMesh;
        pieceMesh.Clear();
        pieceMesh.name = pieceObject.name + "_Mesh";
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();
        Matrix4x4 worldToPiece = pieceObject.transform.worldToLocalMatrix;
        Matrix4x4 normalMatrix = localToWorld;

        for (int i = 0; i < triangleGroup.Count; i++)
        {
            int triangleIndex = triangleGroup[i] * 3;
            for (int vertexIndex = 0; vertexIndex < 3; vertexIndex++)
            {
                int sourceIndex = sourceTriangles[triangleIndex + vertexIndex];
                Vector3 worldPosition = localToWorld.MultiplyPoint3x4(sourceVertices[sourceIndex]);
                vertices.Add(worldToPiece.MultiplyPoint3x4(worldPosition));
                Vector3 normal = sourceNormals != null && sourceNormals.Length == sourceVertices.Length
                    ? normalMatrix.MultiplyVector(sourceNormals[sourceIndex]).normalized
                    : Vector3.up;
                normals.Add(pieceObject.transform.InverseTransformDirection(normal));
                uvs.Add(sourceUvs != null && sourceUvs.Length == sourceVertices.Length ? sourceUvs[sourceIndex] : Vector2.zero);
                triangles.Add(vertices.Count - 1);
            }
        }

        pieceMesh.SetVertices(vertices);
        pieceMesh.SetNormals(normals);
        pieceMesh.SetUVs(0, uvs);
        pieceMesh.SetTriangles(triangles, 0, true);
        pieceMesh.RecalculateBounds();

        Material sourceMaterial = sourceMaterials != null && sourceMaterials.Length > 0 ? sourceMaterials[0] : null;
        Material shardMaterial = GetSharedShardMaterial(sourceMaterial, shardShader);
        meshRenderer.sharedMaterial = shardMaterial;
        pieces.Add(new MeshSplitPiece(pieceObject, meshRenderer, shardMaterial));
    }

    /// <summary>
    /// 从本地对象池获取碎片对象，首次使用时创建 Mesh、MeshFilter 和 MeshRenderer。
    /// </summary>
    private static GameObject AcquirePieceObject(string objectName, out MeshFilter meshFilter, out MeshRenderer meshRenderer)
    {
        GameObject pieceObject = null;
        while (PiecePool.Count > 0 && pieceObject == null)
        {
            pieceObject = PiecePool.Pop();
        }

        if (pieceObject == null)
        {
            pieceObject = new GameObject(objectName);
            meshFilter = pieceObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = new Mesh();
            meshRenderer = pieceObject.AddComponent<MeshRenderer>();
        }
        else
        {
            pieceObject.name = objectName;
            meshFilter = pieceObject.GetComponent<MeshFilter>();
            meshRenderer = pieceObject.GetComponent<MeshRenderer>();
        }

        pieceObject.SetActive(true);
        return pieceObject;
    }

    /// <summary>
    /// 将碎片对象放回本地对象池，清空运行时网格和渲染参数供下一次碎裂复用。
    /// </summary>
    public static void RecyclePiece(MeshSplitPiece piece)
    {
        if (piece == null || piece.GameObject == null)
        {
            return;
        }

        piece.Renderer.SetPropertyBlock(null);
        piece.Renderer.sharedMaterial = null;
        MeshFilter meshFilter = piece.GameObject.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            meshFilter.sharedMesh.Clear();
        }

        piece.GameObject.transform.SetParent(null, false);
        piece.GameObject.SetActive(false);
        if (PiecePool.Count < MaxPooledPieceCount)
        {
            PiecePool.Push(piece.GameObject);
            return;
        }

        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            UnityEngine.Object.Destroy(meshFilter.sharedMesh);
        }
        UnityEngine.Object.Destroy(piece.GameObject);
    }

    /// <summary>
    /// 按 Shader、角色纹理、冰冻纹理和原颜色缓存共享碎片材质，避免每块碎片分别创建材质实例。
    /// </summary>
    private static Material GetSharedShardMaterial(Material sourceMaterial, Shader shardShader)
    {
        Texture sourceTexture = null;
        Texture iceTexture = null;
        Color sourceColor = Color.white;
        Color lightColor = Color.white;
        float lightScale = 1.2f;
        float halfLambertValue = 0.5f;
        if (sourceMaterial != null)
        {
            if (sourceMaterial.HasProperty("_MainTex"))
            {
                sourceTexture = sourceMaterial.GetTexture("_MainTex");
            }
            else if (sourceMaterial.HasProperty("_BaseMap"))
            {
                sourceTexture = sourceMaterial.GetTexture("_BaseMap");
            }

            if (sourceMaterial.HasProperty("_Color"))
            {
                sourceColor = sourceMaterial.GetColor("_Color");
            }

            if (sourceMaterial.HasProperty("_ICETex"))
            {
                iceTexture = sourceMaterial.GetTexture("_ICETex");
            }

            if (sourceMaterial.HasProperty("_LightColor"))
            {
                lightColor = sourceMaterial.GetColor("_LightColor");
            }

            if (sourceMaterial.HasProperty("_LightScale"))
            {
                lightScale = sourceMaterial.GetFloat("_LightScale");
            }

            if (sourceMaterial.HasProperty("_HalfLambertValue"))
            {
                halfLambertValue = sourceMaterial.GetFloat("_HalfLambertValue");
            }
        }

        MaterialCacheKey cacheKey = new MaterialCacheKey(shardShader, sourceTexture, iceTexture, sourceColor);
        if (MaterialCache.TryGetValue(cacheKey, out Material material) && material != null)
        {
            return material;
        }

        material = new Material(shardShader);
        // 开启 GPU Instancing，使相同碎片网格和材质可以使用实例化绘制。
        material.enableInstancing = true;
        material.name = "IceShard_Shared_" + cacheKey.TextureId;
        if (sourceTexture != null && material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", sourceTexture);
        }
        if (iceTexture != null && material.HasProperty("_ICETex"))
        {
            material.SetTexture("_ICETex", iceTexture);
        }
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", sourceColor);
        }
        if (material.HasProperty("_LightColor"))
        {
            material.SetColor("_LightColor", lightColor);
        }
        if (material.HasProperty("_LightScale"))
        {
            material.SetFloat("_LightScale", lightScale);
        }
        if (material.HasProperty("_HalfLambertValue"))
        {
            material.SetFloat("_HalfLambertValue", halfLambertValue);
        }
        MaterialCache[cacheKey] = material;
        return material;
    }

    /// <summary>
    /// 共享碎片材质的缓存键，使用 Shader、纹理和颜色区分不同角色材质。
    /// </summary>
    private readonly struct MaterialCacheKey : IEquatable<MaterialCacheKey>
    {
        public readonly int ShaderId;
        public readonly int TextureId;
        public readonly int IceTextureId;
        public readonly Color Color;

        /// <summary>兼容旧的材质缓存键创建方式。</summary>
        public MaterialCacheKey(Shader shader, Texture texture, Color color)
            : this(shader, texture, null, color)
        {
        }

        /// <summary>
        /// 创建共享碎片材质缓存键。
        /// </summary>
        public MaterialCacheKey(Shader shader, Texture texture, Texture iceTexture, Color color)
        {
            ShaderId = shader.GetInstanceID();
            TextureId = texture != null ? texture.GetInstanceID() : 0;
            IceTextureId = iceTexture != null ? iceTexture.GetInstanceID() : 0;
            Color = color;
        }

        /// <summary>
        /// 判断两个缓存键是否对应同一份材质参数。
        /// </summary>
        public bool Equals(MaterialCacheKey other)
        {
            return ShaderId == other.ShaderId
                && TextureId == other.TextureId
                && IceTextureId == other.IceTextureId
                && Color.Equals(other.Color);
        }

        /// <summary>
        /// 判断对象是否为相同材质缓存键。
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is MaterialCacheKey other && Equals(other);
        }

        /// <summary>
        /// 生成材质缓存键哈希值。
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ShaderId;
                hashCode = (hashCode * 397) ^ TextureId;
                hashCode = (hashCode * 397) ^ IceTextureId;
                hashCode = (hashCode * 397) ^ Color.GetHashCode();
                return hashCode;
            }
        }
    }
}

/// <summary>
/// 单个冰冻碎片的对象、渲染器和运行时材质引用，供 SplitDieComponent 驱动与清理。
/// </summary>
public sealed class MeshSplitPiece
{
    public readonly GameObject GameObject;
    public readonly Renderer Renderer;
    public readonly Material Material;

    /// <summary>
    /// 保存碎片运行时对象及其材质引用。
    /// </summary>
    public MeshSplitPiece(GameObject gameObject, Renderer renderer, Material material)
    {
        GameObject = gameObject;
        Renderer = renderer;
        Material = material;
    }
}
