using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>Applies an RVO pull toward the caster to enemies inside the skill area.</summary>
public class BlackHallFlyObject : FlyObjectLogicBase
{
    private float liveTime;
    private float searchCooldown;
    private float pullMultiplier;
    private int searchRequest = -1;
    private bool searchPending;
    private bool destroyed;
    private readonly List<int> resultIndices = new List<int>();
    private readonly List<int> resultUids = new List<int>();
    private int nearestIndex = -1;
    private int randomIndex = -1;

    public override void SetFlyObjectInfo(
        FlyObjectCfg flyObjectCfg, Vector3 oriPos, Vector3 tarPos,
        UnitLogicBase atkUnitLogic, List<UnitLogicBase> targetLogicList,
        UnitLogicBase searchTargetLogic, SkillLogicBase skillLogic,
        int attackDamage, int flyUIndex)
    {
        base.SetFlyObjectInfo(flyObjectCfg, oriPos, tarPos, atkUnitLogic,
            targetLogicList, searchTargetLogic, skillLogic, attackDamage, flyUIndex);
        liveTime = flyObjectCfg.liveTime;
        searchCooldown = 0f;
        searchPending = false;
        destroyed = false;
        resultIndices.Clear();
        resultUids.Clear();

        if (!float.TryParse(flyObjectCfg.flyParam, NumberStyles.Float,
                CultureInfo.InvariantCulture, out pullMultiplier) || pullMultiplier <= 0f)
        {
            Debug.LogError($"BlackHall flyParam is invalid: {flyObjectCfg.flyParam}");
            Die();
            return;
        }

        mFlyObjectGob = UnitViewFactory.CreateGob(flyObjectCfg.prefab, atkUnitLogic.CurPos, Vector3.zero);
    }

    public override void FlyObjectUpdate(float dt)
    {
        if (destroyed) return;
        if (mAtkUnitLogic == null || mAtkUnitLogic.IsDead) { Die(); return; }

        mTarPos = mAtkUnitLogic.CurPos;
        if (mFlyObjectGob != null) mFlyObjectGob.transform.position = mTarPos;
        liveTime -= dt;
        if (liveTime <= 0f) { Die(); return; }

        searchCooldown -= dt;
        if (!searchPending && searchCooldown <= 0f)
        {
            searchRequest = MapCellManager.Instance.RequestSearch(
                mTarPos, mSkillLogic.SkillCfg.skillArea, mAtkUnitLogic.OtherCampTypeInt);
            searchPending = searchRequest >= 0;
            searchCooldown = Mathf.Max(0.02f, mFlyObjectCfg.damClipTime);
        }
    }

    public override void FlyObjectLateUpdate(float dt)
    {
        if (destroyed || !searchPending) return;
        resultIndices.Clear();
        nearestIndex = -1;
        randomIndex = -1;
        MapCellManager.Instance.GetResult(searchRequest, resultIndices, ref nearestIndex, ref randomIndex);
        UnitTargetSnapshot.Capture(resultIndices, resultUids);
        searchPending = false;

        for (int i = 0; i < resultIndices.Count; i++)
        {
            if (!UnitTargetSnapshot.TryGet(resultIndices, resultUids, i, out UnitLogicBase target)
                || target == null || target.IsDead
                || target.CampTypeInt == mAtkUnitLogic.CampTypeInt)
            {
                continue;
            }

            Vector3 pullDirection = mTarPos - target.CurPos;
            pullDirection.y = 0f;
            if (pullDirection.sqrMagnitude > 0.0001f)
            {
                target.AgentMoveForward(pullDirection.normalized, pullMultiplier);
            }
        }
    }

    private void Die()
    {
        if (destroyed) return;
        destroyed = true;
        if (mFlyObjectGob != null)
        {
            UnitViewFactory.RemoveGob(mFlyObjectCfg.prefab, mFlyObjectGob);
            mFlyObjectGob = null;
        }
        UnitFactory.RemoveFlyObjectLogic(this);
    }
}
