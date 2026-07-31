
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class AnimMapBakerWindow : EditorWindow {

    private enum SaveStrategy
    {
        // Only anim map
        AnimMap, 
        // With shader
        Mat, 
        // Prefab with mat
        Prefab 
    }

    #region FIELDS

    private const string BuiltInShader = "chenjd/BuiltIn/AnimMapShader";
    private const string URPShader = "chenjd/URP/AnimMapShader";
    private const string ShadowShader = "chenjd/BuiltIn/AnimMapWithShadowShader";
    private static List<GameObject> _targetGoList = new List<GameObject>() {};
    private static GameObject _targetGo;
    private static AnimMapBaker _baker;
    private static string _exportFolderParentPath = "Assets/AnimMapBaker";
    private static string _prefabFolderName = "Prefab";
    private static string _animTexFolderName = "AnimTexture";
    private static string _mainTexFolderName = "MainTexture";
    private static string _matFolderName = "Material";
    private static string _meshFolderName = "Mesh";

    private static string _subPath = "";
    private static string _prefabFolderPath => _subPath + "/" + _prefabFolderName;
    private static string _animTexFolderPath => _subPath + "/" + _animTexFolderName;
    private static string _mainTexFolderPath => _subPath + "/" + _mainTexFolderName;
    private static string _matFolderPath => _subPath + "/" + _matFolderName;
    private static string _meshFolderPath => _subPath + "/" + _meshFolderName;

    private static Shader _animMapShader;
    private static Shader _prevAnimMapShader;
    private static readonly int MainTexID = Shader.PropertyToID("_MainTex");
    private static readonly int AnimMapID = Shader.PropertyToID("_AnimMap");
    private static readonly int AnimLenID = Shader.PropertyToID("_AnimLen");
    private bool _isShadowEnabled = false;

    #endregion


    #region  METHODS

    [MenuItem("Window/AnimMapBaker")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(AnimMapBakerWindow));
        _baker = new AnimMapBaker();
    }

   

    private void OnGUI()
    {
        if (GUILayout.Button("Add"))
        {
            _targetGoList.Add(null);
        }
        if (GUILayout.Button("Del"))
        {
            _targetGoList.RemoveAt(_targetGoList.Count - 1);
        }
        for (int i = 0; i < _targetGoList.Count; i++)
            {
                _targetGoList[i] = (GameObject)EditorGUILayout.ObjectField(_targetGoList[i], typeof(GameObject), true);
            }

            //_targetGo = (GameObject)EditorGUILayout.ObjectField(_targetGo, typeof(GameObject), true);
           
            //EditorGUILayout.LabelField(string.Format($"Output Path: {_subPath}"));
            //_subPath = EditorGUILayout.TextField(_subPath);

            //_isShadowEnabled = EditorGUILayout.Toggle("Enable Shadow", _isShadowEnabled);

            //if(_isShadowEnabled)
            //{
            //    var style = new GUIStyle(EditorStyles.label);
            //    style.normal.textColor = Color.yellow;

            //    EditorGUILayout.LabelField("Warning: Enabling shadows will cause additional draw calls to draw shadows.", style);

            //    _prevAnimMapShader = _animMapShader;
            //    _animMapShader = Shader.Find(ShadowShader);
            //}
            //else if(_prevAnimMapShader != null)
            //{
            //    _animMapShader = _prevAnimMapShader;
            //}
           
        if (!GUILayout.Button("Bake")) return;

        for (int i = 0; i < _targetGoList.Count; i++)
        {
            _targetGo = _targetGoList[i];
            if (_targetGo == null)
            {
                //EditorUtility.DisplayDialog("错误", "请选择一个GameObject！", "确认");
                continue;
            }
            _subPath = _targetGo == null ? _subPath : _exportFolderParentPath + "/" + _targetGo.name;
            // 重置需要烘焙的角色
            _targetGo.transform.position = Vector3.zero;
            _targetGo.transform.localScale = Vector3.one;
            _targetGo.transform.eulerAngles = Vector3.zero;

#if UNITY_6000_0_OR_NEWER
            var renderPipelineAsset = GraphicsSettings.defaultRenderPipeline;
#else
            var renderPipelineAsset = GraphicsSettings.renderPipelineAsset;
#endif
            var shaderName = renderPipelineAsset != null ? URPShader : BuiltInShader;
            _animMapShader = Shader.Find(shaderName);

            // 存在之前的则删除
            if (Directory.Exists(_subPath))
            {
                Directory.Delete(_subPath, true);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            createDirectory();

            // 创建一个烘焙器
            if (_baker == null)
            {
                _baker = new AnimMapBaker();
            }
            // 设置目标
            _baker.SetAnimData(_targetGo);

            var list = _baker.Bake();

            if (list == null) return;
            string _targetGoName = _targetGo.name;

            var data_first = list[0];
            Mesh mesh = SaveMesh(ref data_first, _targetGoName);

            Dictionary<EActionType, MatInfo> matDic = new Dictionary<EActionType, MatInfo>();

            foreach (var t in list)
            {
                var data = t;
                float animLength = data.AnimLen;
                EActionType actionType = AnimMapBaker.GetActionTypeByAnimName(data.Name);
                if (!matDic.ContainsKey(actionType))
                {
                    MatInfo matInfo = new MatInfo() { AnimLen = animLength, Mat = SaveMat(ref data, _targetGo.name) };
                    matDic.Add(actionType, matInfo);
                }
            }
            SavePrefab(mesh, _targetGo.name, matDic);
            _baker = null;
        }
    }

   
    private static void SavePrefab(Mesh mesh, string preafabName, Dictionary<EActionType, MatInfo> matDic)
    {
        var go = new GameObject();
        go.AddComponent<MeshRenderer>().sharedMaterial = matDic[EActionType.wait].Mat;                                                        
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        ActionFlow actionFlow = go.AddComponent<ActionFlow>();
        foreach(var v in matDic)
        {
            var key = v.Key;
            var value = v.Value;
            actionFlow.SetActionData(key, value.AnimLen, value.Mat);
        }   
        string preafbPath = _prefabFolderPath + "/" + preafabName + ".prefab";
        string resourcesPath =  "Assets/" + "Resources/Soliders/" + preafabName + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(go, preafbPath);
        PrefabUtility.SaveAsPrefabAsset(go, resourcesPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        GameObject.DestroyImmediate(go);
    }

    private Mesh SaveMesh(ref BakedData data, string meshName)
    {
        string meshFilePath = _meshFolderPath + "/" + meshName + ".mesh";
        Mesh mesh = null;
        AssetDatabase.CreateAsset(data.MeshData, meshFilePath);
        mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshFilePath);
        return mesh;
    }

    private static Material SaveMat(ref BakedData data, string name)
    {
        if(_animMapShader == null)
        {
            EditorUtility.DisplayDialog("err", "shader is null!!", "OK");
            return null;
        }

        if(_targetGo == null || !_targetGo.GetComponentInChildren<SkinnedMeshRenderer>())
        {
            EditorUtility.DisplayDialog("err", "SkinnedMeshRender is null!!", "OK");
            return null;
        } 

        var smr = _targetGo.GetComponentInChildren<SkinnedMeshRenderer>();
        var mat = new Material(_animMapShader);
        var animTex = SaveAnimTex(ref data);
        string mainTexPath = _mainTexFolderPath + "/" + name + ".png";
        var mainTex = AssetDatabase.LoadAssetAtPath<Texture2D>(mainTexPath);
        mat.SetTexture(MainTexID, mainTex);
        mat.SetTexture(AnimMapID, animTex);
        mat.SetFloat(AnimLenID, data.AnimLen);

        string matPath = _matFolderPath + "/" + data.Name + ".mat";
        AssetDatabase.CreateAsset(mat, matPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return mat;
    }

    // 保存动作图片
    private static Texture2D SaveAnimTex(ref BakedData data)
    {
        string animTexPath = _animTexFolderPath + "/" + data.Name + ".asset";
        var animTex = new Texture2D(data.AnimMapWidth, data.AnimMapHeight, TextureFormat.RGBAHalf, false);
        animTex.LoadRawTextureData(data.RawAnimMap);
        AssetDatabase.CreateAsset(animTex, animTexPath);
        AssetDatabase.Refresh();
        return animTex;
    }

    public static void SaveMainTex(Texture2D tex, string texName)
    {
        Texture2D readableTex = new Texture2D(
            tex.width,
            tex.height,
            TextureFormat.RGBA32,
            false
        );
        readableTex.SetPixels(tex.GetPixels());
        readableTex.Apply();       
        byte[] bytes = readableTex.EncodeToPNG();
        string mainTexPath = _mainTexFolderPath + "/" + texName + ".png";
        File.WriteAllBytes(mainTexPath, bytes);
        AssetDatabase.ImportAsset(mainTexPath);
        AssetDatabase.Refresh();
    }

    private static void createDirectory()
    {
        if (!Directory.Exists(_exportFolderParentPath))
            Directory.CreateDirectory(_exportFolderParentPath);
        if (!Directory.Exists(_prefabFolderPath))
            Directory.CreateDirectory(_prefabFolderPath);
        if (!Directory.Exists(_animTexFolderPath))
            Directory.CreateDirectory(_animTexFolderPath);
        if (!Directory.Exists(_mainTexFolderPath))
            Directory.CreateDirectory(_mainTexFolderPath);
        if (!Directory.Exists(_matFolderPath))
            Directory.CreateDirectory(_matFolderPath);
        if (!Directory.Exists(_meshFolderPath))
            Directory.CreateDirectory(_meshFolderPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    #endregion
}

class MatInfo
{
    public float AnimLen;
    public Material Mat;
}
