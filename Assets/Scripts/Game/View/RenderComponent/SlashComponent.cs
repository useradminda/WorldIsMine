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
                renderComponenter = this.gameObject.GetOrAddComponentInChild<RenderComponent>();
            return renderComponenter;
        }
    }

    public void SetSlash(Color slashColor, float time = 0.05f)
    {
        if (slashState == true)
            return;
        RenderComponenter.SetPropertyBlockColor("_SlashColor", slashColor);
        currentTime = time;
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
