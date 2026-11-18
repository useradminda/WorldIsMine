using UnityEngine;

public class PushDieComponent : DieBaseComponent
{
    [SerializeField] private float totalDuration = 0.9f;
    [SerializeField] private float laybackDuration = 0.18f;
    [SerializeField] private float laybackRotationAngle = -90f;
    [SerializeField] private float backwardDistance = 10f;
    [SerializeField] private float upwardHeight = 1.2f;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private float elapsed;
    private bool finished;

    protected override void EnterDieState()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        elapsed = 0f;
        finished = false;
        mUnitView.ActionFlowComponent.PlayAction(EActionType.wait);
    }

    protected override void UpdateDie(float dt)
    {
        if (finished || mUnitView == null || transform == null) return;
        elapsed += dt;
        float duration = Mathf.Max(0.1f, totalDuration);
        float motionTime = Mathf.Min(elapsed, duration);
        UpdateMovement(motionTime, duration);
        if (elapsed >= duration)
        {
            finished = true;
            CycleUnitView();
        }
    }

    private void UpdateMovement(float motionTime, float duration)
    {
        Vector3 backwardDirection = -(startRotation * Vector3.forward);
        backwardDirection.y = 0f;
        backwardDirection.Normalize();
        float t = Mathf.Clamp01(motionTime / Mathf.Max(0.1f, duration));
        // 前段快速加速击飞，后段平滑减速落地，整个过程中保持仰面朝天。
        float flightT = 1f - Mathf.Pow(1f - t, 2f);
        float height = 4f * upwardHeight * t * (1f - t);
        transform.position = startPosition
            + backwardDirection * (backwardDistance * flightT)
            + Vector3.up * height;

        float rotationT = Mathf.Clamp01(motionTime / Mathf.Max(0.01f, laybackDuration));
        rotationT = rotationT * rotationT * (3f - 2f * rotationT);
        transform.rotation = Quaternion.Slerp(
            startRotation,
            startRotation * Quaternion.Euler(laybackRotationAngle, 0f, 0f),
            rotationT);
    }
}
