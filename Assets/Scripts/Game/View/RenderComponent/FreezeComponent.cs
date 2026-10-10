using Nebukam.ORCA;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
// �������
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

    public void SetFreeze(float ratio)
    {
        mActionFlow.SetAnimationSpeed(ratio);
        RenderComponenter.SetPropertyBlockFloat("_ICEState", 1);
    }

    public void ExitFreeze(float ratio)
    {
        mActionFlow.SetAnimationSpeed(ratio);
        RenderComponenter.SetPropertyBlockFloat("_ICEState", 0);
    }
}
