
using UnityEngine;

public class RollDieComponent : DieBaseComponent
{
    [Header("飞行")]
    [SerializeField] private float flyDistance = 3f;
    [SerializeField] private float maxHeight = 1.2f;

    [Header("翻滚")]
    [SerializeField] private float rollSpeed = 720f;

    [Header("随机")]
    [SerializeField] private bool randomRollDirection = true;

    private Vector3 startPosition;
    private Vector3 flyDirection;

    private float time;
    private float duration;

    private float rollDirection;

    private bool playing;


    protected override void EnterDieState()
    {
        startPosition = transform.position;

        time = 0f;

        duration = mUnitView.ActionFlowComponent.GetAnimLen(EActionType.die);

        // 随机一个水平飞行方向
        float angle = Random.Range(0f, 360f);
        float rad = angle * Mathf.Deg2Rad;

        flyDirection = new Vector3(
            Mathf.Cos(rad),
            0f,
            Mathf.Sin(rad)
        );

        // 随机翻滚方向
        rollDirection = randomRollDirection
            ? (Random.value > 0.5f ? 1f : -1f)
            : 1f;

        playing = true;
    }


    protected override void UpdateDie(float dt)
    {
        if (!playing || transform == null)
            return;

        time += dt;

        float t = Mathf.Clamp01(time / duration);


        // ========================================
        // 1. 水平飞行
        // ========================================

        // 前期快速飞出去，后期减速
        float moveT = 1f - Mathf.Pow(1f - t, 2f);

        Vector3 horizontalOffset =
            flyDirection *
            flyDistance *
            moveT;


        // ========================================
        // 2. 抛物线高度
        // ========================================

        float height =
            4f *
            maxHeight *
            t *
            (1f - t);


        transform.position =
            startPosition +
            horizontalOffset +
            Vector3.up * height;


        // ========================================
        // 3. 翻滚
        // ========================================

        float rollAngle =
            rollSpeed *
            rollDirection *
            dt;

        // X轴翻滚
        transform.Rotate(
            Vector3.right,
            rollAngle,
            Space.Self
        );


        // ========================================
        // 4. 结束
        // ========================================

        if (t >= 1f)
        {
            playing = false;

            transform.position =
                startPosition +
                flyDirection * flyDistance;

            CycleUnitView();
        }
    }
}


//### 这个版本的几个参数

//例如：

//```text
//flyDistance = 3
//maxHeight   = 1.2
//rollSpeed   = 720
//```

//效果就是：

//```text
//             ↗ ↻
//          ↗     ↻
//       ↗          ↻
//    ●
//```

//其中高度：

//```csharp
//4f * maxHeight * t * (1f - t)
//```

//和你之前的 `NormalDie` 一样，所以：

//```text
//开始       中间最高       结束
//  ●          ●             ●
//             ↑
//          maxHeight
//```

//---

//### 如果你想让“翻滚”更明显

//现在：

//```csharp
//transform.Rotate(Vector3.right, rollAngle, Space.Self);
//```

//是绕自身** X轴**翻滚。

//如果你的角色模型朝向和 X 轴不太合适，可以改成：

//```csharp
//transform.Rotate(Vector3.forward, rollAngle, Space.Self);
//```

//或者：

//```csharp
//transform.Rotate(
//    new Vector3(1f, 0.3f, 0.5f).normalized,
//    rollAngle,
//    Space.Self
//);
//```

//最后这个会变成一种** 歪着滚出去**的卡通效果，我比较推荐。

//另外，这个版本有一个特点：**每个死亡单位都会随机向不同方向滚出去**，所以一群单位同时死亡时，会有一种“散开翻飞”的感觉。
