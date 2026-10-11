using UnityEngine;

/// <summary>击飞落地后小幅弹跳，再倒地回收。</summary>
public class BounceDieComponent : DieBaseComponent
{
    [SerializeField] private float duration = 1.2f;
    [SerializeField] private float flyDistance = 3f;
    [SerializeField] private float flyHeight = 1.5f;
    [SerializeField] private float bounceHeight = 0.45f;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 direction;
    private float elapsed;
    private bool playing;

    /// <summary>记录初始姿态，开始死亡动画。</summary>
    protected override void EnterDieState()
    {
        startPosition = transform.position;
        startRotation = transform.localRotation;
        direction = Vector3.ProjectOnPlane(-transform.forward, Vector3.up).normalized;
        elapsed = 0f;
        playing = true;
        mUnitView.ActionFlowComponent.PlayAction(EActionType.die);
    }

    /// <summary>依次播放主抛物线、二次弹跳和落地停留。</summary>
    protected override void UpdateDie(float dt)
    {
        if (!playing)
        {
            return;
        }

        elapsed += dt;
        float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
        float height;
        float distance;
        if (t < 0.6f)
        {
            float phase = t / 0.6f;
            height = 4f * flyHeight * phase * (1f - phase);
            distance = flyDistance * 0.8f * phase;
        }
        else if (t < 0.9f)
        {
            float phase = (t - 0.6f) / 0.3f;
            height = 4f * bounceHeight * phase * (1f - phase);
            distance = flyDistance * (0.8f + 0.2f * phase);
        }
        else
        {
            height = 0f;
            distance = flyDistance;
        }

        transform.position = startPosition + direction * distance + Vector3.up * height;
        float roll = Mathf.Clamp01(t / 0.9f) * 450f;
        transform.localRotation = startRotation * Quaternion.Euler(roll, 0f, 0f);

        if (t >= 1f)
        {
            playing = false;
            transform.position = startPosition;
            transform.localRotation = startRotation;
            CycleUnitView();
        }
    }
}
