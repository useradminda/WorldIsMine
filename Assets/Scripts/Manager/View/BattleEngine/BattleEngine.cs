using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nebukam.ORCA;
using Unity.Mathematics;
using ZTools;
// 战斗引擎
public class BattleEngine : MonoSingleton<BattleEngine>
{
    // 场地碰撞配置
    public ObstacleConfig ObstacleConfigIns;
    // 出生位置
    public BornConfig BornConfigIns;

    private bool battleInit = false;

    private void Awake()
    {
        initBattle();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        UnitManager.Instance.ManagerUpdate();
        RvoManager.Instance.ManagerUpdate();
        KDTreeManager.Instance.ManagerUpdate();

        UnitViewManager.Instance.ManagerUpdate();
    }

    private void LateUpdate()
    {
        UnitManager.Instance.ManagerLateUpdate();
        RvoManager.Instance.ManagerLateUpdate();
        KDTreeManager.Instance.ManagerLateUpdate();

        UnitViewManager.Instance.ManagerLateUpdate();
    }


    // 创建单位
    public void CreateUnit(ECampType campType, int count, float radius, string prefab)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 forward = BornConfigIns.GetForward(campType);
            Vector3 bornPoint = BornConfigIns.GetBornPoint(campType);
            bornPoint = getCreatePoint(bornPoint, forward, count);       

            UnitLogicBase unitLogic = UnitFactory.CreateUnit(bornPoint, forward, radius, campType);
            UnitManager.Instance.AddUnit(unitLogic);
            RvoManager.Instance.AddAgent(unitLogic.Agenter);
            KDTreeManager.Instance.AddWaitingKDInfo(unitLogic);

            UnitView unityView = UnitViewFactory.CreateUnitView(prefab, bornPoint, forward, unitLogic);
            UnitViewManager.Instance.AddUnitView(unityView);
        }
    }

    // 获取创建位置点
    private Vector3 getCreatePoint(Vector3 bornPoint, Vector3 forward, int count)
    {
        float soliderWithClipDis = BattleDefine.AreaTotalWith / BattleDefine.FootManWithCount;
        int withLineIndex = count / BattleDefine.FootManWithCount;
        float z = bornPoint.z + (-forward.z) * (withLineIndex - 1) * BattleDefine.FootManHeightSegDis;

        float x = 0;
        int index = count / 2;
        int leftOrRightFlagValue = count % 2;
        int flagValue = leftOrRightFlagValue == 0 ? 1 : -1;   
        x = bornPoint.x + flagValue * index * soliderWithClipDis;
        return new Vector3(x, 0, z);        
    }

    private bool initBattle()
    {
        if (BornConfigIns == null)
        {
            Debug.LogError("没有出生位置信息");
            return false;
        } 

        UnitManager.Instance.ManagerInit();
        KDTreeManager.Instance.ManagerInit();

        RvoManager.Instance.ManagerInit();
        if (ObstacleConfigIns == null)
        {
            Debug.LogError("没有边界障碍信息");
        }
        else
        {
            RvoManager.Instance.SetBorderInfo(ObstacleConfigIns.GetComponent<ObstacleConfig>().BorderList);
        }

        UnitViewManager.Instance.ManagerInit();

        return true;
    }
}
