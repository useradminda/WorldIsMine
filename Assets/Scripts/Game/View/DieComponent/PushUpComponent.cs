using UnityEngine;

public class PushUpComponent : DieBaseComponent
{
    [SerializeField] private float actionLeadTime = 0.1f;
    [SerializeField, Range(3, 8)] private int freezeFrameCount = 5;
    [SerializeField] private float totalDuration = 2.5f;
    [SerializeField] private float rotationStartTime = 0f;
    [SerializeField] private float rotationDuration = 0.35f;
    [SerializeField] private float spinRotationAngle = 360f;
    [SerializeField] private float laybackRotationAngle = 180f;
    [SerializeField] private float upwardVelocity = 14f;
    [SerializeField] private float gravity = 24f;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private float elapsed;
    private float freezeDuration;
    private float motionDuration;
    private bool animationResumed;
    private bool finished;

    protected override void EnterDieState()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        elapsed = 0f;
        freezeDuration = Mathf.Max(0f, freezeFrameCount) / 60f;
        motionDuration = Mathf.Max(0.1f, totalDuration - actionLeadTime - freezeDuration);
        animationResumed = false;
        finished = false;
        mUnitView.ActionFlowComponent.SetAnimationSpeed(1f);
        mUnitView.ActionFlowComponent.PlayAction(EActionType.die);
    }

    protected override void UpdateDie(float dt)
    {
        if (finished || mUnitView == null || transform == null) return;
        elapsed += dt;
        if (elapsed >= actionLeadTime && elapsed < actionLeadTime + freezeDuration)
        {
            mUnitView.ActionFlowComponent.FreezeAnimationAt(actionLeadTime);
        }
        if (elapsed < actionLeadTime + freezeDuration) return;
        if (!animationResumed)
        {
            mUnitView.ActionFlowComponent.ResumeAnimation();
            animationResumed = true;
        }
        float motionTime = Mathf.Min(elapsed - actionLeadTime - freezeDuration, motionDuration);
        UpdateMovement(motionTime);
        UpdateRotation(motionTime);
        if (elapsed >= actionLeadTime + freezeDuration + motionDuration)
        {
            finished = true;
            CycleUnitView();
        }
    }

    private void UpdateMovement(float motionTime)
    {
        float height = upwardVelocity * motionTime - 0.5f * Mathf.Max(0.01f, gravity) * motionTime * motionTime;
        Vector3 position = startPosition;
        position.y = Mathf.Max(0f, startPosition.y + height);
        transform.position = position;
    }

    private void UpdateRotation(float motionTime)
    {
        float rotateDuration = Mathf.Max(0f, rotationDuration);
        float rotateTime = Mathf.Max(0f, motionTime - rotationStartTime);
        float progress = rotateDuration <= 0f ? 1f : Mathf.Clamp01(rotateTime / rotateDuration);
        transform.rotation = startRotation * Quaternion.Euler(
            laybackRotationAngle * progress,
            spinRotationAngle * progress,
            0f);
    }
}
