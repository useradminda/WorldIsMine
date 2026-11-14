using UnityEngine;

/// <summary>
/// 控制单位 Buff 颜色闪烁的独立表现组件。
/// 用于全局攻击 Buff 等需要让角色颜色在指定颜色和原色之间周期闪烁的场景。
/// </summary>
public class BuffColorBlinkComponent : MonoBehaviour
{
    private const float MaxBuffColorStrength = 0.4f;
    private const float BlinkCycleDuration = 1f;
    private static readonly Color BuffColor = new Color(1f, 1f, 0f, 1f);

    private RenderComponent renderComponent;
    private float elapsedTime;
    private bool isBlinking;

    /// <summary>
    /// 缓存单位模型的渲染组件，供后续更新材质属性块使用。
    /// </summary>
    private void Awake()
    {
        renderComponent = gameObject.GetOrAddComponentInChild<RenderComponent>();
    }

    /// <summary>
    /// 按周期更新 Buff 颜色混合强度，使强度在 0 和 0.4 之间往返变化。
    /// </summary>
    private void Update()
    {
        if (!isBlinking)
        {
            return;
        }

        elapsedTime += Time.deltaTime;
        float halfCycleDuration = BlinkCycleDuration * 0.5f;
        float strength = Mathf.PingPong(elapsedTime / halfCycleDuration, MaxBuffColorStrength);
        ApplyColorStrength(strength);
    }

    /// <summary>
    /// 开始播放 Buff 颜色闪烁，并将初始混合强度设为 0。
    /// </summary>
    public void StartBlink()
    {
        if (renderComponent == null)
        {
            renderComponent = gameObject.GetOrAddComponentInChild<RenderComponent>();
        }

        elapsedTime = 0f;
        isBlinking = true;
        enabled = true;
        renderComponent.SetPropertyBlockColor("_BuffColor", BuffColor);
        ApplyColorStrength(0f);
    }

    /// <summary>
    /// 停止 Buff 颜色闪烁并恢复原始颜色状态。
    /// </summary>
    public void StopBlink()
    {
        isBlinking = false;
        elapsedTime = 0f;
        ResetColor();
        enabled = false;
    }

    /// <summary>
    /// 在对象被禁用或回收到对象池时清理材质属性块，避免下次复用残留 Buff 颜色。
    /// </summary>
    private void OnDisable()
    {
        isBlinking = false;
        elapsedTime = 0f;
        ResetColor();
    }

    /// <summary>
    /// 写入当前 Buff 颜色混合强度。
    /// </summary>
    private void ApplyColorStrength(float strength)
    {
        if (renderComponent == null)
        {
            return;
        }

        renderComponent.SetPropertyBlockColor("_BuffColor", BuffColor);
        renderComponent.SetPropertyBlockFloat("_BuffColorStrength", Mathf.Clamp(strength, 0f, MaxBuffColorStrength));
    }

    /// <summary>
    /// 将 Buff 颜色和混合强度恢复为默认状态。
    /// </summary>
    private void ResetColor()
    {
        if (renderComponent == null)
        {
            return;
        }

        renderComponent.SetPropertyBlockColor("_BuffColor", Color.white);
        renderComponent.SetPropertyBlockFloat("_BuffColorStrength", 0f);
    }
}
