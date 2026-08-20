using Nebukam.ORCA;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
// ±ù¶³×é¼þ
public class FreezeComponent : MonoBehaviour
{

    private RenderComponent renderComponenter;
    protected RenderComponent RenderComponenter
    {
        get
        {
            if (renderComponenter == null)
                renderComponenter = gameObject.GetOrAddComponent<RenderComponent>();
            return renderComponenter;
        }
    }
    private ActionFlow actionFlower;
    protected ActionFlow mActionFlow
    {
        get
        {
            if(actionFlower == null)
                actionFlower = gameObject.GetOrAddComponent<ActionFlow>();
            return actionFlower;
        }
    }

    public void SetFreeze()
    {
        mActionFlow.ActionFreeze(0.001f);
        RenderComponenter.SetPropertyBlockFloat("_ICEState", 1);
    }

    public void ExitFreeze()
    {
        mActionFlow.ActionExitFreeze();
        RenderComponenter.SetPropertyBlockFloat("_ICEState", 0);
    }
}
