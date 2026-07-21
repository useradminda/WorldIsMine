using AillieoUtils;
using System.Collections.Generic;
using ZTools;

// KDTree 搜索性能优化
public class KDTreeManager : Singleton<KDTreeManager>, IManager
{
    public WaitListTemplate<KDInfo> KDInfoList;
    // 查询结果
    private IEnumerable<KDInfo> queryResult = new List<KDInfo>();

    private KDTree<KDInfo> kdTree;
    protected KDTree<KDInfo> mKDTree
    {
        get
        {
            if (kdTree == null)
            {
                kdTree = new KDTree<KDInfo>();
                kdTree.initPool();
                //queryResult = kdTree.QueryInRange(Vector2.zero, 1);
            }
            return kdTree;
        }
    }
    // 初始化
    public void ManagerInit()
    {
        KDInfoList = new WaitListTemplate<KDInfo>((KDInfo a) => mKDTree.Add(a));
    }

    public void ManagerUpdate()
    {
        if (mKDTree != null)
        {
            mKDTree.Rebuild();
        }
    }

    public void ManagerLateUpdate()
    {
        KDInfoList.AddWaitingList();
    }


    public void ManagerRefuse()
    {
        mKDTree.recycleAgent();
        mKDTree.Clear();
        kdTree = null;
    }

    public void ManagerDestroy()
    {
       
    }

    // 增加待入KDInfo
    public void AddWaitingKDInfo(UnitLogicBase unit)
    {
        KDInfo kdInfo = mKDTree.applyAgent();
        kdInfo.SetUnit(unit);
        KDInfoList.Add(kdInfo);
    }
}
