
using UnityEngine;

public class SpiralDieComponent : DieBaseComponent
{
    [Header("旋转")]
    [SerializeField] private float rotateSpeed = 720f;

    [Header("向外扩散")]
    [SerializeField] private float startRadius = 0.1f;
    [SerializeField] private float endRadius = 2f;

    [Header("上升")]
    [SerializeField] private float riseHeight = 1.5f;

    private Transform childTrans;

    private Vector3 startPosition;

    private float angle;
    private float time;
    private float duration;

    private bool playing;

    // 随机旋转方向
    private float rotateDir;


    protected override void EnterDieState()
    {
        childTrans = transform;

        startPosition = childTrans.position;

        angle = Random.Range(0f, 360f);

        // 有的顺时针，有的逆时针
        rotateDir = Random.value > 0.5f ? 1f : -1f;

        time = 0f;

        duration = mUnitView.ActionFlowComponent.GetAnimLen(EActionType.die);

        playing = true;
    }


    protected override void UpdateDie(float dt)
    {
        if (!playing || childTrans == null)
            return;

        time += dt;

        float t = Mathf.Clamp01(time / duration);

        // 旋转
        angle += rotateSpeed * rotateDir * dt;

        float rad = angle * Mathf.Deg2Rad;

        // 半径逐渐变大
        float radius = Mathf.Lerp(
            startRadius,
            endRadius,
            t
        );

        // 螺旋位置
        Vector3 offset = new Vector3(
            Mathf.Cos(rad) * radius,
            riseHeight * t,
            Mathf.Sin(rad) * radius
        );

        childTrans.position = startPosition + offset;

        // 死亡结束
        if (t >= 1f)
        {
            playing = false;

            childTrans.position =
                startPosition +
                new Vector3(
                    Mathf.Cos(rad) * endRadius,
                    riseHeight,
                    Mathf.Sin(rad) * endRadius
                );

            CycleUnitView();
        }
    }
}


//它的轨迹大概是：

//```text
//             ●
//          ↗
//       ↗
//    ●
//      ↘
//         ↘
//            ●
//```

//而且实际上是**向上螺旋**：

//```text
//       ●
//      ↗
//    ↗
//   ●
//    ↘
//      ↘
//        ●
//```

//如果你不想上升，把：

//```csharp
//riseHeight = 0;
//```

//就变成纯平面螺旋。

//---


