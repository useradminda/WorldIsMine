
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
        //UnitList.AddWaitingList();
    }

    public void ManagerRefuse()
    {

    }
    public void ManagerDestroy()
    {

    }

    /// <summary>
    /// 回收当前场景中的全部单位表现对象。
    /// </summary>
    public void ClearAll()
    {
        for (int i = UnitList.Count - 1; i >= 0; i--)
        {
            UnitView unitView = UnitList[i];
            if (unitView != null)
            {
                UnitViewFactory.RemoveUnitView(unitView);
            }
        }
    }

    public void AddUnitViewImmediately(UnitView unitView, bool immediately)
    {
        if ( immediately)
        {
            UnitList.AddImmediately(unitView);
            return;
        }
        UnitList.AddImmediately(unitView);
    }

    public void RemoveSwapBackUnitView(UnitView unitView)
    {
        UnitList.RemoveSwapBack(unitView);
    }
}
