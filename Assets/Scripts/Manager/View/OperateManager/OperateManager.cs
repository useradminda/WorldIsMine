using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZTools;
public class OperateManager : MonoSingleton<OperateManager>
{
    public void UpdateInput()
    {
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                BattleEngine.Instance.CreateUnit(1, ECampType.Red, 50);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                BattleEngine.Instance.CreateUnit(2, ECampType.Red, 50);
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                BattleEngine.Instance.CreateUnit(3, ECampType.Red, 50);
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            BattleEngine.Instance.CreateUnit(1, ECampType.Blue, 50);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            BattleEngine.Instance.CreateUnit(2, ECampType.Blue, 50);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            BattleEngine.Instance.CreateUnit(3, ECampType.Blue, 50);
        }
    }
}
