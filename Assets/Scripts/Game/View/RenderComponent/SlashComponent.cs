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

    /// <summary>开始受击闪白，并设置闪白持续时间与混合强度。</summary>
    public void SetSlash(Color slashColor, float time = 0.05f, float flashStrength = 1f)
    {
        if (slashState == true)
            return;
        RenderComponenter.SetPropertyBlockColor("_SlashColor", slashColor);
        // 城门材质可能把默认闪白强度设为 0，受击时需要通过属性块临时开启闪白。
        RenderComponenter.SetPropertyBlockFloat("_FlashStrength", Mathf.Clamp01(flashStrength));
        currentTime = time;
        slashState = true;
    }

    /// <summary>设置单位死亡后的闪白颜色，并关闭闪白强度。</summary>
    public void SetDead()
    {
        slashState = false;
        RenderComponenter.SetPropertyBlockColor("_SlashColor", new Color(85 / 255f, 85 / 255f, 85 / 255f, 1f));
        RenderComponenter.SetPropertyBlockFloat("_FlashStrength", 0f);
    }

    /// <summary>结束受击闪白并恢复默认闪白强度。</summary>
    public void ExitSlash()
    {
        slashState = false;
        RenderComponenter.SetPropertyBlockColor("_SlashColor", Color.black);
        RenderComponenter.SetPropertyBlockFloat("_FlashStrength", 0f);
    }

    /// <summary>逐帧倒计时闪白效果，时间结束后关闭闪白。</summary>
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
