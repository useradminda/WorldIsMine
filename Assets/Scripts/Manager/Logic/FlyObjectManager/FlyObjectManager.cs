
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
        //flyObjectList.AddWaitingList();
    }

    public void ManagerRefuse()
    {
       
    }

    public void ManagerDestroy()
    {

    }

    public FlyObjectLogicBase AddFlyUnitImmediately(FlyObjectLogicBase flyObjet)
    {
        flyObjectList.AddImmediately(flyObjet);
        return flyObjet;
    }

    public void RemoveFlyUnitImmediately(FlyObjectLogicBase flyObject)
    {
        flyObjectList.RemoveImmediately(flyObject);
    }

}
