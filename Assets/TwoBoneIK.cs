using UnityEngine;

public class TwoBoneIK : MonoBehaviour
{
    [Header("Joints")]
    public Transform hip;
    public Transform knee;
    public Transform foot;

    [Header("Target")]
    public Transform target;

    [Header("IK Plane")]
    public Transform pole;

    float upperLength;
    float lowerLength;

    void Start()
    {
        upperLength = Vector3.Distance(hip.position, knee.position);
        lowerLength = Vector3.Distance(knee.position, foot.position);
    }

    void LateUpdate()
    {
        Solve();
    }

    public void Solve()
    {
        Vector3 rootPos = hip.position;
        Vector3 targetPos = target.position;

        Vector3 toTarget = targetPos - rootPos;

        float distance = toTarget.magnitude;

        float minDistance = Mathf.Abs(upperLength - lowerLength) + 0.001f;
        float maxDistance = upperLength + lowerLength - 0.001f;

        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        Vector3 direction = toTarget.normalized;

        // IK 平面的方向
        Vector3 poleDir = pole.position - rootPos;

        Vector3 planeNormal = Vector3.Cross(direction, poleDir).normalized;

        // 如果 pole 与 target 在同一直线上
        if (planeNormal.sqrMagnitude < 0.0001f)
        {
            planeNormal = Vector3.up;
        }

        // 重新计算真正的弯曲方向
        Vector3 bendDirection = Vector3.Cross(planeNormal, direction).normalized;

        // 余弦定理
        float cosHip =
            (upperLength * upperLength + distance * distance - lowerLength * lowerLength) /
            (2f * upperLength * distance);

        cosHip = Mathf.Clamp(cosHip, -1f, 1f);

        float hipAngle = Mathf.Acos(cosHip) * Mathf.Rad2Deg;

        // 上腿方向
        Quaternion hipRotation =
            Quaternion.LookRotation(direction, bendDirection);

        hipRotation =
            Quaternion.AngleAxis(-hipAngle, planeNormal) * hipRotation;

        hip.rotation = hipRotation;

        // 强制 Knee 指向 Target
        Vector3 kneeToTarget = targetPos - knee.position;

        if (kneeToTarget.sqrMagnitude > 0.0001f)
        {
            knee.rotation =
                Quaternion.LookRotation(kneeToTarget.normalized, bendDirection);
        }
    }
}