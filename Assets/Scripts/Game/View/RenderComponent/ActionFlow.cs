using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 动作组件
/// </summary>
[System.Serializable]
[ExecuteInEditMode]
public class ActionFlow : MonoBehaviour
{
    private RenderComponent renderComponenter;
    protected RenderComponent RenderComponenter
    {
        get
        {
            renderComponenter = gameObject.GetOrAddComponentInChild<RenderComponent>();
            return renderComponenter;
        }
    }

    private Animator animator;
    protected Animator AnimatorC
    {
        get
        {
            if(animator == null)
            {
                animator = gameObject.GetComponentInChildren<Animator>();
            }
            return animator;
        }
    }

    // 当前动画时间
    public float CurrentAnimTime;
    // 动画播放速率
    private float animPlaySpeedValue = 1;
    // 当前动作开始计算时间的时间点
    private float animationTimeReference;
    // 当前动作是否循环播放
    private bool animationLoop;
    // 当前动作是否冻结在指定帧
    private bool animationPaused;

    // 当前播放的动画
    private ActionData currentActionData;
    public ActionData CurrentActionData => currentActionData;

    // 动作信息
    public List<ActionData> ActionDataList = new List<ActionData>();
    // 动作信息map，空间换查询时间
    public Dictionary<EActionType, ActionData> ActionDataDic = new Dictionary<EActionType, ActionData>();

    // Start is called before the first frame update
    void Awake()
    {
        initActionDataDic();
        PlayAction(EActionType.run);
    }

    private void Start()
    {
    }
    
    // 设置动画信息
    public void SetActionData(EActionType actionType, float animLen, Material mat)
    {
        ActionData _data = new ActionData() { ActionType = actionType, AnimLen = animLen, Mat = mat };
        ActionDataList.Add(_data);
    }

    // 清空动画信息
    public void ClearActionData()
    {
        ActionDataDic.Clear();
        ActionDataList.Clear();
    }

    // 播放一个动画
    public void PlayAction(EActionType _actionType)
    {
        if (currentActionData != null && currentActionData.ActionType == _actionType)
            return;
        if(ActionDataDic != null && ActionDataDic.ContainsKey(_actionType))
        {
            ActionData _actionData = ActionDataDic[_actionType];        
            currentActionData = _actionData;
            if (_actionData.Mat == null)
            {
                animatorPlay(_actionType.ToString());
                return;
            }
            else
            {
                RenderComponenter.SetMat(_actionData.Mat);
            }
            setActionAnimLenth(currentActionData);
            // 死亡动作只播放一次并停在末帧，其他动作继续循环播放。
            animationLoop = _actionType != EActionType.die;
            animationPaused = false;
            animationTimeReference = Time.time;
            RenderComponenter.SetPropertyBlockFloat("_AnimationLoop", animationLoop ? 1f : 0f);
            RenderComponenter.SetPropertyBlockFloat("_AnimationPaused", 0f);
            if (_actionType == EActionType.run)
            {
                CurrentAnimTime = Random.Range(0, 1f);
                RenderComponenter.SetPropertyBlockFloat("_CurrentTime", GetShaderStartTime());
                RenderComponenter.SetPropertyBlockFloat("_TimeSinceLevelLoad", Time.time);
            }
            else
            {
                CurrentAnimTime = 0;
                RenderComponenter.SetPropertyBlockFloat("_CurrentTime", GetShaderStartTime());
                RenderComponenter.SetPropertyBlockFloat("_TimeSinceLevelLoad", Time.time);
            }          
        }
    }

    /// <summary>
    /// 按配置中的动作名称播放动作。
    /// </summary>
    public void PlayAction(string actionType)
    {
        if (!string.IsNullOrWhiteSpace(actionType)
            && System.Enum.TryParse(actionType, true, out EActionType parsedActionType))
        {
            PlayAction(parsedActionType);
            return;
        }

        PlayAction(EActionType.skill);
    }

    // 获取动作时长
    public float GetAnimLen(EActionType _actionType)
    {
        if(ActionDataDic != null && ActionDataDic.ContainsKey(_actionType))
        {
            ActionData _actionData = ActionDataDic[_actionType];
            return _actionData.AnimLen;
        }
        return 1;
    }

    /// <summary>
    /// 使用单位配置覆盖指定动作的动画时长，并同步当前正在播放的动作参数。
    /// </summary>
    public void SetActionAnimationLength(EActionType actionType, float animationLength)
    {
        if (animationLength <= 0f || ActionDataDic == null || !ActionDataDic.ContainsKey(actionType))
        {
            return;
        }

        ActionData actionData = ActionDataDic[actionType];
        actionData.AnimLen = animationLength;
        if (currentActionData != null && currentActionData.ActionType == actionType && !animationPaused)
        {
            setActionAnimLenth(currentActionData);
        }
    }
    /// <summary>设置当前动作播放速度，负数会按零处理。</summary>
    public void SetAnimationSpeed(float speedRatio)
    {
        if (currentActionData != null && !animationPaused)
        {
            CurrentAnimTime = GetCurrentAnimationTime();
        }

        animPlaySpeedValue = Mathf.Max(0f, speedRatio);
        animationTimeReference = Time.time;
        if (currentActionData != null)
        {
            if (animationPaused)
            {
                setPausedAnimationFrame();
            }
            else
            {
                setActionAnimLenth(currentActionData);
                RenderComponenter.SetPropertyBlockFloat("_CurrentTime", GetShaderStartTime());
                RenderComponenter.SetPropertyBlockFloat("_TimeSinceLevelLoad", animationTimeReference);
            }
        }
    }

    /// <summary>
    /// 将当前动作冻结在指定帧，供卡刀等表现逻辑使用。
    /// </summary>
    public void FreezeAnimationAt(float animationTime)
    {
        if (currentActionData == null)
        {
            return;
        }

        float animationLength = Mathf.Max(0.0001f, currentActionData.AnimLen);
        CurrentAnimTime = Mathf.Clamp(animationTime, 0f, animationLength);
        animationPaused = true;
        setPausedAnimationFrame();
    }

    /// <summary>计算当前动作在真实动画时间轴上的帧位置。</summary>
    private float GetCurrentAnimationTime()
    {
        if (currentActionData == null || animationPaused)
        {
            return CurrentAnimTime;
        }

        float animationLength = Mathf.Max(0.0001f, currentActionData.AnimLen);
        float elapsedTime = Mathf.Max(0f, Time.time - animationTimeReference) * animPlaySpeedValue;
        float animationTime = CurrentAnimTime + elapsedTime;
        return animationLoop ? Mathf.Repeat(animationTime, animationLength) : Mathf.Min(animationTime, animationLength);
    }

    /// <summary>冻结当前正在显示的动作帧，供麻痹和冰冻效果使用。</summary>
    public void FreezeCurrentAnimation()
    {
        if (currentActionData == null)
        {
            return;
        }

        CurrentAnimTime = GetCurrentAnimationTime();
        animationPaused = true;
        setPausedAnimationFrame();
    }

    /// <summary>解除动作冻结并从冻结帧继续播放。</summary>
    public void ResumeAnimation()
    {
        if (currentActionData == null || !animationPaused)
        {
            return;
        }

        animationPaused = false;
        animationTimeReference = Time.time;
        setActionAnimLenth(currentActionData);
        RenderComponenter.SetPropertyBlockFloat("_CurrentTime", GetShaderStartTime());
        RenderComponenter.SetPropertyBlockFloat("_TimeSinceLevelLoad", animationTimeReference);
        RenderComponenter.SetPropertyBlockFloat("_AnimationPaused", 0f);
    }

    /// <summary>把当前帧写入材质属性，并通知 Shader 停止使用全局时间推进。</summary>
    private void setPausedAnimationFrame()
    {
        float animationLength = Mathf.Max(0.0001f, currentActionData.AnimLen);
        RenderComponenter.SetPropertyBlockFloat("_AnimLen", animationLength);
        RenderComponenter.SetPropertyBlockFloat("_CurrentTime", CurrentAnimTime);
        RenderComponenter.SetPropertyBlockFloat("_TimeSinceLevelLoad", Time.time);
        RenderComponenter.SetPropertyBlockFloat("_AnimationPaused", 1f);
    }

    /// <summary>将原始动画时间换算成当前倍率下的起始时间，保证变速前后的帧位置一致。</summary>
    private float GetShaderStartTime()
    {
        float safeSpeedRatio = animPlaySpeedValue > 0f ? animPlaySpeedValue : 0.0001f;
        return CurrentAnimTime / safeSpeedRatio;
    }

    // 设置动画时长
    private void setActionAnimLenth(ActionData _actionData)
    {
        // Shader 使用时间除以动画长度，速度倍率越大，动画长度需要越短。
        float safeSpeedRatio = animPlaySpeedValue > 0f ? animPlaySpeedValue : 0.0001f;
        float _animLenth = _actionData.AnimLen / safeSpeedRatio;
        RenderComponenter.SetPropertyBlockFloat("_AnimLen", _animLenth);
    }

    private void initActionDataDic()
    {
        for(int i = 0; i < ActionDataList.Count; i++)
        {
            if(!ActionDataDic.ContainsKey(ActionDataList[i].ActionType))
                ActionDataDic.Add(ActionDataList[i].ActionType, ActionDataList[i]);           
        }
    }

    private void animatorPlay(string _stateName)
    {
        if(AnimatorC != null)
            AnimatorC.CrossFade(_stateName,0.05f);
    }

}

// 动画信息
[System.Serializable]
public class ActionData
{
    public Material Mat;
    public float AnimLen;
    public EActionType ActionType = EActionType.None;
}

[System.Serializable]
public enum EActionType
{
    None,
    run,
    attack,
    die,
    wait,
    skill,
    show,
    victory,
}
