using System;
using UnityEngine;

/// <summary>
/// 战场中线投石控制器。
/// 单位在自身更新时通知越线事件，本组件只负责冷却和投石位置，不遍历全场单位。
/// </summary>
public class BattleCenterStoneController : MonoBehaviour
{
    private const float GlobalStoneOriginHeight = 25f;
    private const float GlobalStoneFlightDistance = 80f;

    [Serializable]
    private class CampStoneConfig
    {
        [InspectorName("投石技能配置 ID")]
        public int skillId;
        [InspectorName("触发间隔（秒）"), Min(0.1f)]
        public float triggerInterval = 60f;
    }

    public static BattleCenterStoneController Instance { get; private set; }

    [Header("投石配置")]
    [SerializeField] private bool enableCenterStone = true;
    [SerializeField] private int globalStoneFlyObjectId = 1001;
    [SerializeField] private CampStoneConfig redStone = new CampStoneConfig();
    [SerializeField] private CampStoneConfig blueStone = new CampStoneConfig();

    [Header("中线判定")]
    [SerializeField, Min(0f)] private float centerCrossPadding = 0.5f;

    private BattleEngine battleEngine;
    private Vector3 centerPoint;
    private Vector3 redForward;
    private Vector3 blueForward;
    private float redRemainTime;
    private float blueRemainTime;
    private bool redPending;
    private bool bluePending;
    private Vector3 redPendingPosition;
    private Vector3 bluePendingPosition;
    private bool initialized;
    private bool battleFinished;

    public void Initialize(BattleEngine engine)
    {
        if (battleEngine != null)
        {
            battleEngine.BattleFinishedFunction -= OnBattleFinished;
        }

        battleEngine = engine;
        initialized = false;
        battleFinished = false;
        redRemainTime = 0f;
        blueRemainTime = 0f;
        redPending = false;
        bluePending = false;

        if (battleEngine == null || battleEngine.BornConfigIns == null || !enableCenterStone)
        {
            return;
        }

        battleEngine.BattleFinishedFunction += OnBattleFinished;
        Vector3 redPoint = battleEngine.BornConfigIns.GetBornPoint(ECampType.Red);
        Vector3 bluePoint = battleEngine.BornConfigIns.GetBornPoint(ECampType.Blue);
        centerPoint = (redPoint + bluePoint) * 0.5f;
        redForward = battleEngine.BornConfigIns.GetForward(ECampType.Red);
        blueForward = battleEngine.BornConfigIns.GetForward(ECampType.Blue);
        redForward.y = 0f;
        blueForward.y = 0f;
        redForward = redForward.sqrMagnitude > 0.0001f ? redForward.normalized : Vector3.forward;
        blueForward = blueForward.sqrMagnitude > 0.0001f ? blueForward.normalized : -Vector3.forward;
        initialized = true;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        if (!initialized || battleFinished || battleEngine == null || !battleEngine.IsBattleStarted)
        {
            return;
        }

        redRemainTime = Mathf.Max(0f, redRemainTime - Time.deltaTime);
        blueRemainTime = Mathf.Max(0f, blueRemainTime - Time.deltaTime);
        TryProcessPending(ECampType.Red);
        TryProcessPending(ECampType.Blue);
    }

    /// <summary>
    /// 由角色更新流程调用，每个单位只会成功通知一次。
    /// </summary>
    public void CheckUnitCrossing(UnitLogicBase unit)
    {
        // 英雄和普通士兵都属于 EUnitType.Solider，都会参与越过中线判定。
        if (!initialized || battleFinished || unit == null || unit.IsDead || unit.UnitType != EUnitType.Solider
            || unit.HasCrossedBattleCenter)
        {
            return;
        }

        Vector3 offset = unit.CurPos - centerPoint;
        offset.y = 0f;
        Vector3 campForward = unit.CampType == ECampType.Red ? redForward : blueForward;
        float depth = Vector3.Dot(offset, campForward);
        bool crossed = depth >= centerCrossPadding;
        if (!crossed)
        {
            return;
        }

        unit.MarkCrossedBattleCenter();
        NotifyCrossing(unit.CampType, unit.CurPos);
    }

    private void NotifyCrossing(ECampType campType, Vector3 position)
    {
        if (campType == ECampType.Red)
        {
            if (redRemainTime > 0f)
            {
                redPending = true;
                redPendingPosition = position;
                return;
            }

            TryCastStone(campType, position);
            return;
        }

        if (blueRemainTime > 0f)
        {
            bluePending = true;
            bluePendingPosition = position;
            return;
        }

        TryCastStone(campType, position);
    }

    private void TryProcessPending(ECampType campType)
    {
        if (campType == ECampType.Red && redRemainTime <= 0f && redPending)
        {
            redPending = false;
            TryCastStone(campType, redPendingPosition);
        }
        else if (campType == ECampType.Blue && blueRemainTime <= 0f && bluePending)
        {
            bluePending = false;
            TryCastStone(campType, bluePendingPosition);
        }
    }

    private void TryCastStone(ECampType campType, Vector3 targetPosition)
    {
        CampStoneConfig config = campType == ECampType.Red ? redStone : blueStone;
        if (config == null || config.skillId <= 0 || globalStoneFlyObjectId <= 0)
        {
            return;
        }

        bool castSuccess = ApplySingleGlobalStoneAtPosition(
            campType,
            config.skillId,
            targetPosition);
        if (!castSuccess)
        {
            return;
        }

        float interval = Mathf.Max(0.1f, config.triggerInterval);
        if (campType == ECampType.Red)
        {
            redRemainTime = interval;
        }
        else
        {
            blueRemainTime = interval;
        }
    }

    private bool ApplySingleGlobalStoneAtPosition(
        ECampType casterCamp,
        int skillId,
        Vector3 targetPosition)
    {
        FlyObjectCfg flyObjectCfg = FlyObjectCfgConfig.Ins.SearchById(globalStoneFlyObjectId);
        SkillCfg skillCfg = SkillCfgConfig.Ins.SearchById(skillId);
        UnitLogicBase attacker = UnitManager.Instance.GetPlayerLogic(casterCamp);
        if (flyObjectCfg == null || flyObjectCfg.flyType != "globalStone"
            || skillCfg == null || attacker == null)
        {
            return false;
        }

        int actualDamage = attacker.GetAttackDamage(skillCfg.skillDamageRate);

        Vector3 forward = battleEngine.BornConfigIns.GetForward(casterCamp).normalized;
        Vector3 origin = targetPosition - forward * GlobalStoneFlightDistance;
        origin.y = targetPosition.y + GlobalStoneOriginHeight;
        SkillLogicBase skillLogic = new SkillLogicBase(attacker, skillCfg);
        GlobalStoneFlyObject stoneFlyObject = FlyObjectFactory.CreateFlyObjectLogic(
            globalStoneFlyObjectId,
            origin,
            targetPosition,
            attacker,
            null,
            null,
            skillLogic,
            actualDamage) as GlobalStoneFlyObject;
        return stoneFlyObject != null;
    }

    private void OnBattleFinished(ECampType winnerCamp)
    {
        battleFinished = true;
    }

    private void OnDestroy()
    {
        if (battleEngine != null)
        {
            battleEngine.BattleFinishedFunction -= OnBattleFinished;
        }
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
