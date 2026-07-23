using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZTools;
public class OperateManager : MonoSingleton<OperateManager>
{
    public void UpdateInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            BattleEngine.Instance.CreateUnit(1, ECampType.Red, 100);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            BattleEngine.Instance.CreateUnit(1, ECampType.Blue, 100);

        }
    }
}
