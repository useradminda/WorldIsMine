using UnityEngine;
using ZTools;

/// <summary>相机震动控制器。震动通过相机局部位置和局部角度叠加，不修改 CameraRig。</summary>
public class CameraShakeControl : MonoSingleton<CameraShakeControl>
{
    public enum ShakeMode
    {
        Horizontal,
        Vertical,
        RandomPosition,
        AngleX,
        AngleY,
        AngleZ,
        RandomAngle,
        PositionAndAngle
    }

    [Header("默认参数")]
    [SerializeField] private float defaultDuration = 0.25f;
    [SerializeField] private float defaultPositionAmplitude = 0.25f;
    [SerializeField] private float defaultAngleAmplitude = 3f;

    private Camera targetCamera;
    private Vector3 appliedPositionOffset;
    private Vector3 appliedAngleOffset;
    private Vector3 positionAmplitude;
    private Vector3 angleAmplitude;
    private float noiseTime;
    private float shakeDuration;
    private float shakeElapsed;
    private bool useDecay;
    private ShakeMode mode;

    public bool IsShaking => shakeDuration > 0f && shakeElapsed < shakeDuration;

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            targetCamera = CameraControl.Instance != null
                ? CameraControl.Instance.MainCamera
                : Camera.main;
        }
        if (targetCamera == null) return;

        // 先移除上一帧的叠加值，避免 CameraControl 的缩放/跟随基准被震动偏移污染。
        targetCamera.transform.localPosition -= appliedPositionOffset;
        targetCamera.transform.localEulerAngles -= appliedAngleOffset;
        appliedPositionOffset = Vector3.zero;
        appliedAngleOffset = Vector3.zero;

        if (!IsShaking)
        {
            shakeDuration = 0f;
            return;
        }

        shakeElapsed += Time.deltaTime;
        float damping = useDecay
            ? 1f - Mathf.Clamp01(shakeElapsed / shakeDuration)
            : 1f;

        noiseTime += Time.deltaTime * 40f;
        Vector3 positionOffset = Vector3.zero;
        Vector3 angleOffset = Vector3.zero;
        switch (mode)
        {
            case ShakeMode.Horizontal:
                positionOffset.x = Mathf.Sin(noiseTime) * positionAmplitude.x;
                break;
            case ShakeMode.Vertical:
                positionOffset.y = Mathf.Sin(noiseTime) * positionAmplitude.y;
                break;
            case ShakeMode.RandomPosition:
                positionOffset = RandomInsideAmplitude(positionAmplitude);
                break;
            case ShakeMode.AngleX:
                angleOffset.x = Mathf.Sin(noiseTime) * angleAmplitude.x;
                break;
            case ShakeMode.AngleY:
                angleOffset.y = Mathf.Sin(noiseTime) * angleAmplitude.y;
                break;
            case ShakeMode.AngleZ:
                angleOffset.z = Mathf.Sin(noiseTime) * angleAmplitude.z;
                break;
            case ShakeMode.RandomAngle:
                angleOffset = RandomInsideAmplitude(angleAmplitude);
                break;
            case ShakeMode.PositionAndAngle:
                positionOffset = RandomInsideAmplitude(positionAmplitude);
                angleOffset = RandomInsideAmplitude(angleAmplitude);
                break;
        }

        appliedPositionOffset = positionOffset * damping;
        appliedAngleOffset = angleOffset * damping;
        targetCamera.transform.localPosition += appliedPositionOffset;
        targetCamera.transform.localEulerAngles += appliedAngleOffset;
    }

    public void Shake(ShakeMode shakeMode, float duration, Vector3 positionAmplitude, Vector3 angleAmplitude, bool useDecay = false)
    {
        mode = shakeMode;
        this.positionAmplitude = positionAmplitude;
        this.angleAmplitude = angleAmplitude;
        noiseTime = 0f;
        shakeDuration = Mathf.Max(0f, duration);
        shakeElapsed = 0f;
        this.useDecay = useDecay;
    }

    public void Shake(ShakeMode shakeMode, float duration, float amplitude, bool useDecay = false)
    {
        Shake(
            shakeMode,
            duration,
            new Vector3(amplitude, amplitude, amplitude),
            new Vector3(amplitude, amplitude, amplitude),
            useDecay);
    }

    public void ShakeHorizontal(float amplitude = -1f, float duration = -1f, bool useDecay = false)
    {
        Shake(ShakeMode.Horizontal, duration > 0f ? duration : defaultDuration,
            new Vector3(amplitude > 0f ? amplitude : defaultPositionAmplitude, 0f, 0f), Vector3.zero, useDecay);
    }

    public void ShakeVertical(float amplitude = -1f, float duration = -1f, bool useDecay = false)
    {
        Shake(ShakeMode.Vertical, duration > 0f ? duration : defaultDuration,
            new Vector3(0f, amplitude > 0f ? amplitude : defaultPositionAmplitude, 0f), Vector3.zero, useDecay);
    }

    public void ShakeRandom(float amplitude = -1f, float duration = -1f, bool useDecay = false)
    {
        float value = amplitude > 0f ? amplitude : defaultPositionAmplitude;
        Shake(ShakeMode.RandomPosition, duration > 0f ? duration : defaultDuration,
            new Vector3(value, value, value), Vector3.zero, useDecay);
    }

    public void ShakeAngleX(float amplitude = -1f, float duration = -1f, bool useDecay = false)
    {
        float value = amplitude > 0f ? amplitude : defaultAngleAmplitude;
        Shake(ShakeMode.AngleX, duration > 0f ? duration : defaultDuration,
            Vector3.zero, new Vector3(value, 0f, 0f), useDecay);
    }

    public void ShakeAngle(float amplitude = -1f, float duration = -1f, bool useDecay = false)
    {
        float value = amplitude > 0f ? amplitude : defaultAngleAmplitude;
        Shake(ShakeMode.RandomAngle, duration > 0f ? duration : defaultDuration,
            Vector3.zero, new Vector3(value, value, value), useDecay);
    }

    public void StopShake()
    {
        shakeDuration = 0f;
        shakeElapsed = 0f;
    }

    private static Vector3 RandomInsideAmplitude(Vector3 amplitude)
    {
        return new Vector3(
            UnityEngine.Random.Range(-amplitude.x, amplitude.x),
            UnityEngine.Random.Range(-amplitude.y, amplitude.y),
            UnityEngine.Random.Range(-amplitude.z, amplitude.z));
    }
}
