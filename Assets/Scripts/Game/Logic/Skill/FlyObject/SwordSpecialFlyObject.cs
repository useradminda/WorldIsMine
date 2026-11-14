using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// 剑魂觉醒式多剑飞行器。
/// 适用于从释放点生成多把剑，先围绕释放点旋转扩散并上升到固定高度，
/// 悬停后再按间隔逐把向各自的地面落点俯冲，并对目标逐把结算伤害。
/// flyParam 只配置剑的数量，其余表现参数在本类顶部统一配置。
/// </summary>
public class SwordSpecialFlyObject : FlyObjectLogicBase
{
    // 剑的圆周扩散半径，单位为世界坐标。
    private const float ConfigRingRadius = 3f;
    // 剑围绕释放点的旋转速度，单位为弧度每秒。
    private const float ConfigRotationSpeed = 2f;
    // 剑从释放点上升到固定高度所需的时间，单位为秒。
    private const float ConfigRiseDuration = 1.2f;
    // 剑悬停的固定世界高度 Y。
    private const float ConfigApexY = 6f;
    // 剑到达固定高度后的悬停时间，单位为秒。
    private const float ConfigHoldDuration = 1f;
    // 第一把剑开始俯冲前的等待间隔，单位为秒。
    private const float ConfigLaunchStartInterval = 1f;
    // 后续每把剑的发射间隔加速系数，数值越大，间隔缩短越快。
    private const float ConfigLaunchAcceleration = 0.25f;
    // 发射间隔允许达到的最小值，单位为秒。
    private const float ConfigLaunchMinInterval = 0.15f;

    /// <summary>单把剑的运行阶段。</summary>
    private enum SwordPhase
    {
        扩散上升,
        高度悬停,
        俯冲攻击,
        已完成
    }

    /// <summary>保存单把剑的对象、圆周初始点和当前飞行状态。</summary>
    private sealed class SwordState
    {
        public GameObject Gob;
        public Vector3 InitialRingPosition;
        public float RingAngle;
        public SwordPhase Phase;
        public bool DamageTriggered;
    }

    private readonly List<SwordState> swords = new List<SwordState>();
    private int swordCount;
    private float ringRadius;
    private float rotationSpeed;
    private float riseDuration;
    private float apexY;
    private float holdDuration;
    private float launchStartInterval;
    private float launchAcceleration;
    private float launchMinInterval;
    private float elapsed;
    private float holdElapsed;
    private float launchElapsed;
    private int launchedSwordCount;
    private bool destroyed;

    /// <summary>初始化多剑参数、创建剑对象并进入扩散上升阶段。</summary>
    public override void SetFlyObjectInfo(
        FlyObjectCfg flyObjectCfg,
        Vector3 oriPos,
        Vector3 tarPos,
        UnitLogicBase atkUnitLogic,
        List<UnitLogicBase> targetLogicList,
        UnitLogicBase searchTargetUnit,
        SkillLogicBase skillLogic,
        int attackDamage,
        int flyUIndex)
    {
        base.SetFlyObjectInfo(
            flyObjectCfg,
            oriPos,
            tarPos,
            atkUnitLogic,
            targetLogicList,
            searchTargetUnit,
            skillLogic,
            attackDamage,
            flyUIndex);

        swords.Clear();
        elapsed = 0f;
        holdElapsed = 0f;
        launchElapsed = 0f;
        launchedSwordCount = 0;
        destroyed = false;

        ReadFlyParameters(flyObjectCfg.flyParam);
        CreateSwords(oriPos);
    }

    /// <summary>按阶段推进所有剑的扩散、悬停、逐把发射和伤害结算。</summary>
    public override void FlyObjectUpdate(float dt)
    {
        if (destroyed)
        {
            return;
        }

        if (swords.Count == 0)
        {
            Die();
            return;
        }

        elapsed += dt;
        UpdateRiseAndSpread(dt);
        UpdateHold(dt);
        UpdateLaunch(dt);

        if (AllSwordsCompleted())
        {
            Die();
        }
    }

    /// <summary>只从 flyParam 读取剑的数量，其余参数使用脚本配置。</summary>
    private void ReadFlyParameters(string flyParam)
    {
        swordCount = 8;
        ringRadius = ConfigRingRadius;
        rotationSpeed = ConfigRotationSpeed;
        riseDuration = ConfigRiseDuration;
        apexY = ConfigApexY;
        holdDuration = ConfigHoldDuration;
        launchStartInterval = ConfigLaunchStartInterval;
        launchAcceleration = ConfigLaunchAcceleration;
        launchMinInterval = ConfigLaunchMinInterval;

        string[] values = (flyParam ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (values.Length > 0
            && int.TryParse(values[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedSwordCount))
        {
            swordCount = Mathf.Max(1, parsedSwordCount);
        }
    }

    /// <summary>在释放点创建全部剑，并为每把剑记录圆周初始角度。</summary>
    private void CreateSwords(Vector3 origin)
    {
        for (int i = 0; i < swordCount; i++)
        {
            float angle = Mathf.PI * 2f * i / swordCount;
            Vector3 ringOffset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ringRadius;
            Vector3 startPosition = origin;
            GameObject swordGob = UnitViewFactory.CreateGob(mFlyObjectCfg.prefab, startPosition, Vector3.up);
            if (swordGob == null)
            {
                continue;
            }

            swordGob.transform.rotation = Quaternion.identity;
            swords.Add(new SwordState
            {
                Gob = swordGob,
                InitialRingPosition = origin + ringOffset,
                RingAngle = angle,
                Phase = SwordPhase.扩散上升,
                DamageTriggered = false
            });
        }
    }

    /// <summary>让所有剑沿圆周扩散、旋转并上升到固定世界高度。</summary>
    private void UpdateRiseAndSpread(float dt)
    {
        float riseProgress = Mathf.Clamp01(elapsed / riseDuration);
        float currentAngleOffset = rotationSpeed * elapsed;
        for (int i = 0; i < swords.Count; i++)
        {
            SwordState sword = swords[i];
            if (sword.Phase != SwordPhase.扩散上升 || sword.Gob == null)
            {
                continue;
            }

            Vector3 origin = mOriPos;
            float angle = sword.RingAngle + currentAngleOffset;
            Vector3 ringOffset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ringRadius * riseProgress;
            Vector3 position = Vector3.Lerp(origin, new Vector3(origin.x, apexY, origin.z), riseProgress);
            position.x += ringOffset.x;
            position.z += ringOffset.z;
            sword.Gob.transform.position = position;
            sword.Gob.transform.rotation = Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f);

            if (riseProgress >= 1f)
            {
                sword.Phase = SwordPhase.高度悬停;
            }
        }
    }

    /// <summary>让达到固定高度的剑保持垂直并等待逐把发射阶段开始。</summary>
    private void UpdateHold(float dt)
    {
        if (elapsed < riseDuration)
        {
            return;
        }

        holdElapsed += dt;
        if (holdElapsed < holdDuration)
        {
            for (int i = 0; i < swords.Count; i++)
            {
                if (swords[i].Phase == SwordPhase.高度悬停 && swords[i].Gob != null)
                {
                    swords[i].Gob.transform.rotation = Quaternion.identity;
                }
            }
        }
    }

    /// <summary>按逐渐缩短的时间间隔发射剑，并推进已经发射的剑。</summary>
    private void UpdateLaunch(float dt)
    {
        if (elapsed < riseDuration + holdDuration)
        {
            return;
        }

        launchElapsed += dt;
        if (launchedSwordCount < swords.Count)
        {
            float currentInterval = GetLaunchInterval(launchedSwordCount);
            if (launchElapsed >= currentInterval)
            {
                launchElapsed = 0f;
                swords[launchedSwordCount].Phase = SwordPhase.俯冲攻击;
                launchedSwordCount++;
            }
        }

        for (int i = 0; i < swords.Count; i++)
        {
            if (swords[i].Phase == SwordPhase.俯冲攻击)
            {
                UpdateSwordDive(swords[i], dt);
            }
        }
    }

    /// <summary>计算第几把剑的发射间隔，让后续剑逐渐加快出发。</summary>
    private float GetLaunchInterval(int swordIndex)
    {
        float interval = launchStartInterval / (1f + launchAcceleration * swordIndex);
        return Mathf.Max(launchMinInterval, interval);
    }

    /// <summary>让单把剑沿固定圆周落点俯冲，并在到达地面位置时结算一次伤害。</summary>
    private void UpdateSwordDive(SwordState sword, float dt)
    {
        if (sword.Gob == null)
        {
            sword.Phase = SwordPhase.已完成;
            return;
        }

        Vector3 targetPosition = new Vector3(
            sword.InitialRingPosition.x,
            0f,
            sword.InitialRingPosition.z);
        Vector3 currentPosition = sword.Gob.transform.position;
        float moveSpeed = Mathf.Max(0.01f, mFlyObjectCfg.speed > 0f ? mFlyObjectCfg.speed : 20f);
        Vector3 nextPosition = Vector3.MoveTowards(currentPosition, targetPosition, moveSpeed * dt);
        Vector3 direction = nextPosition - currentPosition;
        sword.Gob.transform.position = nextPosition;
        if (direction.sqrMagnitude > 0.0001f)
        {
            sword.Gob.transform.rotation = Quaternion.LookRotation(direction.normalized);
        }

        if ((nextPosition - targetPosition).sqrMagnitude <= 0.01f * 0.01f)
        {
            TriggerSwordDamage(sword, targetPosition);
            sword.Phase = SwordPhase.已完成;
        }
    }

    /// <summary>在单把剑到达自己的落点时结算一次伤害和技能 Buff。</summary>
    private void TriggerSwordDamage(SwordState sword, Vector3 hitPosition)
    {
        if (sword.DamageTriggered)
        {
            return;
        }

        sword.DamageTriggered = true;
        CreateArriveEffect(hitPosition);
        if (mSearchTargetUnitLogic != null && !mSearchTargetUnitLogic.IsDead)
        {
            BattleLogicDamageTools.DoDamage(
                mAtkUnitLogic,
                mSearchTargetUnitLogic,
                GetFinalDamage(mSearchTargetUnitLogic),
                mSearchTargetUnitLogic.UId,
                mSkillLogic.SkillCfg.dieType,
                hitPosition);
            mSkillLogic.ApplyBuffs(mSearchTargetUnitLogic);
        }
    }

    /// <summary>判断所有剑是否已经完成俯冲和伤害结算。</summary>
    private bool AllSwordsCompleted()
    {
        for (int i = 0; i < swords.Count; i++)
        {
            if (swords[i].Phase != SwordPhase.已完成)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>回收全部剑对象和飞行器逻辑，结束本次技能表现。</summary>
    private void Die()
    {
        if (destroyed)
        {
            return;
        }

        destroyed = true;
        for (int i = 0; i < swords.Count; i++)
        {
            if (swords[i].Gob != null)
            {
                UnitViewFactory.RemoveGob(mFlyObjectCfg.prefab, swords[i].Gob);
                swords[i].Gob = null;
            }
        }

        swords.Clear();
        UnitFactory.RemoveFlyObjectLogic(this);
    }
}

