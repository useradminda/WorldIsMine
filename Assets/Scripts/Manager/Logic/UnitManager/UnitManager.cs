using Nebukam.ORCA;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using ZTools;

public class UnitManager : Singleton<UnitManager>, IManager
{
    public WaitListTemplate<UnitLogicBase> UnitList = new WaitListTemplate<UnitLogicBase>(null);
  
    public void ManagerInit()
    {

    }

    public void ManagerUpdate()
    {
        for (int i = 0; i < UnitList.Count; i++)
        {
            UnitList.DataList[i].UnitUpdate();
        }
    }

    public void ManagerLateUpdate()
    {
        UnitList.AddWaitingList();
    }

    public void ManagerRefuse()
    {

    }
    public void ManagerDestroy()
    {

    }

    // 增加一个Unit
    public UnitLogicBase AddUnit(UnitLogicBase agent)
    {
        UnitList.Add(agent);
        return agent;
    }
}
