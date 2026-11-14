using Nebukam.ORCA;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
/// <summary>冰冻表现组件，用于冻结单位动作帧并切换冰冻材质效果。</summary>
public class FreezeComponent : MonoBehaviour
{
    private RenderComponent renderComponenter;
    protected RenderComponent RenderComponenter
    {
        get
        {
            if (renderComponenter == null)
                renderComponenter = gameObject.GetOrAddComponentInChild<RenderComponent>();
            return renderComponenter;
        }
    }
    private ActionFlow actionFlower;
    protected ActionFlow mActionFlow
    {
        get
        {
            if(actionFlower == null)
                actionFlower = gameObject.GetOrAddComponentInChild<ActionFlow>();
            return actionFlower;
        }
    }

    /// <summary>进入冰冻状态，记录当前动作帧并暂停动画时间轴。</summary>
    public void SetFreeze(float ratio)
    {
        mActionFlow.SetAnimationSpeed(ratio);
        if (ratio <= 0f)
        {
            mActionFlow.FreezeCurrentAnimation();
        }
        RenderComponenter.SetPropertyBlockFloat("_ICEState", 0.5f);
    }

    /// <summary>退出冰冻状态，从冻结帧恢复当前动作播放。</summary>
    public void ExitFreeze(float ratio)
    {
        mActionFlow.SetAnimationSpeed(ratio);
        // 冰冻结束时若仍被麻痹，保持冻结帧直到逻辑恢复。
        if (ratio > 0f)
        {
            mActionFlow.ResumeAnimation();
        }
        RenderComponenter.SetPropertyBlockFloat("_ICEState", 0);
    }
}
