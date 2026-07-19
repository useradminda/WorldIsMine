using AillieoUtils;
using System.Collections.Generic;
using ZTools;

// KDTree 搜索性能优化
public class KDTreeManager : Singleton<KDTreeManager>, IManager
{
    // 等待添加的列表
    private List<KDInfo> waitingAddList = new List<KDInfo>();
    // 等待退出的列表
    private List<KDInfo> waitingRemoveList = new List<KDInfo>();
    // 查询结果
    private IEnumerable<KDInfo> queryResult = new List<KDInfo>();

    private KDTree<KDInfo> kdTree;
    protected KDTree<KDInfo> mKDTree
    {
        get
        {
            if (kdTree == null)
            {
                ManagerInit();
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
        waitingAddList.Clear();
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
        
    }


    public void AfterUpdate()
    {
        addWaitingKDInfoList();
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

    // 初始化KDTree
    public void InitKDTree(List<UnitBase> unitList)
    {
        for (int i = 0; i < unitList.Count; i++)
        {
            AddKDInfoToTree(unitList[i]);
        }
    }

    // 增加待入KDInfo
    public void AddWaitingKDInfo(UnitBase unit)
    {
        KDInfo kdInfo = mKDTree.applyAgent();
        kdInfo.SetUnit(unit);
        waitingAddList.Add(kdInfo);
    }

    // 添加KDInfo到KDTree
    public void AddKDInfoToTree(UnitBase unit)
    {
        KDInfo kdInfo = mKDTree.applyAgent();
        kdInfo.SetUnit(unit);
        mKDTree.Add(kdInfo);
    }

    // 添加KDInfo到KDTree
    public void AddKDInfoToTree(KDInfo kdInfo)
    {
        mKDTree.Add(kdInfo);
    }

    // 晚一帧加入
    private void addWaitingKDInfoList()
    {
        for(int i = 0; i < waitingAddList.Count; i++)
        {
            AddKDInfoToTree(waitingAddList[i]);
        }
        waitingAddList.Clear();
    }
}
