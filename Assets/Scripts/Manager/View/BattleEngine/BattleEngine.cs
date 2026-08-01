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
        FlyObjectManager.Instance.ManagerUpdate(Time.deltaTime);
        RvoManager.Instance.ManagerUpdate(Time.deltaTime);
        KDTreeManager.Instance.ManagerUpdate(Time.deltaTime);

        UnitViewManager.Instance.ManagerUpdate(Time.deltaTime);

        OperateManager.Instance.UpdateInput();
    }

    private void LateUpdate()
    {
        if (battleInit == false)
            return;
        UnitManager.Instance.ManagerLateUpdate(Time.deltaTime);
        FlyObjectManager.Instance.ManagerUpdate(Time.deltaTime);
        RvoManager.Instance.ManagerLateUpdate(Time.deltaTime);
        KDTreeManager.Instance.ManagerLateUpdate(Time.deltaTime);

        UnitViewManager.Instance.ManagerLateUpdate(Time.deltaTime);
    }


    // 创建单位
    public void CreateUnit(int id, ECampType campType, int count)
    {
        if (!battleInit)
        {
            Debug.LogError(
                $"[Battle][Spawn] BattleEngine is not initialized. Camp={campType}, " +
                $"UnitConfigId={id}, Count={count}");
            return;
        }
        if (count <= 0)
        {
            Debug.LogWarning(
                $"[Battle][Spawn] Ignored non-positive count. Camp={campType}, " +
                $"UnitConfigId={id}, Count={count}");
            return;
        }

        Vector3 forward = BornConfigIns.GetForward(campType);
        Vector3 baseBornPoint = BornConfigIns.GetBornPoint(campType);
        Debug.Log(
            $"[Battle][Spawn] Camp={campType}, UnitConfigId={id}, Count={count}, " +
            $"BasePoint={baseBornPoint}, Forward={forward}");

        for (int i = 0; i < count; i++)
        {
            Vector3 bornPoint = GetCreatePoint(
                baseBornPoint,
                forward,
                i,
                count);

            UnitLogicBase unitLogic = UnitFactory.CreateUnit(id, bornPoint, forward, campType);
            UnitManager.Instance.AddUnit(unitLogic);
            RvoManager.Instance.AddAgent(unitLogic.Agenter);
            KDTreeManager.Instance.AddWaitingKDInfo(unitLogic);

            UnitView unityView = UnitViewFactory.CreateUnitView(unitLogic.SoliderCfg.prefab, bornPoint, forward, unitLogic);
            UnitViewManager.Instance.AddUnitView(unityView);
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
        if (!BornConfigIns.HasDistinctSpawnPoints(1f, out string spawnError))
        {
            Debug.LogError($"[Battle][Spawn] Invalid spawn configuration: {spawnError}");
            return false;
        }

        Debug.Log(
            $"[Battle][Spawn] Ready. Red={BornConfigIns.GetBornPoint(ECampType.Red)}, " +
            $"Blue={BornConfigIns.GetBornPoint(ECampType.Blue)}");

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
