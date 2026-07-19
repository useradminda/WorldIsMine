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
                renderComponenter = gameObject.GetComponent<RenderComponent>();
            return renderComponenter;
        }
    }
    private ActionFlow actionFlower;
    protected ActionFlow actorFlower
    {
        get
        {
            if(actionFlower == null)
                actionFlower = gameObject.GetComponent<ActionFlow>();
            return actionFlower;
        }
    }

    public void SetFreeze()
    {     
        RenderComponenter.SetPropertyBlockFloat("_ICEState", 1);
    }

    public void ExitFreeze()
    {
        RenderComponenter.SetPropertyBlockFloat("_ICEState", 0);
    }
}
