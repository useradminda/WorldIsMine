using UnityEngine;

/// <summary>升空停顿后加速砸地，压扁回弹后回收。</summary>
public class GroundSmashDieComponent : DieBaseComponent
{
    [SerializeField] private float duration = 1.1f;
    [SerializeField] private float liftHeight = 2f;
    [SerializeField, Range(0.1f, 1f)] private float squashRatio = 0.35f;

    private Vector3 startPosition;
    private Vector3 startScale;
    private float elapsed;
    private bool playing;

    /// <summary>保存原始缩放并开始死亡动画。</summary>
    protected override void EnterDieState()
    {
        startPosition = transform.position;
        startScale = transform.localScale;
        elapsed = 0f;
        playing = true;
        mUnitView.ActionFlowComponent.PlayAction(EActionType.die);
    }

    /// <summary>通过分段曲线控制抬升、悬停、下砸和落地形变。</summary>
    protected override void UpdateDie(float dt)
    {
        if (!playing)
        {
            return;
        }

        elapsed += dt;
        float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
        float height = 0f;
        float squash = 0f;
        if (t < 0.3f)
        {
            float phase = t / 0.3f;
            height = liftHeight * (1f - (1f - phase) * (1f - phase));
        }
        else if (t < 0.45f)
        {
            height = liftHeight;
        }
        else if (t < 0.65f)
        {
            float phase = (t - 0.45f) / 0.2f;
            height = liftHeight * (1f - phase * phase);
        }
        else if (t < 0.72f)
        {
            squash = (t - 0.65f) / 0.07f;
        }
        else
        {
            squash = 1f - Mathf.Clamp01((t - 0.72f) / 0.28f);
        }

        transform.position = startPosition + Vector3.up * height;
        Vector3 scaleFactor = new Vector3(1f + squash * 0.35f,
            Mathf.Lerp(1f, squashRatio, squash), 1f + squash * 0.35f);
        transform.localScale = Vector3.Scale(startScale, scaleFactor);

        if (t >= 1f)
        {
            playing = false;
            transform.position = startPosition;
            transform.localScale = startScale;
            CycleUnitView();
        }
    }
}
