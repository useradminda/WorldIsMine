using Nebukam.ORCA;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System.Collections;
using System.Collections.Generic;
using System;
using Unity.Mathematics;
using UnityEngine;
using ZTools;
// 战斗引擎
public class BattleEngine : MonoSingleton<BattleEngine>
{
    private const float SpawnUnitPadding = 1f;
    // 场地碰撞配置
    public ObstacleConfig ObstacleConfigIns;
    // 出生位置
    public BornConfig BornConfigIns;

    [Header("交战示意特效")]
    public BattleClashEffectSettings ClashEffects = new BattleClashEffectSettings();
    [Header("士兵移动脚底烟雾")]
    public FootSmokeSettings FootSmoke = new FootSmokeSettings();

    [Header("Legacy Debug")]
    [SerializeField]
    [Tooltip("旧数字键刷兵：1=红方100个，2=蓝方100个。正常联调必须关闭。")]
    private bool enableLegacySpawnHotkeys = false;

    private bool battleInit = false;
    private bool battleFinished = false;
    public event Action<ECampType> BattleFinishedFunction;

    private void Awake()
    {
        gameObject.GetOrAddComponent<UIOperateManager>();
        battleInit = initBattle();
    }

    // Update is called once per frame
    void Update()
    {
        if (battleInit == false)
            return;
        if (battleFinished)
            return;
       
        UnitManager.Instance.ManagerUpdate(Time.deltaTime);
        RvoManager.Instance.ManagerUpdate(Time.deltaTime);
        MapCellManager.Instance.ManagerUpdate(Time.deltaTime);
        ProjectileJobManager.Instance.ManagerUpdate(Time.deltaTime);
        FlyObjectManager.Instance.ManagerUpdate(Time.deltaTime);
        GlobalSkillManager.Instance.ManagerUpdate(Time.deltaTime);

        UnitViewManager.Instance.ManagerUpdate(Time.deltaTime);
        BattleClashEffectManager.Instance.ManagerUpdate(Time.deltaTime);  
        OperateManager.Instance.UpdateInput();
    }

    private void LateUpdate()
    {
        if (battleInit == false)
            return;
        if (battleFinished)
            return;

        MapCellManager.Instance.ManagerLateUpdate(Time.deltaTime);
        RvoManager.Instance.ManagerLateUpdate(Time.deltaTime);
        UnitManager.Instance.ManagerLateUpdate(Time.deltaTime);
        ProjectileJobManager.Instance.ManagerLateUpdate(Time.deltaTime);
        FlyObjectManager.Instance.ManagerLateUpdate(Time.deltaTime);

        UnitViewManager.Instance.ManagerLateUpdate(Time.deltaTime);
        BattleClashEffectManager.Instance.ManagerLateUpdate(Time.deltaTime);
    }

    private void OnDestroy()
    {
        BattleClashEffectManager.Instance.ManagerDestroy();
        FootSmokeManager.Instance.ManagerDestroy();
        UnitManager.Instance.WallDestroyed -= OnWallDestroyed;
        GlobalSkillManager.Instance.ManagerDestroy();
        RvoManager.Instance.ManagerDestroy();
    }

    private void OnWallDestroyed(ECampType destroyedWallCamp)
    {
        if (battleFinished)
        {
            return;
        }

        battleFinished = true;
        RvoManager.Instance.ManagerDestroy();
        UnitManager.Instance.ManagerDestroy();
        BattleClashEffectManager.Instance.ManagerDestroy();
        FootSmokeManager.Instance.ManagerDestroy();
        ECampType winnerCamp = destroyedWallCamp == ECampType.Red
            ? ECampType.Blue
            : ECampType.Red;
        BattleFinishedFunction?.Invoke(winnerCamp);
        Debug.Log($"Battle finished. DestroyedWall={destroyedWallCamp}, Winner={winnerCamp}");
    }

    public void CreateWall()
    {
        if (BornConfigIns == null)
            return;

        CreateWallView(ECampType.Red);
        CreateWallView(ECampType.Blue);
    }

    public Vector3 GetCityDoorPoint(ECampType campType)
    {
        return BornConfigIns != null ? BornConfigIns.GetCityDoorPoint(campType) : Vector3.zero;
    }

    public Vector3 GetCityDoorForward(ECampType campType)
    {
        return BornConfigIns != null ? BornConfigIns.GetCityDoorForward(campType) : Vector3.forward;
    }

    private void CreateWallView(ECampType campType)
    {
        UnitLogicBase wallLogic = UnitManager.Instance.GetWallLogic(campType);
        if (wallLogic == null || wallLogic.SoliderCfg == null)
            return;

        Vector3 position = BornConfigIns.GetCityDoorPoint(campType);
        Vector3 forward = BornConfigIns.GetCityDoorForward(campType);
        if (forward.sqrMagnitude <= 0.0001f)
            forward = Vector3.forward;

        UnitView wallView = UnitViewFactory.CreateUnitView(
            wallLogic.SoliderCfg.prefab,
            position,
            forward,
            wallLogic);
        UnitViewManager.Instance.AddUnitViewImmediately(wallView, true);
        wallLogic.BindUnitView(wallView);
    }

    // 创建单位
    public void CreateUnit(int cfgId, ECampType campType, int count)
    {
        CreateUnitInternal(cfgId, campType, count, false, false, Vector3.zero, Vector3.zero);
    }

    public void CreateReinforcementUnits(int cfgId, ECampType campType, int count)
    {
        CreateUnitInternal(cfgId, campType, count, true, false, Vector3.zero, Vector3.zero);
    }

    public void CreateReinforcementUnits(int cfgId, ECampType campType, int count, Vector3 spawnPoint, Vector3 spawnForward)
    {
        CreateUnitInternal(cfgId, campType, count, true, true, spawnPoint, spawnForward);
    }

    private void CreateUnitInternal(int cfgId, ECampType campType, int count, bool isReinforcement,
        bool hasSpawnPoint, Vector3 spawnPoint, Vector3 spawnForward)
    {
        if (battleFinished == true)
            return;
        if (count <= 0)
        {
            return;
        }

        if (!UnitFactory.IsHero(cfgId) && UnitManager.Instance.HasWaitingRequest(campType))
        {
            UnitManager.Instance.EnqueueSpawn(cfgId, campType, count, isReinforcement,
                hasSpawnPoint, spawnPoint, spawnForward);
            return;
        }

        if (!UnitFactory.CanCreateUnitGroup(cfgId, campType, count, isReinforcement))
        {
            UnitManager.Instance.EnqueueSpawn(cfgId, campType, count, isReinforcement,
                hasSpawnPoint, spawnPoint, spawnForward);
            return;
        }

        CreateAvailableUnits(cfgId, campType, count, isReinforcement, hasSpawnPoint, spawnPoint, spawnForward);
    }

    private int CreateAvailableUnits(int cfgId, ECampType campType, int count, bool isReinforcement,
        bool hasSpawnPoint, Vector3 spawnPoint, Vector3 spawnForward)
    {
        int creatableCount = UnitFactory.GetCreatableUnitCount(cfgId, campType, count, isReinforcement);
        if (creatableCount <= 0)
        {
            return 0;
        }

        Vector3 forward = hasSpawnPoint && spawnForward.sqrMagnitude > 0.0001f
            ? spawnForward.normalized
            : BornConfigIns.GetForward(campType);
        Vector3 baseBornPoint = hasSpawnPoint ? spawnPoint : BornConfigIns.GetBornPoint(campType);
        SoliderCfg soliderCfg = SoliderCfgConfig.Ins.SearchById(cfgId);
        float unitRadius = soliderCfg != null ? soliderCfg.radius : 1.5f;
       
        for (int i = 0; i < creatableCount; i++)
        {
            Vector3 bornPoint = isReinforcement && hasSpawnPoint
                ? GetReinforcementCreatePoint(baseBornPoint, i, creatableCount, unitRadius)
                : GetCreatePoint(baseBornPoint, forward, i, creatableCount, unitRadius);

            UnitLogicBase unitLogic = UnitFactory.GetUnitCatch(cfgId, bornPoint, forward, campType, isReinforcement);

            if(unitLogic == null)
            { 
                unitLogic = UnitFactory.CreateUnit(cfgId, forward, campType, UnitManager.Instance.UnitList.Count, isReinforcement);
                if (unitLogic == null)
                {
                    return i;
                }
                Agent agent = UnitFactory.CreateAgent(bornPoint, forward, unitLogic.Prop.Radius, unitLogic.Prop.MaxSpeed, unitLogic.SoliderCfg.myLayer, unitLogic.SoliderCfg.ignoreLayer);
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

        return creatableCount;
    }

    // 获取创建位置点
    private Vector3 GetCreatePoint(
        Vector3 bornPoint,
        Vector3 forward,
        int unitIndex,
        int totalCount,
        float unitRadius)
    {
        float spacing = Mathf.Max(0.1f, unitRadius * 2f + SpawnUnitPadding);
        int columns = Mathf.Max(1, Mathf.FloorToInt(BattleDefine.AreaTotalWith / spacing));
        columns = Mathf.Min(columns, Mathf.Max(1, totalCount));
        int row = unitIndex / columns;
        int column = unitIndex % columns;
        int unitsInRow = Mathf.Min(columns, totalCount - row * columns);

        float centeredColumn = column - (unitsInRow - 1) * 0.5f;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 rowOffset = -forward * row * spacing;
        Vector3 columnOffset = right * centeredColumn * spacing;
        return bornPoint + rowOffset + columnOffset;
    }

    private Vector3 GetReinforcementCreatePoint(
        Vector3 center,
        int unitIndex,
        int totalCount,
        float unitRadius)
    {
        if (totalCount <= 0)
        {
            return center;
        }

        float spacing = Mathf.Max(0.1f, unitRadius * 2f + SpawnUnitPadding);
        float circumferenceRadius = spacing * totalCount / (Mathf.PI * 2f);
        float radius = Mathf.Max(spacing * 1.5f, circumferenceRadius);
        float angle = Mathf.PI * 2f * unitIndex / totalCount;
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
        return center + offset;
    }

    private bool initBattle()
    {
        if (BornConfigIns == null)
        {
            Debug.LogError("没有出生位置信息");
            return false;
        } 

        UnitManager.Instance.SetBornConfig(BornConfigIns);
        UnitManager.Instance.ManagerInit();
        RvoManager.Instance.ManagerInit();
        MapCellManager.Instance.ManagerInit();
        ProjectileJobManager.Instance.ManagerInit();
        FlyObjectManager.Instance.ManagerInit();

        GlobalSkillManager.Instance.ManagerInit();
        GlobalSkillManager.Instance.SetBornConfig(BornConfigIns);

        
        UnitManager.Instance.SetSpawnHandler(CreateAvailableUnits);
        UnitManager.Instance.WallDestroyed += OnWallDestroyed;

        if (ObstacleConfigIns == null)
        {
            Debug.LogError("没有边界障碍信息");
        }
        else
        {
            RvoManager.Instance.SetBorderInfo(ObstacleConfigIns.GetComponent<ObstacleConfig>().BorderList);
        }

        BattleClashEffectManager.Instance.SetSettings(ClashEffects);
        BattleClashEffectManager.Instance.ManagerInit();
        FootSmokeManager.Instance.SetSettings(FootSmoke);
        FootSmokeManager.Instance.ManagerInit();

        UnitViewManager.Instance.ManagerInit();
        CreateWall();

        return true;
    }
}
