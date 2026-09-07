using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NormalDieComponent : DieBaseComponent
{
    private Vector3 startPosition;
    private Vector3 backwardDirection;
    private float startTime;
    private float duration;
    private float backwardDistance = 5;
    private float maxHeight = 2f;
    private bool playingDie;
    // 进入
    protected override void EnterDieState()
    {
        this.duration = mUnitView.ActionFlowComponent.GetAnimLen(EActionType.die);
        mUnitView.ActionFlowComponent.PlayAction(EActionType.die);
        startPosition = transform.position;
        backwardDirection = -transform.forward;
        startTime = Time.time;
        playingDie = true;
    }

    protected override void UpdateDie(float dt)
    {
        if (!playingDie || transform == null)
            return;
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
