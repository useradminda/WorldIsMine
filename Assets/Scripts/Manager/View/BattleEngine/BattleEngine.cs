using Nebukam.ORCA;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using ZTools;
// 战斗引擎
public class BattleEngine : MonoSingleton<BattleEngine>
{
    // 场地碰撞配置
    public ObstacleConfig ObstacleConfigIns;
    // 出生位置
    public BornConfig BornConfigIns;

    [Header("Legacy Debug")]
    [SerializeField]
    [Tooltip("旧数字键刷兵：1=红方100个，2=蓝方100个。正常联调必须关闭。")]
    private bool enableLegacySpawnHotkeys = false;

    private bool battleInit = false;

    private void Awake()
    {
        battleInit = initBattle();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (battleInit == false)
            return;
        UnitManager.Instance.ManagerUpdate(Time.deltaTime);
        ProjectileJobManager.Instance.ManagerUpdate(Time.deltaTime);
        RvoManager.Instance.ManagerUpdate(Time.deltaTime);
        MapCellManager.Instance.ManagerUpdate(Time.deltaTime);

        UnitViewManager.Instance.ManagerUpdate(Time.deltaTime);

        OperateManager.Instance.UpdateInput();
    }

    private void LateUpdate()
    {
        if (battleInit == false)
            return;
        UnitManager.Instance.ManagerLateUpdate(Time.deltaTime);
        ProjectileJobManager.Instance.ManagerLateUpdate(Time.deltaTime);
        RvoManager.Instance.ManagerLateUpdate(Time.deltaTime);
        MapCellManager.Instance.ManagerLateUpdate(Time.deltaTime);

        UnitViewManager.Instance.ManagerLateUpdate(Time.deltaTime);
    }

    private void OnDestroy()
    {
        RvoManager.Instance.ManagerDestroy();
    }

    // 创建单位
    public void CreateUnit(int cfgId, ECampType campType, int count)
    {
        Vector3 forward = BornConfigIns.GetForward(campType);
        Vector3 baseBornPoint = BornConfigIns.GetBornPoint(campType);
       
        for (int i = 0; i < count; i++)
        {
            Vector3 bornPoint = GetCreatePoint( baseBornPoint, forward, i, count);

            bool create = false;
            UnitLogicBase unitLogic = UnitFactory.GetUnitCatch(cfgId, bornPoint, forward, campType);

            if(unitLogic == null)
            { 
                unitLogic = UnitFactory.CreateUnit(cfgId, bornPoint, forward, campType, UnitManager.Instance.UnitList.Count);
                Agent agent = UnitFactory.CreateAgent(bornPoint, forward, unitLogic.Prop.Radius, unitLogic.Prop.MaxSpeed);
                unitLogic.BindAgent(agent);

                UnitManager.Instance.AddUnitImmediately(unitLogic);
                RvoManager.Instance.AddAgentImmediately(unitLogic.Agenter);
                MapCellManager.Instance.AddUnit(unitLogic.Agenter.pos, unitLogic.CampTypeInt);
            }
           
            UnitView unityView = UnitViewFactory.CreateUnitView(unitLogic.SoliderCfg.prefab, bornPoint, forward, unitLogic);
            UnitViewManager.Instance.AddUnitViewImmediately(unityView, true);
            unitLogic.BindUnitView(unityView);

            unitLogic.InitStateMachine();
            unitLogic.StateMachine.ChangeState(EStateTyep.Move);
        }
    }

    // 获取创建位置点
    private Vector3 GetCreatePoint(
        Vector3 bornPoint,
        Vector3 forward,
        int unitIndex,
        int totalCount)
    {
        int maxColumns = Mathf.Max(1, BattleDefine.FootManWithCount);
        int columns = Mathf.Min(maxColumns, Mathf.Max(1, totalCount));
        int row = unitIndex / columns;
        int column = unitIndex % columns;
        int unitsInRow = Mathf.Min(columns, totalCount - row * columns);

        float horizontalSpacing = BattleDefine.AreaTotalWith / maxColumns;
        float centeredColumn = column - (unitsInRow - 1) * 0.5f;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 rowOffset = -forward * row * BattleDefine.FootManHeightSegDis;
        Vector3 columnOffset = right * centeredColumn * horizontalSpacing;
        return bornPoint + rowOffset + columnOffset;
    }

    private bool initBattle()
    {
        if (BornConfigIns == null)
        {
            Debug.LogError("没有出生位置信息");
            return false;
        } 

        UnitManager.Instance.ManagerInit();
        RvoManager.Instance.ManagerInit();
        MapCellManager.Instance.ManagerInit();
        ProjectileJobManager.Instance.ManagerInit();

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
