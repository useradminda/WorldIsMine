using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DieBaseComponent : MonoBehaviour
{
    protected UnitView mUnitView;

    public void SetUnitView(UnitView unitView)
    {
        enabled = true;
        mUnitView = unitView;
        // 进入死亡表现前清理普攻卡刀，避免角色死亡后停留在攻击动画帧。
        if (mUnitView != null)
        {
            mUnitView.AttackStopComp.Cancel();
        }
        EnterDieState();
    }

    /// <summary>
    /// 清理死亡表现组件对旧单位逻辑和协程状态的引用，供 View 缓存复用。
    /// </summary>
    public void ClearForRecycle()
    {
        mUnitView = null;
        enabled = false;
    }

    protected virtual void EnterDieState()
    {

    }

    /// <summary>
    /// 按死亡表现持续时间换算动画速度，确保死亡动作在指定时间内完整播放一次。
    /// </summary>
    protected void PlayDieActionForDuration(float duration)
    {
        float animationLength = mUnitView.ActionFlowComponent.GetAnimLen(EActionType.die);
        float safeDuration = Mathf.Max(0.0001f, duration);
        float speedRatio = animationLength > 0.0001f ? animationLength / safeDuration : 1f;
        mUnitView.ActionFlowComponent.SetAnimationSpeed(speedRatio);
        mUnitView.ActionFlowComponent.PlayAction(EActionType.die);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateDie(Time.deltaTime);   
    }

    protected virtual void UpdateDie(float dt)
    {

    }

    protected void CycleUnitView()
    {
        mUnitView.NotifyDeathViewFinished();
        UnitViewFactory.RemoveUnitView(mUnitView);
        DieBaseComponent dieComp = GetComponent<DieBaseComponent>();
        GameObject.Destroy(dieComp);
    }
}
