
using ZTools;

public class UnitViewManager : MonoSingleton<UnitViewManager>, IManager
{
    public WaitListTemplate<UnitView> UnitList = new WaitListTemplate<UnitView>(null);

    public void ManagerInit()
    {

    }

    public void ManagerUpdate(float dt)
    {
        for (int i = 0; i < UnitList.Count; i++)
        {
            UnitList[i].ViewUpdate(dt);
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

    public void AddUnitView(UnitView unitView, bool immediately)
    {
        if ( immediately)
        {
            UnitList.AddCurrent(unitView);
            return;
        }
        UnitList.Add(unitView);
    }

}
