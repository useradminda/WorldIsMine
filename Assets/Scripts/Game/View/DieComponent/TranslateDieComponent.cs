using UnityEngine;

/// <summary>
/// 传送即死表现组件。
/// 适用于传送技能触发的普通士兵即死：单位先出现在空中，保持躺倒姿态落到地面，停留后回收。
/// </summary>
public class TranslateDieComponent : DieBaseComponent
{
    // 死亡单位传送到的天空高度。
    private const float StartHeight = 10f;
    // 从天空落到地面的时间，单位为秒。
    private const float FallDuration = 2f;
    // 落地后停留时间，单位为秒。
    private const float GroundStayDuration = 2f;
    // 躺倒旋转角度。
    private const float LayRotationAngle = 90f;

    private Vector3 groundPosition;
    private Quaternion layRotation;
    private float elapsed;
    private bool finished;

    /// <summary>
    /// 初始化空中出生点、躺倒姿态和待机动画。
    /// </summary>
    protected override void EnterDieState()
    {
        Vector3 currentPosition = transform.position;
        groundPosition = new Vector3(currentPosition.x, 0f, currentPosition.z);
        transform.position = new Vector3(currentPosition.x, StartHeight, currentPosition.z);
        layRotation = transform.rotation * Quaternion.Euler(LayRotationAngle, 0f, 0f);
        transform.rotation = layRotation;
        elapsed = 0f;
        finished = false;
        mUnitView.ActionFlowComponent.SetAnimationSpeed(1f);
        mUnitView.ActionFlowComponent.PlayAction(EActionType.wait);
    }

    /// <summary>
    /// 推进空中下落、落地停留和最终回收流程。
    /// </summary>
    protected override void UpdateDie(float dt)
    {
        if (finished || mUnitView == null)
        {
            return;
        }

        elapsed += dt;
        if (elapsed <= FallDuration)
        {
            float progress = Mathf.Clamp01(elapsed / FallDuration);
            float easedProgress = progress * progress * (3f - 2f * progress);
            transform.position = Vector3.Lerp(
                new Vector3(groundPosition.x, StartHeight, groundPosition.z),
                groundPosition,
                easedProgress);
            transform.rotation = layRotation;
            return;
        }

        transform.position = groundPosition;
        transform.rotation = layRotation;
        if (elapsed >= FallDuration + GroundStayDuration)
        {
            finished = true;
            CycleUnitView();
        }
    }
}
