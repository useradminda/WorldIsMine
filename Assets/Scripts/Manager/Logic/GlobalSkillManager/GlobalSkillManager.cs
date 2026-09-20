using System.Collections.Generic;
using UnityEngine;
using ZTools;

public class GlobalSkillManager : Singleton<GlobalSkillManager>, IManager
{
    private readonly List<CampBuffState> campBuffStateList = new List<CampBuffState>();
    private readonly List<ArrowRainState> arrowRainStateList = new List<ArrowRainState>();

    private BornConfig bornConfig;
    private bool initialized;

    private class CampBuffState
    {
        public ECampType CampType;
        public int BuffId;
        public float RemainTime;
    }

    private class ArrowRainState
    {
        public string Prefab;
        public GameObject Effect;
        public ParticleSystem[] Particles;
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

    /// <summary>更新全军Buff剩余时间和箭雨特效回收计时。</summary>
    public void ManagerUpdate(float dt)
    {
        UpdateCampBuff(dt);
        UpdateArrowRain(dt);
    }

    /// <summary>设置战场配置，供箭雨取得本方进攻朝向。</summary>
    public void SetBornConfig(BornConfig config)
    {
        bornConfig = config;
    }

    /// <summary>给指定阵营当前全部士兵添加攻击Buff。</summary>
    public bool TryCastCampAttackBuff(ECampType campType, int buffId, int victoryPointCost)
    {
        return BattleScoreManager.Instance.TryCastGlobalSkill(
            victoryPointCost,
            () => ApplyCampAttackBuff(campType, buffId));
    }

    /// <summary>给指定阵营当前全部士兵添加攻击Buff，不扣除胜点。</summary>
    public bool ApplyCampAttackBuff(ECampType campType, int buffId)
    {
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

    /// <summary>消耗胜点，播放整体箭雨特效并结算一次范围伤害。</summary>
    public bool TryCastArrowRain(
        ECampType casterCamp,
        int flyObjectCfgId,
        int damage,
        float damageRadius,
        float effectDuration,
        string dieType,
        int victoryPointCost)
    {
        return BattleScoreManager.Instance.TryCastGlobalSkill(
            victoryPointCost,
            () => ApplyArrowRain(casterCamp, flyObjectCfgId, damage, damageRadius, effectDuration, dieType));
    }

    /// <summary>随机选择敌方位置，播放一个完整特效，并立即结算一次范围伤害。</summary>
    public bool ApplyArrowRain(
        ECampType casterCamp,
        int flyObjectCfgId,
        int damage,
        float damageRadius,
        float effectDuration,
        string dieType)
    {
        if (bornConfig == null || damage <= 0 || damageRadius <= 0f || effectDuration <= 0f
            || float.IsNaN(damageRadius) || float.IsInfinity(damageRadius)
            || float.IsNaN(effectDuration) || float.IsInfinity(effectDuration))
        {
            return false;
        }

        FlyObjectCfg config = FlyObjectCfgConfig.Ins.SearchById(flyObjectCfgId);
        if (config == null || string.IsNullOrEmpty(config.prefab)
            || Resources.Load<GameObject>(config.prefab) == null)
        {
            Debug.LogError($"箭雨特效配置或预制体不存在，FlyObjectCfgId={flyObjectCfgId}");
            return false;
        }

        UnitLogicBase enemy = GetRandomEnemy(casterCamp);
        if (enemy == null)
        {
            return false;
        }

        Vector3 center = enemy.CurPos;
        // 预制体原点为伤害中心，本地+Z为本方后场朝向敌方的方向。
        GameObject effect = UnitViewFactory.CreateGob(
            config.prefab, center, bornConfig.GetForward(casterCamp));
        ParticleSystem[] particles = effect.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem.MainModule main = particles[i].main;
            main.stopAction = ParticleSystemStopAction.None;
            particles[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles[i].Play(false);
        }

        arrowRainStateList.Add(new ArrowRainState
        {
            Prefab = config.prefab,
            Effect = effect,
            Particles = particles,
            RemainTime = effectDuration
        });
        ApplyArrowRainDamage(casterCamp, center, damageRadius, damage, dieType);
        return true;
    }

    /// <summary>一次遍历结算圆形范围内所有存活敌兵，每个敌人只受伤一次。</summary>
    private void ApplyArrowRainDamage(
        ECampType casterCamp, Vector3 center, float radius, int damage, string dieType)
    {
        float radiusSquared = radius * radius;
        for (int i = 0; i < UnitManager.Instance.UnitList.Count; i++)
        {
            UnitLogicBase target = UnitManager.Instance.UnitList[i];
            if (target == null || target.IsDead || target.CampType == casterCamp
                || target.UnitType != EUnitType.Solider)
            {
                continue;
            }

            Vector3 offset = target.CurPos - center;
            offset.y = 0f;
            if (offset.sqrMagnitude <= radiusSquared)
            {
                BattleLogicDamageTools.DoDamage(null, target, -damage, target.UId, dieType, center);
            }
        }
    }

    /// <summary>全局技能管理器不需要LateUpdate处理。</summary>
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
        for (int i = 0; i < arrowRainStateList.Count; i++)
        {
            RecycleArrowRainEffect(arrowRainStateList[i]);
        }
        arrowRainStateList.Clear();
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

    /// <summary>只更新整体特效的回收时间，不再生成箭矢或重复结算伤害。</summary>
    private void UpdateArrowRain(float dt)
    {
        for (int i = arrowRainStateList.Count - 1; i >= 0; i--)
        {
            ArrowRainState state = arrowRainStateList[i];
            state.RemainTime -= dt;
            if (state.RemainTime <= 0f)
            {
                RecycleArrowRainEffect(state);
                arrowRainStateList.RemoveAt(i);
            }
        }
    }

    /// <summary>清空粒子并归还整体箭雨特效到现有对象缓存。</summary>
    private void RecycleArrowRainEffect(ArrowRainState state)
    {
        if (state.Effect == null)
        {
            return;
        }
        for (int i = 0; i < state.Particles.Length; i++)
        {
            if (state.Particles[i] != null)
            {
                state.Particles[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
        UnitViewFactory.RemoveGob(state.Prefab, state.Effect);
    }

    /// <summary>随机取得一个存活的敌方士兵作为箭雨中心。</summary>
    private UnitLogicBase GetRandomEnemy(ECampType casterCamp)
    {
        UnitLogicBase result = null;
        int enemyCount = 0;
        for (int i = 0; i < UnitManager.Instance.UnitList.Count; i++)
        {
            UnitLogicBase unitLogic = UnitManager.Instance.UnitList[i];
            if (unitLogic == null || unitLogic.IsDead
                || unitLogic.CampType == casterCamp
                || unitLogic.UnitType != EUnitType.Solider)
            {
                continue;
            }

            enemyCount++;
            if (Random.Range(0, enemyCount) == 0)
            {
                result = unitLogic;
            }
        }
        return result;
    }

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
