
using UnityEngine;

public class ExplosionDieComponent : DieBaseComponent
{
    [Header("爆炸点")]
    [SerializeField] private Vector3 explosionPoint;

    [Header("爆开距离")]
    [SerializeField] private float explosionDistance = 2f;

    [Header("向上")]
    [SerializeField] private float upwardHeight = 1f;

    [Header("旋转")]
    [SerializeField] private float rotateSpeed = 720f;

    [Header("随机方向")]
    [SerializeField] private float randomAngle = 10f;

    private Vector3 startPosition;
    private Vector3 explosionDirection;

    private float time;
    private float duration;

    private float randomRotate;

    private bool playing;

    public void SetExplosionPoint(Vector3 explosionPoint)
    {
        this.explosionPoint = explosionPoint;
    }

    protected override void EnterDieState()
    {
        startPosition = transform.position;

        time = 0f;

        duration =
            mUnitView.ActionFlowComponent.GetAnimLen(
                EActionType.die
            );

        // 爆炸点 -> 当前单位
        explosionDirection = transform.position - explosionPoint;

        // 只计算水平爆炸方向
        explosionDirection.y = 0f;

        // 防止单位刚好在爆炸中心
        if (explosionDirection.sqrMagnitude < 0.0001f)
        {
            float angle = Random.Range(0f, 360f);

            float rad = angle * Mathf.Deg2Rad;

            explosionDirection = new Vector3(
                Mathf.Cos(rad),
                0f,
                Mathf.Sin(rad)
            );
        }
        else
        {
            explosionDirection.Normalize();

            // 在爆炸方向基础上增加一点随机偏转
            if (randomAngle > 0f)
            {
                float angle =
                    Random.Range(-randomAngle, randomAngle);

                explosionDirection =
                    Quaternion.Euler(
                        0f,
                        angle,
                        0f
                    ) * explosionDirection;
            }
        }

        // 随机旋转方向
        randomRotate =
            Random.value > 0.5f ? 1f : -1f;

        playing = true;
    }


    protected override void UpdateDie(float dt)
    {
        if (!playing)
            return;

        time += dt;

        float t =
            Mathf.Clamp01(time / duration);

        // 爆炸初期速度快，后面减速
        float explosionT =
            1f - Mathf.Pow(1f - t, 3f);

        Vector3 horizontalOffset =
            explosionDirection *
            explosionDistance *
            explosionT;

        // 抛物线
        float height =
            upwardHeight *
            Mathf.Sin(t * Mathf.PI);

        transform.position =
            startPosition +
            horizontalOffset +
            Vector3.up * height;

        // 自身旋转
        transform.Rotate(
            Vector3.up,
            rotateSpeed *
            randomRotate *
            dt,
            Space.Self
        );

        // 结束
        if (t >= 1f)
        {
            playing = false;

            transform.position =
                startPosition +
                explosionDirection *
                explosionDistance;

            CycleUnitView();
        }
    }
}
