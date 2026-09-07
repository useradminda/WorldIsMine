
using UnityEngine;

public class TornadoDieComponent : DieBaseComponent
{
    [Header("旋转")]
    public float rotateSpeed = 720f;          // 绕中心旋转速度

    [Header("自身旋转")]
    public Vector3 selfRotateSpeed = new Vector3(0f, 720f, 0f);
   

    [Header("扩散")]
    public float startRadius = 0.5f;
    public float radiusSpeed = 0.8f;

    [Header("上升")]
    public float riseSpeed = 3f;

    private Vector3 center;
   // private Vector3 oriAng;
    private float angle;
    private float height;
    private float radius;

    private bool dieState = false;
    private float dieTime = 1f;

    private Transform childTrans;

    // 进入死亡状态
    protected override void EnterDieState()
    {
        childTrans = transform.GetChild(0);
        center = transform.position;// childTrans.localPosition;
        //oriAng = childTrans.localEulerAngles;
        angle = Random.Range(0f, 360f);
        height = 0f;
        radius = startRadius;

        dieState = true;
    }

    protected override void UpdateDie(float dt)
    {
        if (!dieState)
            return;

        dieTime -= dt;
        angle += rotateSpeed * dt;
        height += riseSpeed * dt;
        radius = startRadius + height * height * 0.15f;
        float rad = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Cos(rad) * radius,
            height,
            Mathf.Sin(rad) * radius
        );

        transform.position = center + offset;
        transform.Rotate(
            selfRotateSpeed * dt,
            Space.Self
        );
        if (dieTime <= 0f)
        {
            dieState = false;
            //childTrans.localPosition = center;
            //childTrans.localEulerAngles = oriAng;
            CycleUnitView();
        }
    }
}

