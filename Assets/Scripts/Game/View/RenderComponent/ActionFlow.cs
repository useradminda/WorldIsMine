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
            renderComponenter = gameObject.GetOrAddComponent<RenderComponent>();
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
        PlayAction(EActionType.wait);
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
            if (_actionType == EActionType.run)
            {
                CurrentAnimTime = Random.Range(0, 1f);
                RenderComponenter.SetPropertyBlockFloat("_CurrentTime", CurrentAnimTime);
                RenderComponenter.SetPropertyBlockFloat("_TimeSinceLevelLoad", Time.time);
            }
            else
            {
                CurrentAnimTime = 0;
                RenderComponenter.SetPropertyBlockFloat("_CurrentTime", CurrentAnimTime);
                RenderComponenter.SetPropertyBlockFloat("_TimeSinceLevelLoad", Time.time);
            }          
        }
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

    // 进入冰冻
    public void ActionFreeze(float speedValue)
    {
        animPlaySpeedValue = 1f / speedValue;
        setActionAnimLenth(currentActionData);
    }

    // 退出冰冻
    public void ActionExitFreeze()
    {
        animPlaySpeedValue = 1;
        setActionAnimLenth(currentActionData);
    }

    // 设置动画时长
    private void setActionAnimLenth(ActionData _actionData)
    {
        float _animLenth = _actionData.AnimLen * animPlaySpeedValue;
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