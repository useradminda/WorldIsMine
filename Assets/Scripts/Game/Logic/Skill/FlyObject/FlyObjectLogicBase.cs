using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;

// 飞行物
public class FlyObjectLogicBase
{
    // 使用状态
    private bool useState = false;
    public bool UseState => useState;


    public FlyObjectLogicBase()
    {
        
    }

    // 设置飞行物的信息
    public void SetFlyObjectInfo(int flyObjectCfgId, float3 oriPos, float3 tarPos, UnitLogicBase atkUnitLogic, List<UnitLogicBase> beAtkUnitLogic, SkillLogicBase skillLogic)
    {

    }

    public void FlyObjectUpdate()
    {

    }

    public void Reset()
    {
        useState = false;
    }


    private void touchTarUnit()
    {

    }
}
