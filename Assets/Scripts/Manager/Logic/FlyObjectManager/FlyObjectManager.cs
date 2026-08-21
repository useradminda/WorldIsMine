
using System.Collections.Generic;
using ZTools;
public class FlyObjectManager : Singleton<FlyObjectManager>, IManager
{
    private int flyUIndex = 0;
    public WaitListTemplate<FlyObjectLogicBase> FlyObjectList = new WaitListTemplate<FlyObjectLogicBase>(null);

    public Dictionary<int, FlyObjectLogicBase> FlyObjectDic = new Dictionary<int, FlyObjectLogicBase>();
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

    public int AddFlyUnitImmediately(FlyObjectLogicBase flyObjet)
    {
        FlyObjectList.AddImmediately(flyObjet);
        ++flyUIndex;
        FlyObjectDic.Add(flyUIndex, flyObjet);
        return flyUIndex;
    }

    public void RemoveFlyUnitImmediately(FlyObjectLogicBase flyObject)
    {
        FlyObjectList.RemoveImmediately(flyObject);
        if (FlyObjectDic.ContainsKey(flyObject.FlyUIndex))
            FlyObjectDic.Remove(flyObject.FlyUIndex);
    }

    public FlyObjectLogicBase SearchByFlyUIndex(int flyUIndex)
    {
        if (FlyObjectDic.ContainsKey(flyUIndex))
        {
            return FlyObjectDic[flyUIndex];
        }
        return null;
    }

}
