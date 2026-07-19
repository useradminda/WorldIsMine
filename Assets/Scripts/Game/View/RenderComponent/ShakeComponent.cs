using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShakeComponent : MonoBehaviour
{
    private Transform shakeBody;
    private int shakeCount = 10;
    private int currentShakeCount = 0;
    private bool shakeState;

    private float shakeOffSet = 0.007f;

    private int flag = -1;
    private Vector3 _origianlPos = Vector3.zero;
    Vector3 _offsetPos = Vector3.zero;
    private void Awake()
    {
        shakeBody = transform.GetChild(0);
        _origianlPos = shakeBody.transform.localPosition;
    }

    public void SetShakeBody()
    {
        currentShakeCount = shakeCount;
        flag = -1;
        shakeState = true;
    }

    private void Update()
    {
        if(shakeState)
        {
            _offsetPos = _origianlPos + flag * shakeBody.transform.forward.normalized * shakeOffSet * currentShakeCount;                       
            shakeBody.transform.localPosition = _offsetPos;
            flag = -flag;
            --currentShakeCount;
            if(currentShakeCount < 0)
            {
                shakeState = false;
            }
        }
    }
}
