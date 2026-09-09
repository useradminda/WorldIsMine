using UnityEngine;

public class ShakeComponent : MonoBehaviour
{
    private Transform shakeBody;

    [Header("抖动")]
    [SerializeField] private float shakeDuration = 0.18f;
    [SerializeField] private float shakeAmount = 0.05f;
    [SerializeField] private float shakeFrequency = 20f;

    private Vector3 originalPos;

    private float shakeTime;
    private bool shakeState;

    // 随机抖动方向
    private Vector3 shakeAxis;

    private void Awake()
    {
        shakeBody = transform.GetChild(0);
        originalPos = shakeBody.localPosition;
    }

    public void SetShake()
    {
        shakeTime = 0f;
        shakeState = true;
        // 每次受击随机一个局部抖动方向
        shakeAxis = new Vector3(Random.Range(0.7f, 1f), 0f, Random.Range(0.7f, 1f)).normalized;
    }

    private void Update()
    {
        if (!shakeState)
            return;

        shakeTime += Time.deltaTime;
        float t = shakeTime / shakeDuration;
        if (t >= 1f)
        {
            shakeState = false;
            shakeBody.localPosition = originalPos;
            return;
        }
        float decay = 1f - t;
        float wave = Mathf.Sin(shakeTime * shakeFrequency * Mathf.PI * 2f);
        Vector3 offset = decay * shakeAmount * wave * shakeAxis;
        shakeBody.localPosition = originalPos + offset;
    }
}


//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class ShakeComponent : MonoBehaviour
//{
//    private Transform shakeBody;
//    private int shakeCount = 10;
//    private int currentShakeCount = 0;
//    private bool shakeState;

//    private float shakeOffSet = 0.007f;

//    private int flag = -1;
//    private Vector3 _origianlPos = Vector3.zero;
//    Vector3 _offsetPos = Vector3.zero;
//    private void Awake()
//    {
//        shakeBody = transform.GetChild(0);
//        _origianlPos = shakeBody.transform.localPosition;
//    }

//    public void SetShake()
//    {
//        currentShakeCount = shakeCount;
//        flag = -1;
//        shakeState = true;
//    }

//    private void Update()
//    {
//        if(shakeState)
//        {
//            _offsetPos = _origianlPos + flag * shakeBody.transform.forward.normalized * shakeOffSet * currentShakeCount;                       
//            shakeBody.transform.localPosition = _offsetPos;
//            flag = -flag;
//            --currentShakeCount;
//            if(currentShakeCount < 0)
//            {
//                shakeState = false;
//            }
//        }
//    }
//}
