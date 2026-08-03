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
                BattleEngine.Instance.CreateUnit(101, ECampType.Red, 50);            
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                BattleEngine.Instance.CreateUnit(102, ECampType.Red, 50);
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                BattleEngine.Instance.CreateUnit(109, ECampType.Red, 50);
            }
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                BattleEngine.Instance.CreateUnit(1001, ECampType.Red, 50);
            }
            return;
        }
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            BattleEngine.Instance.CreateUnit(201, ECampType.Blue, 50);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            BattleEngine.Instance.CreateUnit(202, ECampType.Blue, 50);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            BattleEngine.Instance.CreateUnit(204, ECampType.Blue, 50);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            BattleEngine.Instance.CreateUnit(2001, ECampType.Blue, 50);
        }
    }
}
