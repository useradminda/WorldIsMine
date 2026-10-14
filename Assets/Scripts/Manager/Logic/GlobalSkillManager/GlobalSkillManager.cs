using System.Collections.Generic;
using UnityEngine;
using ZTools;

public class GlobalSkillManager : Singleton<GlobalSkillManager>, IManager
{
    private const int GlobalAttackBuffId = 2;
    public const int GlobalStoneProjectileCount = 3;
    private const float GlobalStoneOriginHeight = 50f;
    private const float GlobalStoneLaunchInterval = 1f;
    private const float GlobalStoneFlightDistance = 160f;
    public const int GlobalArrowMaxArrowCount = 100;
    private const float GlobalArrowOriginDistance = 100f;
    private const float GlobalArrowRandomDistance = 40f;
    public const int GlobalDragonBreathFlyObjectCfgId = 1004;
    public const int GlobalDragonBreathSkillCfgId = 1004;

    private readonly List<CampBuffState> campBuffStateList = new List<CampBuffState>();

    private BornConfig bornConfig;
    private bool initialized;

    private class CampBuffState
    {
        public ECampType CampType;
        public int BuffId;
        public float RemainTime;
    }

    /// <summary>初始化全局技能管理器。</summary>
    public void ManagerInit()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
    }

    /// <summary>更新全军Buff剩余时间。</summary>
    public void ManagerUpdate(float dt)
    {
        UpdateCampBuff(dt);
    }

    /// <summary>设置战场出生配置。</summary>
    public void SetBornConfig(BornConfig config)
    {
        bornConfig = config;
    }

    /// <summary>给指定阵营当前全部士兵添加攻击Buff。</summary>
    public bool TryCastCampAttackBuff(ECampType campType, int buffId, int victoryPointCost)
    {
        return BattleScoreManager.Instance.TryCastGlobalSkill(
            victoryPointCost,
            () => ApplyCampAttackBuff(campType, GlobalAttackBuffId));
    }

    /// <summary>给指定阵营当前全部士兵添加攻击Buff，不扣除胜点。</summary>
    public bool ApplyCampAttackBuff(ECampType campType, int buffId)
    {
        buffId = GlobalAttackBuffId;
        BuffCfg buffCfg = BuffCfgConfig.Ins.SearchById(buffId);
        if (buffCfg == null)
        {
            Debug.LogError($"全局攻击Buff配置不存在，BuffId={buffId}");
            return false;
        }

        if ((BuffDefine.EBuffType)buffCfg.tyep != BuffDefine.EBuffType.AddAtk)
        {
            Debug.LogError($"全局攻击技能需要攻击Buff，BuffId={buffId}，Type={buffCfg.tyep}");
            return false;
        }

        CampBuffState state = GetCampBuffState(campType, buffId);
        if (state == null)
        {
            state = new CampBuffState
            {
                CampType = campType,
                BuffId = buffId
            };
            campBuffStateList.Add(state);
        }
        state.RemainTime = buffCfg.time;

        for (int i = 0; i < UnitManager.Instance.UnitList.Count; i++)
        {
            UnitLogicBase unitLogic = UnitManager.Instance.UnitList[i];
            if (CanApplyCampBuff(unitLogic, campType))
            {
                unitLogic.AddBuff(buffId, state.RemainTime);
            }
        }

        return true;
    }

    /// <summary>消耗胜点并同时释放三发全局火球。</summary>
    public bool TryCastGlobalStone(ECampType casterCamp, int flyObjectCfgId, int skillCfgId, int damage, int victoryPointCost)
    {
        return BattleScoreManager.Instance.TryCastGlobalSkill(
            victoryPointCost,
            () => ApplyGlobalStone(casterCamp, flyObjectCfgId, skillCfgId, damage));
    }

    /// <summary>创建全局火球批次，并同时发射三发火球。</summary>
    public bool ApplyGlobalStone(ECampType casterCamp, int flyObjectCfgId, int skillCfgId, int damage)
    {
        if (bornConfig == null || damage <= 0)
        {
            return false;
        }

        FlyObjectCfg flyObjectCfg = FlyObjectCfgConfig.Ins.SearchById(flyObjectCfgId);
        SkillCfg skillCfg = SkillCfgConfig.Ins.SearchById(skillCfgId);
        UnitLogicBase attacker = UnitManager.Instance.GetPlayerLogic(casterCamp);
        List<UnitLogicBase> enemyList = GetEnemyUnits(casterCamp);
        if (flyObjectCfg == null || flyObjectCfg.flyType != "globalStone" || attacker == null || skillCfg == null || enemyList.Count == 0)
        {
            Debug.LogError($"全局火球配置不存在、缺少攻击者或战场上没有敌人，FlyObjectCfgId={flyObjectCfgId}，SkillCfgId={skillCfgId}");
            return false;
        }

        int actualDamage = attacker.GetAttackDamage(skillCfg.skillDamageRate);
        if (actualDamage <= 0)
        {
            return false;
        }

        int projectileCount = Mathf.Min(GlobalStoneProjectileCount, enemyList.Count);
        for (int i = 0; i < projectileCount; i++)
        {
            int randomEnemyIndex = Random.Range(i, enemyList.Count);
            UnitLogicBase temp = enemyList[i];
            enemyList[i] = enemyList[randomEnemyIndex];
            enemyList[randomEnemyIndex] = temp;
        }

        Vector3 forward = bornConfig.GetForward(casterCamp).normalized;
        SkillLogicBase skillLogic = new SkillLogicBase(attacker, skillCfg);
        int createdCount = 0;
        for (int i = 0; i < projectileCount; i++)
        {
            UnitLogicBase enemy = enemyList[i];
            Vector3 target = enemy.CurPos;
            Vector3 origin = target - forward * GlobalStoneFlightDistance + Vector3.up * GlobalStoneOriginHeight;
            GlobalStoneFlyObject stoneFlyObject = UnitFactory.CreateFlyObjectLogic(flyObjectCfgId, origin, target, attacker, null, enemy, skillLogic, actualDamage) as GlobalStoneFlyObject;
            if (stoneFlyObject != null)
            {
                stoneFlyObject.SetLaunchDelay(i * GlobalStoneLaunchInterval);
                createdCount++;
            }
        }

        return createdCount > 0;
    }

    public bool TryCastGlobalArrow(ECampType casterCamp, int flyObjectCfgId, int skillCfgId, int victoryPointCost)
    {
        return BattleScoreManager.Instance.TryCastGlobalSkill(victoryPointCost, () => ApplyGlobalArrow(casterCamp, flyObjectCfgId, skillCfgId));
    }

    public bool ApplyGlobalArrow(ECampType casterCamp, int flyObjectCfgId, int skillCfgId)
    {
        if (bornConfig == null)
        {
            return false;
        }

        FlyObjectCfg flyObjectCfg = FlyObjectCfgConfig.Ins.SearchById(flyObjectCfgId);
        SkillCfg skillCfg = SkillCfgConfig.Ins.SearchById(skillCfgId);
        UnitLogicBase attacker = UnitManager.Instance.GetPlayerLogic(casterCamp);
        List<UnitLogicBase> enemyList = GetEnemyUnits(casterCamp);
        if (flyObjectCfg == null || attacker == null || skillCfg == null || enemyList.Count == 0)
        {
            return false;
        }

        int arrowCount = Mathf.Min(GlobalArrowMaxArrowCount, enemyList.Count);
        for (int i = 0; i < arrowCount; i++)
        {
            int randomEnemyIndex = Random.Range(i, enemyList.Count);
            UnitLogicBase temp = enemyList[i];
            enemyList[i] = enemyList[randomEnemyIndex];
            enemyList[randomEnemyIndex] = temp;
        }

        SkillLogicBase skillLogic = new SkillLogicBase(attacker, skillCfg);
        GlobalArrowFlyObject.DamageContext damageContext = new GlobalArrowFlyObject.DamageContext();
        int createdCount = 0;
        for (int i = 0; i < arrowCount; i++)
        {
            UnitLogicBase enemy = enemyList[i];
            Vector3 target = enemy.CurPos;
            Vector3 arrowOriginDirection = (Vector3.up - bornConfig.GetForward(casterCamp).normalized).normalized;
            float arrowOriginDistance = GlobalArrowOriginDistance + Random.Range(-GlobalArrowRandomDistance, GlobalArrowRandomDistance);
            Vector3 origin = target + arrowOriginDirection * arrowOriginDistance;
            GlobalArrowFlyObject arrowFlyObject = UnitFactory.CreateFlyObjectLogic(flyObjectCfgId, origin, target, attacker, null, enemy, skillLogic, 0) as GlobalArrowFlyObject;
            if (arrowFlyObject != null)
            {
                arrowFlyObject.SetDamageContext(damageContext);
                createdCount++;
            }
        }

        return createdCount > 0;
    }


    /// <summary>全局技能管理器不需要LateUpdate处理。</summary>
    public bool TryCastGlobalDragonBreath(ECampType casterCamp, int victoryPointCost)
    {
        return BattleScoreManager.Instance.TryCastGlobalSkill(victoryPointCost, () => ApplyGlobalDragonBreath(casterCamp, GlobalDragonBreathFlyObjectCfgId, GlobalDragonBreathSkillCfgId));
    }

    public bool ApplyGlobalDragonBreath(ECampType casterCamp, int flyObjectCfgId, int skillCfgId)
    {
        if (bornConfig == null)
        {
            return false;
        }

        FlyObjectCfg flyObjectCfg = FlyObjectCfgConfig.Ins.SearchById(flyObjectCfgId);
        SkillCfg skillCfg = SkillCfgConfig.Ins.SearchById(skillCfgId);
        UnitLogicBase attacker = UnitManager.Instance.GetPlayerLogic(casterCamp);
        List<UnitLogicBase> enemyList = GetEnemyUnits(casterCamp);
        if (flyObjectCfg == null || flyObjectCfg.flyType != "line" || skillCfg == null || attacker == null || enemyList.Count == 0)
        {
            return false;
        }

        int actualDamage = attacker.GetAttackDamage(skillCfg.skillDamageRate);
        if (actualDamage <= 0)
        {
            return false;
        }

        ECampType targetCamp = casterCamp == ECampType.Red ? ECampType.Blue : ECampType.Red;
        Vector3 origin = bornConfig.GetBornPoint(casterCamp);
        Vector3 target = bornConfig.GetBornPoint(targetCamp);
        SkillLogicBase skillLogic = new SkillLogicBase(attacker, skillCfg);
        LineFlyObject lineFlyObject = UnitFactory.CreateFlyObjectLogic(flyObjectCfgId, origin, target, attacker, null, null, skillLogic, actualDamage) as LineFlyObject;
        if (lineFlyObject == null)
        {
            return false;
        }
        return true;
    }

    public void ManagerLateUpdate(float dt)
    {
    }

    /// <summary>暂停时预留的管理器接口。</summary>
    public void ManagerRefuse()
    {
    }

    /// <summary>注销事件并清理尚未完成的全局技能任务。</summary>
    public void ManagerDestroy()
    {
        initialized = false;

        campBuffStateList.Clear();
        bornConfig = null;
    }

    /// <summary>更新全军Buff的全局有效时间。</summary>
    private void UpdateCampBuff(float dt)
    {
        for (int i = campBuffStateList.Count - 1; i >= 0; i--)
        {
            campBuffStateList[i].RemainTime -= dt;
            if (campBuffStateList[i].RemainTime <= 0f)
            {
                campBuffStateList.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 创建一发火球，并让其从本方出生线飞向敌方随机落点。
    /// 三发火球使用相同的前进方向，因此轨迹保持平行。
    /// </summary>
    #if false
    /* obsolete legacy stone spawner removed */
    private bool SpawnGlobalStone(GlobalStoneState state)
    {
        UnitLogicBase enemy = GetRandomEnemy(state.CampType);
        Vector3 forward = bornConfig.GetForward(state.CampType).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        float segmentWidth = BattleDefine.AreaTotalWith / 3f;
        float segmentCenter = -BattleDefine.AreaTotalWith * 0.5f + segmentWidth * (state.FiredCount + 0.5f);
        Vector3 origin = bornConfig.GetBornPoint(state.CampType) + right * segmentCenter - forward * GlobalStoneOriginBackwardDistance;
        origin.y += GlobalStoneOriginHeight;
        Vector3 middleBornPoint = (bornConfig.GetBornPoint(ECampType.Red) + bornConfig.GetBornPoint(ECampType.Blue)) * 0.5f;
        float targetDepth;
        if (enemy == null)
        {
            targetDepth = middleBornPoint.z + Random.Range(-GlobalStoneFallbackDepthRandomDistance, GlobalStoneFallbackDepthRandomDistance);
        }
        else
        {
            if (state.HasTargetDepth == false)
            {
                state.TargetDepth = enemy.CurPos.z;
                state.HasTargetDepth = true;
            }
            targetDepth = state.TargetDepth;
        }
        float depthDistance = Mathf.Abs(targetDepth - origin.z);
        float travelDistance = Mathf.Max(GlobalStoneMinTravelDistance, depthDistance);
        Vector3 target = origin + forward * travelDistance;
        target.y = middleBornPoint.y;
        if (depthDistance >= GlobalStoneMinTravelDistance)
        {
            target.z = targetDepth;
        }

        UnitFactory.CreateFlyObjectLogic(state.FlyObjectCfgId, origin, target, state.Attacker, null, enemy, state.SkillLogic, state.Damage);

        state.FiredCount++;
        return true;
    }

    #endif

    private List<UnitLogicBase> GetEnemyUnits(ECampType casterCamp)
    {
        List<UnitLogicBase> enemyList = new List<UnitLogicBase>();
        for (int i = 0; i < UnitManager.Instance.UnitList.Count; i++)
        {
            UnitLogicBase unitLogic = UnitManager.Instance.UnitList[i];
            if (unitLogic == null || unitLogic.IsDead || unitLogic.CampType == casterCamp)
            {
                continue;
            }

            enemyList.Add(unitLogic);
        }

        return enemyList;
    }

    /// <summary>随机取得一个存活的敌方单位。</summary>
    /// <summary>查找相同阵营和配置的全军Buff状态。</summary>
    private CampBuffState GetCampBuffState(ECampType campType, int buffId)
    {
        for (int i = 0; i < campBuffStateList.Count; i++)
        {
            CampBuffState state = campBuffStateList[i];
            if (state.CampType == campType && state.BuffId == buffId)
            {
                return state;
            }
        }
        return null;
    }

    /// <summary>判断士兵是否可以获得指定阵营的全军Buff。</summary>
    private bool CanApplyCampBuff(UnitLogicBase unitLogic, ECampType campType)
    {
        return unitLogic != null
            && !unitLogic.IsDead
            && unitLogic.CampType == campType
            && unitLogic.UnitType == EUnitType.Solider;
    }
}
