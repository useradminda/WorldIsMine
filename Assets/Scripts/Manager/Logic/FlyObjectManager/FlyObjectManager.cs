
using System.Collections.Generic;
using ZTools;
public class FlyObjectManager : Singleton<FlyObjectManager>, IManager
{
    public WaitListTemplate<FlyObjectLogicBase> flyObjectList = new WaitListTemplate<FlyObjectLogicBase>(null);

    public void ManagerInit()
    {
       
    }

    public void ManagerUpdate(float dt)
    {
        //for (int i = 0; i < flyObjectList.Count; i++)
        //{
        //    flyObjectList[i].FlyObjectUpdate(dt);
        //}
    }

    public void ManagerLateUpdate(float dt)
    {
        flyObjectList.AddWaitingList();
    }

    public void ManagerRefuse()
    {
       
    }

    public void ManagerDestroy()
    {

    }

    // 增加一个Unit
    public FlyObjectLogicBase AddUnit(FlyObjectLogicBase flyObjet)
    {
        flyObjectList.Add(flyObjet);
        return flyObjet;
    }

    public FlyObjectLogicBase AddUnitImmediately(FlyObjectLogicBase flyObjet)
    {
        flyObjectList.AddCurrent(flyObjet);
        return flyObjet;
    }

}
