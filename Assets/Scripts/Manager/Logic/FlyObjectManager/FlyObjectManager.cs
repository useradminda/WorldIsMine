
using System.Collections.Generic;
using ZTools;
public class FlyObjectManager : Singleton<FlyObjectManager>, IManager
{
    private int uIndex = 0;
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
        ++uIndex;
        FlyObjectDic.Add(uIndex, flyObjet);
        return uIndex;
    }

    public void RemoveFlyUnitImmediately(FlyObjectLogicBase flyObject)
    {
        FlyObjectList.RemoveImmediately(flyObject);
        if (FlyObjectDic.ContainsKey(flyObject.UIdex))
            FlyObjectDic.Remove(flyObject.UIdex);
    }

    public FlyObjectLogicBase SearchByUIndex(int uIndex)
    {
        if (FlyObjectDic.ContainsKey(uIndex))
        {
            return FlyObjectDic[uIndex];
        }
        return null;
    }

}
