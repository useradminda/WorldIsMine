/*
 * Created by jiadong chen
 * https://jiadong-chen.medium.com/
 * 用来烘焙动作贴图。烘焙对象使用Animation组件，并且在导入时设置Rig为Legacy
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using Unity.VisualScripting;

/// <summary>
/// 保存需要烘焙的动画的相关数据
/// </summary>
public struct AnimData
{
    #region FIELDS

    private int _vertexCount;
    private int _mapWidth;
    private readonly List<AnimationState> _animClips;
    private readonly List<AnimationState> _horseClips;
    private string _name;

    private Animation _animation;
    private Animation _horseAnim;
    private SkinnedMeshRenderer _skin;
    private SkinnedMeshRenderer _horseSkin;

    public List<MeshFilter> MeshList;    ////
    public List<AnimationState> AnimationClips => _animClips;
    public List<AnimationState> HorseAnimClips => _horseClips;
    public int MapWidth => _mapWidth;
    public string Name => _name;

    #endregion

    public AnimData(Animation anim, Animation horseAnim, SkinnedMeshRenderer smr, SkinnedMeshRenderer horseSkin, string goName, GameObject _gob)
    {
        _vertexCount = smr.sharedMesh.vertexCount;      
        _mapWidth = Mathf.NextPowerOfTwo(_vertexCount);
        _animClips = new List<AnimationState>(anim.Cast<AnimationState>());
        _horseClips = horseAnim != null ? new List<AnimationState>(horseAnim.Cast<AnimationState>()) : null;
        _animation = anim;
        _horseAnim = horseAnim;
        _skin = smr;
        _horseSkin = horseSkin;
        _name = goName;


        MeshList = new List<MeshFilter>();   
        _vertexCount = 0;
        RecursivelyPrintTransformNames(_gob.transform);
        for(int i = 0; i < MeshList.Count; i++)
        {
            _vertexCount += MeshList[i].sharedMesh.vertexCount;
        }
        _vertexCount += smr.sharedMesh.vertexCount;
        if (horseSkin != null)
            _vertexCount += _horseSkin.sharedMesh.vertexCount;
        _mapWidth = Mathf.NextPowerOfTwo(_vertexCount);
    }

    #region METHODS

    public void AnimationPlay(string animName)
    {
        _animation.Play(animName);
        if (_horseAnim && AnimMapBaker.ManAnimToHorseDic.ContainsKey(animName))
        {
            string horseAnimName = AnimMapBaker.ManAnimToHorseDic[animName];
            _horseAnim.Play(horseAnimName);
        }
    }

    public void SampleAnimAndBakeMesh(ref Mesh m, bool needCombineTex, Rect[] rectArray)
    {
        SampleAnim();
        BakeMesh(ref m, needCombineTex, rectArray);
    }

    private void SampleAnim()
    {
        if (_animation == null)
        {
            Debug.LogError("animation is null!!");
            return;
        }

        _animation.Sample();
        if (_horseAnim != null)
        {
            _horseAnim.Sample();
        }
    }

    private void BakeMesh(ref Mesh m, bool needCombineTex, Rect[] rectArray)
    {
        if (_skin == null)
        {
            Debug.LogError("skin is null!!");
            return;
        }

        List<CombineInstance> combineInstances = new List<CombineInstance>(); ////   

        Mesh _humanMesh = new Mesh();       ////
        _skin.BakeMesh(_humanMesh);         ////
        if (needCombineTex)
        {
            _humanMesh.uv = RemapUV(_humanMesh.uv, rectArray[0]);
        }
        combineInstances.Add(new CombineInstance()
        {
            mesh = _humanMesh,//_skin.sharedMesh,
            transform = _skin.transform.localToWorldMatrix
        });

        if(_horseSkin != null)
        {
            Mesh _horseMesh = new Mesh();
            _horseSkin.BakeMesh(_horseMesh);

            if (needCombineTex)
            {
                _horseMesh.uv = RemapUV(_horseMesh.uv, rectArray[1]);
            }

            combineInstances.Add(new CombineInstance()
            {
                mesh = _horseMesh,//_skin.sharedMesh,
                transform = _horseSkin.transform.localToWorldMatrix
            });
        }

        for (int i = 0; i < MeshList.Count;  i++)
        {
            combineInstances.Add(new CombineInstance()
            {
                mesh = MeshList[i].sharedMesh,
                transform = MeshList[i].transform.localToWorldMatrix
            });
        }
        
        m.CombineMeshes(combineInstances.ToArray(), true);
    }

    public void RecursivelyPrintTransformNames(Transform parent)
    {
        if(parent.gameObject.activeInHierarchy == true && parent.GetComponent<MeshFilter>() != null)
        {
            MeshList.Add(parent.GetComponent<MeshFilter>());
            
        }    
        foreach (Transform child in parent)
        {
            RecursivelyPrintTransformNames(child);
        }
    }

    /// <summary>
    /// UV重映射
    /// </summary>
    private static Vector2[] RemapUV(
        Vector2[] uvs,
        Rect rect)
    {
        Vector2[] result =
            new Vector2[uvs.Length];

        for (int i = 0; i < uvs.Length; i++)
        {
            result[i] = new Vector2(
                rect.x + uvs[i].x * rect.width,
                rect.y + uvs[i].y * rect.height
            );
        }

        return result;
    }

    public AnimationState GetHorseAnimationState(AnimationState heroState)
    {
        if (HorseAnimClips == null)
        {
            return null;
        }
        else
        {
            if(HorseAnimClips.Count > 0)
            {
                var animName = heroState.name;
                if(AnimMapBaker.ManAnimToHorseDic.ContainsKey(animName))
                {
                    var horseAnimName = AnimMapBaker.ManAnimToHorseDic[animName];
                    for(int i = 0; i < HorseAnimClips.Count; i++)
                    {
                        if(horseAnimName == HorseAnimClips[i].name)
                        {
                            return HorseAnimClips[i];
                        }
                    }
                }
            }
        }
        return null;
    }

    #endregion


}

/// <summary>
/// 烘焙后的数据
/// </summary>
public struct BakedData
{
    #region FIELDS

    private readonly string _name;
    private readonly float _animLen;
    private readonly byte[] _rawAnimMap;
    private readonly int _animMapWidth;
    private readonly int _animMapHeight;
    public EActionType ActionType;

    private Mesh _mesh;    ////
  
    #endregion

    public BakedData(EActionType actionType, string name, float animLen, Texture2D animMap, Mesh mesh) //// (string name, float animLen, Texture2D animMap)
    {
        _name = name;
        _animLen = animLen;
        _animMapHeight = animMap.height;
        _animMapWidth = animMap.width;
        _rawAnimMap = animMap.GetRawTextureData();
        _mesh = mesh;
        ActionType = actionType;
    }

    public int AnimMapWidth => _animMapWidth;

    public string Name => _name;

    public float AnimLen => _animLen;

    public byte[] RawAnimMap => _rawAnimMap;

    public int AnimMapHeight => _animMapHeight;

    public Mesh MeshData => _mesh; ////
}

/// <summary>
/// 烘焙器
/// </summary>
public class AnimMapBaker{

    #region FIELDS

    private AnimData? _animData = null;
    private Mesh _bakedMesh;
    private readonly List<BakedData> _bakedDataList = new List<BakedData>();
    private bool _needCombineTex = false;
    private Rect[] texRectArray;

    public static Dictionary<string, string> ManAnimToHorseDic = new Dictionary<string, string>() {
        { "run",  "run" },
        { "wait", "wait" },
        { "die",  "die" },
        { "attack", "attack" },
        { "skill", "wait" },
        { "show", "wait" },
    };

    #endregion

    #region METHODS

    public void SetAnimData(GameObject go)
    {
        if(go == null)
        {
            Debug.LogError("go is null!!");
            return;
        }

        var anim = go.GetComponent<Animation>();
        Animation horseAnim = getHorseAnim(go);

        var smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
        SkinnedMeshRenderer horseSkin = getHorseSkinnedMeshRenderer(go);      

        if(anim == null || smr == null)
        {
            Debug.LogError("anim or smr is null!!");
            return;
        }
        _bakedMesh = new Mesh();
        _animData = new AnimData(anim, horseAnim, smr, horseSkin, go.name, go);  ////(anim, smr, go.name)
        Texture2D mainTex = GetBeCopyMainTex(smr, horseSkin);
        AnimMapBakerWindow.SaveMainTex(mainTex, go.name);
    }

    public List<BakedData> Bake()
    {
        if(_animData == null)
        {
            Debug.LogError("bake data is null!!");
            return _bakedDataList;
        }

        //每一个动作都生成一个动作图
        foreach (var t in _animData.Value.AnimationClips)
        {
            if(!t.clip.legacy)
            {
                Debug.LogError(string.Format($"{t.clip.name} is not legacy!!"));
                continue;
            }

            BakePerAnimClip(t, _animData.Value.GetHorseAnimationState(t));
        }

        return _bakedDataList;
    }

    private void BakePerAnimClip(AnimationState curAnim, AnimationState horseAnim)
    {
        var curClipFrame = 0;
        float sampleTime = 0;
        float perFrameTime = 0;

        curClipFrame = Mathf.ClosestPowerOfTwo((int)(curAnim.clip.frameRate * curAnim.length));
        perFrameTime = curAnim.length / curClipFrame; ;
        _animData.Value.AnimationPlay(curAnim.name);

        var animTex = new Texture2D(_animData.Value.MapWidth, curClipFrame, TextureFormat.RGBAHalf, true);
        for (var i = 0; i < curClipFrame; i++)
        {
            curAnim.time = sampleTime;
            if (horseAnim != null)
                horseAnim.time = sampleTime;

            _animData.Value.SampleAnimAndBakeMesh(ref _bakedMesh, _needCombineTex, texRectArray);

            for(var j = 0; j < _bakedMesh.vertexCount; j++)
            {
                var vertex = _bakedMesh.vertices[j];
                animTex.SetPixel(j, i, new Color(vertex.x, vertex.y, vertex.z));
            }

            sampleTime += perFrameTime;
        }
        animTex.Apply();

        _bakedDataList.Add(new BakedData(GetActionTypeByAnimName(curAnim.name), curAnim.name, curAnim.clip.length, animTex, _bakedMesh));  ////
    }

    private SkinnedMeshRenderer getHorseSkinnedMeshRenderer(GameObject go)
    {
        SkinnedMeshRenderer horseSkin = null;
        for (int i = 0; i < go.transform.childCount; i++)
        {
            if (go.transform.GetChild(i).name.Contains("Horse"))
            {
                if (go.transform.GetChild(i).gameObject.activeInHierarchy == false)
                    continue;
                horseSkin = go.transform.GetChild(i).GetComponentInChildren<SkinnedMeshRenderer>(false);
                if (horseSkin == null)
                    continue;
                break;
            }
        }
        return horseSkin;
    }

    private Animation getHorseAnim(GameObject go)
    {
        Animation horseAnim = null;
        for (int i = 0; i < go.transform.childCount; i++)
        {
            if (go.transform.GetChild(i).name.Contains("Horse"))
            {
                if (go.transform.GetChild(i).gameObject.activeInHierarchy == false)
                    continue;
                horseAnim = go.transform.GetChild(i).GetComponent<Animation>();
                if (horseAnim == null)
                    continue;
                break;
            }
        }
        return horseAnim;
    }

    public static EActionType GetActionTypeByAnimName(string animName)
    {
        // 空值直接返回None
        if (string.IsNullOrEmpty(animName))
            return EActionType.None;

        // 遍历所有枚举值
        foreach (EActionType actionType in System.Enum.GetValues(typeof(EActionType)))
        {        
            if (actionType == EActionType.None)
                continue;
            string enumStr = actionType.ToString();
            if (animName.ToLower().Contains(enumStr.ToLower()))
            {
                return actionType;
            }
        }
        return EActionType.None;
    }

    public Texture2D GetBeCopyMainTex(SkinnedMeshRenderer smr, SkinnedMeshRenderer horseSkin)
    {
        if (horseSkin == null)
        {
            _needCombineTex = false;
            Texture2D oriTex = (Texture2D)smr.sharedMaterial.mainTexture;
            return oriTex;
        }
        _needCombineTex = smr.sharedMaterial.mainTexture != horseSkin.sharedMaterial.mainTexture ? true : false;
        if (_needCombineTex == false)
        {
            Texture2D oriTex = (Texture2D)smr.sharedMaterial.mainTexture;
            return oriTex;
        }
   
        Texture2D heroTex = (Texture2D)smr.sharedMaterial.mainTexture;
       
        Texture2D horseTex = (Texture2D)horseSkin.sharedMaterial.mainTexture;

        //int maxSize = Mathf.Max(heroTex.width, horseTex.width);
        if (_needCombineTex)
        {
            Texture2D atlas = new Texture2D(2048, 2048, TextureFormat.BGRA32, false, true);
            atlas.wrapMode = TextureWrapMode.Clamp;
            texRectArray = atlas.PackTextures(
                new Texture2D[]
                {
                    heroTex,
                    horseTex
                },
                4,
                2048
            );
            return atlas;
        }
        return (Texture2D)smr.sharedMaterial.mainTexture;
    }

    #endregion

}
