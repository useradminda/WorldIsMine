using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nebukam.ORCA;
public class SlashComponent : MonoBehaviour
{
    private float currentTime = 0;
    private bool slashState = false;
    private RenderComponent renderComponenter;
    protected RenderComponent RenderComponenter
    {
        get
        {
            if(renderComponenter == null)
                renderComponenter = this.gameObject.GetComponent<RenderComponent>();
            return renderComponenter;
        }
    }

    public void SetSlash()
    {
        if (slashState == true)
            return;
        RenderComponenter.SetPropertyBlockColor("_SlashColor", new Color(159/ 255f, 153/ 255f, 82/255f, 1f));
        currentTime = 0.1f;
        slashState = true;
    }

    public void SetDead()
    {
        slashState = false;
        RenderComponenter.SetPropertyBlockColor("_SlashColor", new Color(85 / 255f, 85 / 255f, 85 / 255f, 1f));
    }

    public void ExitSlash()
    {
        slashState = false;
        RenderComponenter.SetPropertyBlockColor("_SlashColor", Color.black);
    }

    // Update is called once per frame
    void Update()
    {
        if (slashState == false)
        {
            return;
        }
        currentTime -= Time.deltaTime;
        if (currentTime <= 0)
            ExitSlash();
    }

}
