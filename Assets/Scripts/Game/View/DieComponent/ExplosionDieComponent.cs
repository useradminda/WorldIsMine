
using UnityEngine;

public class ExplosionDieComponent : DieBaseComponent
{
    [Header("爆开距离")]
    [SerializeField] private float explosionDistance = 2f;

    [Header("向上")]
    [SerializeField] private float upwardHeight = 1f;

    [Header("旋转")]
    [SerializeField] private float rotateSpeed = 720f;

    [Header("随机方向")]
    [SerializeField] private float randomAngle = 30f;

    private Transform childTrans;

    private Vector3 startPosition;

    private Vector3 explosionDirection;

    private float time;
    private float duration;

    private float randomRotate;

    private bool playing;


    protected override void EnterDieState()
    {
        childTrans = transform; //transform.GetChild(0);

        startPosition = childTrans.position;

        time = 0f;

        duration = mUnitView.ActionFlowComponent.GetAnimLen(EActionType.die);

        // 随机一个水平爆炸方向
        float angle = Random.Range(0f, 360f);

        float rad = angle * Mathf.Deg2Rad;

        explosionDirection = new Vector3(
            Mathf.Cos(rad),
            0f,
            Mathf.Sin(rad)
        );

        // 随机旋转方向
        randomRotate = Random.value > 0.5f ? 1f : -1f;

        playing = true;
    }


    protected override void UpdateDie(float dt)
    {
        if (!playing || childTrans == null)
            return;

        time += dt;

        float t = Mathf.Clamp01(time / duration);

        /*
         * 爆炸效果：
         *
         * 开始很快
         * 后面逐渐减速
         *
         * 0 -> 1
         */
        float explosionT = 1f - Mathf.Pow(1f - t, 3f);

        Vector3 horizontalOffset =
            explosionDirection *
            explosionDistance *
            explosionT;

        // 上升
        float height =
            upwardHeight *
            Mathf.Sin(t * Mathf.PI);

        childTrans.position =
            startPosition +
            horizontalOffset +
            Vector3.up * height;

        // 自己旋转
        childTrans.Rotate(
            Vector3.up,
            rotateSpeed * randomRotate * dt,
            Space.Self
        );

        // 结束
        if (t >= 1f)
        {
            playing = false;

            childTrans.position =
                startPosition +
                explosionDirection * explosionDistance;

            CycleUnitView();
        }
    }
}

//### 这个 ExplosionDie 的轨迹

//例如一个角色随机到了右前方：

//```text
//             起点
//               ●
//              ↗
//            ↗
//          ↗
//        ↗
//      ●

//        ↑
//       先向上
//       再落回去
//```

//实际上是：

//```text
//       ●
//      / \
//     /   \
//    /     \
//   ●       ●
//```

//但是**没有真正的爆炸中心**。

//每一个角色：

//```text
//自己的当前位置
//       ↓
//      ●
//      \
//       \
//        ●
//```

//---