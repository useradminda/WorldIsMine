//using UnityEngine;

//public class TwoBoneIK : MonoBehaviour
//{
//    [Header("Joints")]
//    public Transform hip;
//    public Transform knee;
//    public Transform foot;

//    [Header("Target")]
//    public Transform target;

//    [Header("IK Plane")]
//    public Transform pole;

//    float upperLength;
//    float lowerLength;

//    void Start()
//    {
//        upperLength = Vector3.Distance(hip.position, knee.position);
//        lowerLength = Vector3.Distance(knee.position, foot.position);
//    }

//    void LateUpdate()
//    {
//        Solve();
//    }

//    public void Solve()
//    {
//        Vector3 rootPos = hip.position;
//        Vector3 targetPos = target.position;

//        Vector3 toTarget = targetPos - rootPos;

//        float distance = toTarget.magnitude;

//        float minDistance = Mathf.Abs(upperLength - lowerLength) + 0.001f;
//        float maxDistance = upperLength + lowerLength - 0.001f;

//        distance = Mathf.Clamp(distance, minDistance, maxDistance);

//        Vector3 direction = toTarget.normalized;

//        // IK 平面的方向
//        Vector3 poleDir = pole.position - rootPos;

//        Vector3 planeNormal = Vector3.Cross(direction, poleDir).normalized;

//        // 如果 pole 与 target 在同一直线上
//        if (planeNormal.sqrMagnitude < 0.0001f)
//        {
//            planeNormal = Vector3.up;
//        }

//        // 重新计算真正的弯曲方向
//        Vector3 bendDirection = Vector3.Cross(planeNormal, direction).normalized;

//        // 余弦定理
//        float cosHip =
//            (upperLength * upperLength + distance * distance - lowerLength * lowerLength) /
//            (2f * upperLength * distance);

//        cosHip = Mathf.Clamp(cosHip, -1f, 1f);

//        float hipAngle = Mathf.Acos(cosHip) * Mathf.Rad2Deg;

//        // 上腿方向
//        Quaternion hipRotation =
//            Quaternion.LookRotation(direction, bendDirection);

//        hipRotation =
//            Quaternion.AngleAxis(-hipAngle, planeNormal) * hipRotation;

//        hip.rotation = hipRotation;

//        // 强制 Knee 指向 Target
//        Vector3 kneeToTarget = targetPos - knee.position;

//        if (kneeToTarget.sqrMagnitude > 0.0001f)
//        {
//            knee.rotation =
//                Quaternion.LookRotation(kneeToTarget.normalized, bendDirection);
//        }
//    }
//}


using UnityEngine;

public class TwoBoneIK : MonoBehaviour
{
    [Header("Joints")]
    public Transform hip;
    public Transform knee;
    public Transform foot;

    [Header("Target")]
    public Transform target;

    [Header("Pole")]
    public Transform pole;

    [Header("Settings")]
    [Range(0f, 1f)]
    public float weight = 1f;

    // 骨骼原始长度
    private float upperLength;
    private float lowerLength;

    // 初始局部旋转
    private Quaternion hipStartLocalRotation;
    private Quaternion kneeStartLocalRotation;
    private Quaternion footStartLocalRotation;

    // 初始世界旋转
    private Quaternion hipStartWorldRotation;
    private Quaternion kneeStartWorldRotation;
    private Quaternion footStartWorldRotation;

    // Local Y 是骨骼长度方向
    private static readonly Vector3 BoneAxis = Vector3.up;

    private void Awake()
    {
        if (hip == null || knee == null || foot == null)
            return;

        upperLength = Vector3.Distance(hip.position, knee.position);
        lowerLength = Vector3.Distance(knee.position, foot.position);

        hipStartLocalRotation = hip.localRotation;
        kneeStartLocalRotation = knee.localRotation;
        footStartLocalRotation = foot.localRotation;

        hipStartWorldRotation = hip.rotation;
        kneeStartWorldRotation = knee.rotation;
        footStartWorldRotation = foot.rotation;
    }

    private void LateUpdate()
    {
        if (hip == null || knee == null || foot == null || target == null)
            return;

        SolveIK();
    }

    private void SolveIK()
    {
        if (weight <= 0f)
            return;

        Vector3 rootPosition = hip.position;
        Vector3 targetPosition = target.position;

        // --------------------------------------------------
        // 1. 根据 Target 和 Pole 计算新的 Knee 位置
        // --------------------------------------------------

        Vector3 toTarget = targetPosition - rootPosition;
        float targetDistance = toTarget.magnitude;

        if (targetDistance < 0.0001f)
            return;

        Vector3 targetDirection = toTarget / targetDistance;

        // 限制目标距离，防止三角形无法成立
        float minDistance = Mathf.Abs(upperLength - lowerLength) + 0.0001f;
        float maxDistance = upperLength + lowerLength - 0.0001f;

        float solvedDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);

        Vector3 solvedTarget = rootPosition + targetDirection * solvedDistance;

        // --------------------------------------------------
        // 2. Pole 决定膝盖应该往哪边弯
        // --------------------------------------------------

        Vector3 poleDirection;

        if (pole != null)
        {
            poleDirection = pole.position - rootPosition;

            // 去掉沿着 root -> target 的分量
            poleDirection -= Vector3.Project(poleDirection, targetDirection);

            if (poleDirection.sqrMagnitude < 0.000001f)
                poleDirection = Vector3.Cross(targetDirection, hip.up);

            poleDirection.Normalize();
        }
        else
        {
            poleDirection = Vector3.Cross(targetDirection, hip.right);

            if (poleDirection.sqrMagnitude < 0.000001f)
                poleDirection = hip.up;

            poleDirection.Normalize();
        }

        // --------------------------------------------------
        // 3. 余弦定理计算 Hip 到 Knee 的角度
        // --------------------------------------------------

        float a = upperLength;
        float b = lowerLength;
        float c = solvedDistance;

        float cosAngle =
            (a * a + c * c - b * b) /
            (2f * a * c);

        cosAngle = Mathf.Clamp(cosAngle, -1f, 1f);

        float angle = Mathf.Acos(cosAngle);

        // --------------------------------------------------
        // 4. 得到理论上的 Knee 位置
        // --------------------------------------------------

        Vector3 kneeOffset =
            targetDirection * (Mathf.Cos(angle) * upperLength) +
            poleDirection * (Mathf.Sin(angle) * upperLength);

        Vector3 solvedKnee = rootPosition + kneeOffset;

        // --------------------------------------------------
        // 5. 计算 Hip 应该旋转到什么方向
        //
        // 注意：
        // 不修改 hip.position
        // 不修改 knee.position
        // 不修改 foot.position
        //
        // 只旋转 Hip
        // --------------------------------------------------

        Vector3 desiredUpperDirection =
            (solvedKnee - hip.position).normalized;

        Quaternion hipDelta =
            Quaternion.FromToRotation(
                hip.TransformDirection(BoneAxis),
                desiredUpperDirection
            );

        Quaternion desiredHipRotation =
            hipDelta * hip.rotation;

        hip.rotation =
            Quaternion.Slerp(
                hip.rotation,
                desiredHipRotation,
                weight
            );

        // --------------------------------------------------
        // 6. Hip 转动以后，Knee 已经被带到新的位置
        // --------------------------------------------------

        Vector3 currentKneePosition = knee.position;

        Vector3 desiredLowerDirection =
            (solvedTarget - currentKneePosition).normalized;

        // --------------------------------------------------
        // 7. 旋转 Knee，让 LowerLeg 指向 Target
        //
        // Local Y 仍然是骨骼方向
        // --------------------------------------------------

        Quaternion kneeDelta =
            Quaternion.FromToRotation(
                knee.TransformDirection(BoneAxis),
                desiredLowerDirection
            );

        Quaternion desiredKneeRotation =
            kneeDelta * knee.rotation;

        knee.rotation =
            Quaternion.Slerp(
                knee.rotation,
                desiredKneeRotation,
                weight
            );

        // --------------------------------------------------
        // 8. Foot 只调整朝向，不调整位置
        // --------------------------------------------------

        if (foot != null)
        {
            Quaternion targetRotation = target.rotation;

            foot.rotation =
                Quaternion.Slerp(
                    foot.rotation,
                    targetRotation,
                    weight
                );
        }
    }

    private void OnDrawGizmos()
    {
        if (hip == null || knee == null || foot == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(
            hip.position,
            knee.position
        );

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(
            knee.position,
            foot.position
        );

        if (target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(
                target.position,
                0.05f
            );
        }

        if (pole != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(
                knee.position,
                pole.position
            );
        }
    }
}
