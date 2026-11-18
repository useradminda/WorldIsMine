
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
        for (int i = FlyObjectList.Count - 1; i >= 0; i--)
        {
            FlyObjectList[i].FlyObjectUpdate(dt);
        }
    }

    public void ManagerLateUpdate(float dt)
    {
        for (int i = FlyObjectList.Count - 1; i >= 0; i--)
        {
            FlyObjectList[i].FlyObjectLateUpdate(dt);
        }
    }

    public void ManagerRefuse()
    {
       
    }

    public void ManagerDestroy()
    {
        ClearAll();
    }

    /// <summary>
    /// 清理所有飞行器逻辑、表现和索引。
    /// </summary>
    public void ClearAll()
    {
        while (FlyObjectList.Count > 0)
        {
            FlyObjectLogicBase flyObject = FlyObjectList[FlyObjectList.Count - 1];
            flyObject?.ClearForBattle();
            if (flyObject != null && FlyObjectList.DataList.Contains(flyObject))
            {
                FlyObjectList.RemoveImmediately(flyObject);
            }
        }

        FlyObjectList.DataList.Clear();
        FlyObjectDic.Clear();
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
