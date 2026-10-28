using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NormalDieComponent : DieBaseComponent
{
    private Vector3 startPosition;
    private Vector3 backwardDirection;
    private float startTime;
    private float duration;
    private float backwardDistance = 10;
    private float maxHeight = 5f;
    private const float MovementDurationScale = 0.6f;
    [SerializeField] private float actionLeadTime = 0.1f;
    [SerializeField, Range(0, 8)] private int freezeFrameCount = 5;
    [SerializeField] private float recoverySpeed = 2f;
    private bool playingDie;
    private float elapsed;
    private float freezeDuration;
    private bool animationResumed;
    // 进入
    protected override void EnterDieState()
    {
        this.duration = Mathf.Max(0.5f, mUnitView.ActionFlowComponent.GetAnimLen(EActionType.die) * MovementDurationScale);
        PlayDieActionForDuration(duration);
        startPosition = transform.position;
        backwardDirection = -transform.forward;
        startTime = Time.time;
        elapsed = 0f;
        freezeDuration = Mathf.Max(0f, freezeFrameCount) / 60f;
        animationResumed = false;
        playingDie = true;
    }

    protected override void UpdateDie(float dt)
    {
        if (!playingDie || transform == null)
            return;
        elapsed += dt;
        if (!animationResumed && elapsed >= actionLeadTime && elapsed < actionLeadTime + freezeDuration)
        {
            mUnitView.ActionFlowComponent.FreezeAnimationAt(actionLeadTime);
        }
        if (!animationResumed && elapsed >= actionLeadTime + freezeDuration)
        {
            mUnitView.ActionFlowComponent.ResumeAnimation();
            mUnitView.ActionFlowComponent.SetAnimationSpeed(Mathf.Max(1f, recoverySpeed));
            animationResumed = true;
        }

        float t = Mathf.Clamp01((Time.time - startTime) / duration);
        Vector3 horizontalOffset = backwardDirection * backwardDistance * t;
        float height = 4f * maxHeight * t * (1f - t);
        transform.position = startPosition + horizontalOffset + Vector3.up * height;

        if (t >= 1f)
        {
            transform.position = startPosition + backwardDirection * backwardDistance;
            playingDie = false;
            CycleUnitView();
            return;
        }
    }

}
