using UnityEngine;

/// <summary>麻痹表现组件，只控制动作播放速度，不切换冰冻材质。</summary>
public class ParalysisComponent : MonoBehaviour
{
    private ActionFlow actionFlow;

    private ActionFlow ActionFlowComponent
    {
        get
        {
            if (actionFlow == null)
            {
                actionFlow = gameObject.GetOrAddComponentInChild<ActionFlow>();
            }
            return actionFlow;
        }
    }

    /// <summary>进入麻痹表现并按当前逻辑倍率设置动作速度。</summary>
    public void SetParalysis(float ratio)
    {
        ActionFlowComponent.SetAnimationSpeed(ratio);
        if (ratio <= 0f)
        {
            ActionFlowComponent.FreezeCurrentAnimation();
        }
    }

    /// <summary>退出麻痹表现并按退出后的逻辑倍率恢复动作速度。</summary>
    public void ExitParalysis(float ratio)
    {
        ActionFlowComponent.SetAnimationSpeed(ratio);
        // 仍有其他控制效果时继续冻结，最后一个控制结束后才恢复动画。
        if (ratio > 0f)
        {
            ActionFlowComponent.ResumeAnimation();
        }
    }
}
