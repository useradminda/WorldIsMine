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
    // 场地碰撞配置
    public ObstacleConfig ObstacleConfigIns;
    // 出生位置
    public BornConfig BornConfigIns;
    // 战场自动出兵控制器
    public BattleAutoSpawnController AutoSpawnController;
    // 战场中线投石控制器
    public BattleCenterStoneController CenterStoneController;

    [Header("交战示意特效")]
    public BattleClashEffectSettings ClashEffects = new BattleClashEffectSettings();
    [Header("士兵移动脚底烟雾")]
    public FootSmokeSettings FootSmoke = new FootSmokeSettings();

    [Header("Legacy Debug")]
    [SerializeField]
    [Tooltip("旧数字键刷兵：1=红方100个，2=蓝方100个。正常联调必须关闭。")]
    private bool enableLegacySpawnHotkeys = false;

    private bool battleInit = false;
    private bool battleStarted = false;
    private bool battleFinished = false;
    private List<UnitLogicBase> collectingCreatedUnits;
    public event Action<ECampType> BattleFinishedFunction;
    public bool IsBattleStarted => battleStarted;
    public bool IsBattleFinished => battleFinished;

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
        if (battleFinished || !battleStarted)
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
        if (battleFinished || !battleStarted)
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
        ClearBattle();
        UnitManager.Instance.WallDestroyed -= OnWallDestroyed;
    }

    private void OnWallDestroyed(ECampType destroyedWallCamp)
    {
        if (battleFinished)
        {
            return;
        }

        ECampType winnerCamp = destroyedWallCamp == ECampType.Red
            ? ECampType.Blue
            : ECampType.Red;
        Victory(winnerCamp);
        Debug.Log($"Battle finished. DestroyedWall={destroyedWallCamp}, Winner={winnerCamp}");
    }

    /// <summary>
    /// 开始战斗并激活预先创建的单位逻辑。
    /// </summary>
    public bool StartBattle()
    {
        if (!battleInit || battleFinished || battleStarted)
        {
            return false;
        }

        battleStarted = true;
        for (int i = 0; i < UnitManager.Instance.UnitList.Count; i++)
        {
            UnitLogicBase unitLogic = UnitManager.Instance.UnitList[i];
            if (unitLogic == null || unitLogic.IsDead || unitLogic.UnitType == EUnitType.Wall)
            {
                continue;
            }

            ActivateUnit(unitLogic);
        }

        return true;
    }

    /// <summary>
    /// 激活单个单位的战斗状态，骑士进入冲锋，其余单位进入移动。
    /// </summary>
    private void ActivateUnit(UnitLogicBase unitLogic)
    {
        if (unitLogic.SoliderCfg != null && unitLogic.SoliderCfg.unitType == 3)
        {
            unitLogic.StateMachine.ChangeState(EStateTyep.KnightCharge);
        }
        else
        {
            unitLogic.StateMachine.ChangeState(EStateTyep.Move);
        }
    }

    /// <summary>
    /// 记录胜利阵营并通知胜利界面、自动出兵和其他战斗表现系统。
    /// </summary>
    public void Victory(ECampType winnerCamp)
    {
        if (battleFinished)
        {
            return;
        }

        battleFinished = true;
        BattleFinishedFunction?.Invoke(winnerCamp);
    }

    /// <summary>
    /// 检查双方城门状态并在城门被摧毁时触发胜利流程。
    /// </summary>
    public void JudgeBattleResult()
    {
        if (battleFinished)
        {
            return;
        }

        UnitLogicBase redWall = UnitManager.Instance.GetWallLogic(ECampType.Red);
        UnitLogicBase blueWall = UnitManager.Instance.GetWallLogic(ECampType.Blue);
        if (redWall != null && redWall.IsDead)
        {
            Victory(ECampType.Blue);
        }
        else if (blueWall != null && blueWall.IsDead)
        {
            Victory(ECampType.Red);
        }
    }

    /// <summary>
    /// 清理本场战斗的逻辑、表现、搜索、RVO、飞行器和投射物数据。
    /// </summary>
    public void ClearBattle()
    {
        if (!battleInit)
        {
            return;
        }

        battleStarted = false;
        battleFinished = true;
        FlyObjectManager.Instance.ClearAll();
        ProjectileJobManager.Instance.ManagerDestroy();
        GlobalSkillManager.Instance.ManagerDestroy();
        MapCellManager.Instance.ManagerDestroy();
        UnitManager.Instance.ClearBattleUnits();
        BattleHUDPanel.Instance?.ClearAll();
        UnitViewManager.Instance.ClearAll();
        BattleScoreManager.Instance.Reset();
        RvoManager.Instance.ManagerDestroy();
        BattleClashEffectManager.Instance.ManagerDestroy();
        FootSmokeManager.Instance.ManagerDestroy();
    }

    /// <summary>
    /// 清理上一局并重新初始化整场战斗，供重新开始按钮调用。
    /// </summary>
    public bool RestartBattle()
    {
        if (!battleInit)
        {
            return false;
        }

        ClearBattle();

        UnitManager.Instance.SetBornConfig(BornConfigIns);
        UnitManager.Instance.ManagerInit();
        RvoManager.Instance.ManagerInit();
        MapCellManager.Instance.ManagerInit();
        MapCellManager.Instance.RebuildFromUnitList();
        ProjectileJobManager.Instance.ManagerInit();
        FlyObjectManager.Instance.ManagerInit();
        GlobalSkillManager.Instance.ManagerInit();
        GlobalSkillManager.Instance.SetBornConfig(BornConfigIns);
        UnitManager.Instance.SetSpawnHandler(CreateAvailableUnits);
        UnitManager.Instance.WallDestroyed -= OnWallDestroyed;
        UnitManager.Instance.WallDestroyed += OnWallDestroyed;

        if (ObstacleConfigIns != null)
        {
            RvoManager.Instance.SetBorderInfo(ObstacleConfigIns.GetComponent<ObstacleConfig>().BorderList);
        }

        BattleClashEffectManager.Instance.SetSettings(ClashEffects);
        BattleClashEffectManager.Instance.ManagerInit();
        FootSmokeManager.Instance.SetSettings(FootSmoke);
        FootSmokeManager.Instance.ManagerInit();
        UnitViewManager.Instance.ManagerInit();
        CreateWall();

        AutoSpawnController?.Initialize(this);
        CenterStoneController?.Initialize(this);
        battleStarted = false;
        battleFinished = false;
        return true;
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
    public List<UnitLogicBase> CreateUnit(int cfgId, ECampType campType, int count)
    {
        collectingCreatedUnits = new List<UnitLogicBase>();
        CreateUnitInternal(cfgId, campType, count, false, false, Vector3.zero, Vector3.zero, null, null);
        List<UnitLogicBase> result = collectingCreatedUnits;
        collectingCreatedUnits = null;
        return result;
    }

    /// <summary>?????????????</summary>
    public List<UnitLogicBase> CreateUnit(int cfgId, ECampType campType, int count, BattlePlayer ownerPlayer)
    {
        collectingCreatedUnits = new List<UnitLogicBase>();
        CreateUnitInternal(cfgId, campType, count, false, false, Vector3.zero, Vector3.zero, null, ownerPlayer);
        List<UnitLogicBase> result = collectingCreatedUnits;
        collectingCreatedUnits = null;
        return result;
    }

    /// <summary>
    /// 在指定出生点创建一个普通单位，供自动出兵系统使用。
    /// </summary>
    public void CreateUnitAtPoint(int cfgId, ECampType campType, Vector3 spawnPoint, Vector3 spawnForward)
    {
        CreateUnitInternal(cfgId, campType, 1, false, true, spawnPoint, spawnForward, null, null);
    }

    /// <summary>在指定出生点为指定玩家召唤一个单位。</summary>
    public void CreateUnitAtPoint(int cfgId, ECampType campType, Vector3 spawnPoint, Vector3 spawnForward, BattlePlayer ownerPlayer)
    {
        CreateUnitInternal(cfgId, campType, 1, false, true, spawnPoint, spawnForward, null, ownerPlayer);
    }

    /// <summary>
    /// 按召唤技能提供的出生点列表创建援军。
    /// </summary>
    public void CreateReinforcementUnits(int cfgId, ECampType campType, List<Vector3> spawnPoints, Vector3 spawnForward)
    {
        CreateUnitInternal(cfgId, campType, spawnPoints.Count, true, true, Vector3.zero, spawnForward, spawnPoints, null);
    }

    /// <summary>使用指定出生点列表为指定玩家召唤援军。</summary>
    public void CreateReinforcementUnits(int cfgId, ECampType campType, List<Vector3> spawnPoints, Vector3 spawnForward, BattlePlayer ownerPlayer)
    {
        CreateUnitInternal(cfgId, campType, spawnPoints.Count, true, true, Vector3.zero, spawnForward, spawnPoints, ownerPlayer);
    }

    private void CreateUnitInternal(int cfgId, ECampType campType, int count, bool isReinforcement,
        bool hasSpawnPoint, Vector3 spawnPoint, Vector3 spawnForward, List<Vector3> spawnPoints = null, BattlePlayer ownerPlayer = null)
    {
        if (battleFinished == true)
            return;
        if (count <= 0)
        {
            return;
        }

        if (!isReinforcement && !UnitFactory.IsHero(cfgId) && UnitManager.Instance.HasWaitingRequest(campType))
        {
            UnitManager.Instance.EnqueueSpawn(cfgId, campType, count, isReinforcement,
                hasSpawnPoint, spawnPoint, spawnForward, spawnPoints, ownerPlayer);
            return;
        }

        if (!isReinforcement && !UnitFactory.CanCreateUnitGroup(cfgId, campType, count, isReinforcement))
        {
            UnitManager.Instance.EnqueueSpawn(cfgId, campType, count, isReinforcement,
                hasSpawnPoint, spawnPoint, spawnForward, spawnPoints, ownerPlayer);
            return;
        }

        CreateAvailableUnits(cfgId, campType, count, isReinforcement, hasSpawnPoint, spawnPoint, spawnForward, spawnPoints, ownerPlayer);
    }

    private int CreateAvailableUnits(int cfgId, ECampType campType, int count, bool isReinforcement,
        bool hasSpawnPoint, Vector3 spawnPoint, Vector3 spawnForward, List<Vector3> spawnPoints = null, BattlePlayer ownerPlayer = null)
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
            bool isHero = UnitFactory.IsHero(cfgId);
            Vector3 bornPoint = isReinforcement
                ? spawnPoints[i]
                : isHero
                    ? GetHeroCreatePoint(campType, forward, unitRadius)
                    : GetCreatePoint(baseBornPoint, forward, i, creatableCount, unitRadius);
            bornPoint = RvoManager.Instance.ClampPositionInsideBorder(bornPoint, unitRadius);

            UnitLogicBase unitLogic = UnitFactory.GetUnitCatch(cfgId, bornPoint, forward, campType, isReinforcement, ownerPlayer);

            if(unitLogic == null)
            { 
                unitLogic = UnitFactory.CreateUnit(cfgId, forward, campType, UnitManager.Instance.UnitList.Count, isReinforcement, ownerPlayer);
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
            else
            {
                UnitManager.Instance.RegisterReusedUnit(unitLogic);
                RvoManager.Instance.AddAgentImmediately(unitLogic.Agenter);
            }

            UnitView unityView = UnitViewFactory.CreateUnitView(unitLogic.SoliderCfg.prefab, bornPoint, forward, unitLogic);
            UnitViewManager.Instance.AddUnitViewImmediately(unityView, true);
            unitLogic.BindUnitView(unityView);
            collectingCreatedUnits?.Add(unitLogic);
            if (isHero)
            {
                BattleHUDPanel.Instance?.CreateHeroHud(unitLogic);
            }

            unitLogic.InitStateMachine();
            if (!battleStarted)
            {
                unitLogic.StateMachine.ChangeState(EStateTyep.Idle);
                unitLogic.UnitView.ActionFlowComponent.PlayAction(EActionType.wait);
            }
            else if (unitLogic.SoliderCfg != null && unitLogic.SoliderCfg.unitType == 3)
            {
                unitLogic.StateMachine.ChangeState(EStateTyep.KnightCharge);
            }
            else
            {
                unitLogic.StateMachine.ChangeState(EStateTyep.Move);
            }
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
        float spacing = Mathf.Max(0.1f, unitRadius * 2f + BattleDefine.SpawnUnitPadding);
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

    /// <summary>
    /// 在阵营城门逻辑位置生成英雄，并在战场横向合法范围内随机分配 X 轴位置。
    /// </summary>
    private Vector3 GetHeroCreatePoint(ECampType campType, Vector3 forward, float unitRadius)
    {
        Vector3 logicDoorPoint = BornConfigIns.GetCityLogicPoint(campType);
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        float maxLateralOffset = Mathf.Min(12f, Mathf.Max(0f, BattleDefine.AreaTotalWith * 0.5f - unitRadius - BattleDefine.SpawnUnitPadding));
        float randomLateralOffset = UnityEngine.Random.Range(-maxLateralOffset, maxLateralOffset);
        return logicDoorPoint + right * randomLateralOffset;
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

        if (AutoSpawnController == null)
        {
            AutoSpawnController = GetComponent<BattleAutoSpawnController>();
        }
        if (AutoSpawnController != null)
        {
            AutoSpawnController.Initialize(this);
        }

        if (CenterStoneController == null)
        {
            CenterStoneController = GetComponent<BattleCenterStoneController>();
        }
        if (CenterStoneController != null)
        {
            CenterStoneController.Initialize(this);
        }

        return true;
    }
}
