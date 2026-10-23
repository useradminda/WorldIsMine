using UnityEngine;

public class PushCameraSideComponent : DieBaseComponent
{
    [SerializeField] private float actionLeadTime = 0.1f;
    [SerializeField, Range(3, 8)] private int freezeFrameCount = 5;
    [SerializeField] private float totalDuration = 2.5f;
    [SerializeField] private float sideTravelDuration = 0.75f;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float elapsed;
    private float freezeDuration;
    private float motionDuration;
    private bool animationResumed;
    private bool finished;

    protected override void EnterDieState()
    {
        startPosition = transform.position;
        elapsed = 0f;
        freezeDuration = Mathf.Max(0f, freezeFrameCount) / 60f;
        motionDuration = Mathf.Max(0.1f, totalDuration - actionLeadTime - freezeDuration);
        animationResumed = false;
        finished = false;
        targetPosition = GetCameraSideTarget();
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
        UpdateMovement(motionTime, motionDuration);
        if (elapsed >= actionLeadTime + freezeDuration + motionDuration)
        {
            finished = true;
            CycleUnitView();
        }
    }

    private void UpdateMovement(float motionTime, float duration)
    {
        float travelDuration = Mathf.Min(Mathf.Max(0.01f, sideTravelDuration), duration);
        float progress = Mathf.Clamp01(motionTime / travelDuration);
        transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
    }

    private Vector3 GetCameraSideTarget()
    {
        Camera targetCamera = Camera.main;
        if (targetCamera == null)
            return startPosition + transform.right * (startPosition.x >= 0f ? 100f : -100f);

        float viewportX = targetCamera.WorldToViewportPoint(startPosition).x < 0.5f ? 0f : 1f;
        Ray ray = targetCamera.ViewportPointToRay(new Vector3(viewportX, 0.5f, 0f));
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, startPosition.y, 0f));
        if (groundPlane.Raycast(ray, out float distance))
            return ray.GetPoint(distance);

        return startPosition + transform.right * (viewportX <= 0f ? -100f : 100f);
    }
}
