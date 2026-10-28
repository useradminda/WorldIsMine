using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DieBaseComponent : MonoBehaviour
{
    protected UnitView mUnitView;

    public void SetUnitView(UnitView unitView)
    {
        mUnitView = unitView;
        // 进入死亡表现前清理普攻卡刀，避免角色死亡后停留在攻击动画帧。
        if (mUnitView != null)
        {
            mUnitView.AttackStopComp.Cancel();
        }
        EnterDieState();
    }

    protected virtual void EnterDieState()
    {

    }

    protected void PlayDieActionForDuration(float duration)
    {
        float animationLength = mUnitView.ActionFlowComponent.GetAnimLen(EActionType.die);
        float speedRatio = animationLength > 0.0001f ? duration / animationLength : 1f;
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
