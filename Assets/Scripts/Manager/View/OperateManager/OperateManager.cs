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
            BattleEngine.Instance.CreateUnit(ECampType.Red, 100, 1, "Soliders/Soldier1_1");
        }
    }
}
