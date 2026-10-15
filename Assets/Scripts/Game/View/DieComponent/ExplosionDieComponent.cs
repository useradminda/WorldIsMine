using UnityEngine;

public class ExplosionDieComponent : DieBaseComponent
{
    [SerializeField] private Vector3 explosionPoint;
    [SerializeField] private float explosionSpeed = 17.5f;
    [SerializeField] private float upwardHeight = 17.5f;
    [SerializeField] private float duration = 1.5f;
    [SerializeField] private float rollSpeed = 0;
    [SerializeField] private float randomAngle = 10f;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 explosionDirection;
    private Vector3 rollAxis;
    private float elapsed;
    private bool playing;

    public void SetExplosionPoint(Vector3 point)
    {
        explosionPoint = point;
    }

    protected override void EnterDieState()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);
        explosionDirection = transform.position - explosionPoint;
        explosionDirection.y = 0f;
        if (explosionDirection.sqrMagnitude <= 0.0001f)
        {
            explosionDirection = Random.insideUnitSphere;
            explosionDirection.y = 0f;
        }
        explosionDirection.Normalize();
        rollAxis = Vector3.Cross(Vector3.up, explosionDirection).normalized;
        if (rollAxis.sqrMagnitude <= 0.0001f)
        {
            rollAxis = Vector3.right;
        }
        if (randomAngle > 0f)
        {
            explosionDirection = Quaternion.Euler(0f, Random.Range(-randomAngle, randomAngle), 0f) * explosionDirection;
        }
        PlayDieActionForDuration(duration);
        playing = true;
    }

    protected override void UpdateDie(float dt)
    {
        if (!playing)
        {
            return;
        }

        elapsed += dt;
        float normalizedTime = Mathf.Clamp01(elapsed / duration);
        float verticalOffset = upwardHeight * 4f * normalizedTime * (1f - normalizedTime);
        transform.position = startPosition + explosionDirection * explosionSpeed * elapsed + Vector3.up * verticalOffset;
        transform.rotation = Quaternion.AngleAxis(rollSpeed * elapsed, rollAxis) * startRotation;
        if (elapsed >= duration)
        {
            playing = false;
            CycleUnitView();
        }
    }
}
