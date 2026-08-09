
using ZTools;

public class UnitManager : Singleton<UnitManager>, IManager
{
    public WaitListTemplate<UnitLogicBase> UnitList = new WaitListTemplate<UnitLogicBase>(null);
  
    public void ManagerInit()
    {

    }

    public void ManagerUpdate(float dt)
    {
        for (int i = 0; i < UnitList.Count; i++)
        {
            UnitList[i].UnitUpdate(dt);
        }
    }

    public void ManagerLateUpdate(float dt)
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
    public UnitLogicBase AddUnit(UnitLogicBase unit)
    {
        UnitList.Add(unit);
        return unit;
    }

    public UnitLogicBase AddUnitImmediately(UnitLogicBase unit)
    {
        UnitList.AddCurrent(unit);
        return unit;
    }
}
