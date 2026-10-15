using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DieBaseComponent : MonoBehaviour
{
    protected UnitView mUnitView;

    public void SetUnitView(UnitView unitView)
    {
        mUnitView = unitView;
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
        UnitViewFactory.RemoveUnitView(mUnitView);
        DieBaseComponent dieComp = GetComponent<DieBaseComponent>();
        GameObject.Destroy(dieComp);
    }
}
