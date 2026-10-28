using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>Instant rectangular hit in the cast direction; MapCell is only a broad-phase query.</summary>
public class FastArrowFlyObject : FlyObjectLogicBase
{
    private readonly List<int> resultIndices = new List<int>();
    private readonly List<int> resultUids = new List<int>();
    private Vector3 forward;
    private Vector3 right;
    private float length;
    private float halfWidth;
    private float visualTime;
    private int searchRequest = -1;
    private int nearestIndex = -1;
    private int randomIndex = -1;
    private bool searchPending;
    private bool hitResolved;
    private bool destroyed;

    public override void SetFlyObjectInfo(
        FlyObjectCfg flyObjectCfg, Vector3 oriPos, Vector3 tarPos,
        UnitLogicBase atkUnitLogic, List<UnitLogicBase> targetLogicList,
        UnitLogicBase searchTargetLogic, SkillLogicBase skillLogic,
        int attackDamage, int flyUIndex)
    {
        base.SetFlyObjectInfo(flyObjectCfg, oriPos, tarPos, atkUnitLogic,
            targetLogicList, searchTargetLogic, skillLogic, attackDamage, flyUIndex);

        if (skillLogic == null || skillLogic.SkillCfg == null ||
            !TryParseSize(skillLogic.SkillCfg.skillParam, out length, out float width))
        {
            Debug.LogError($"FastArrow skillParam must be length,width. FlyObjectId={flyObjectCfg.id}");
            Die();
            return;
        }

        halfWidth = width * 0.5f;
        forward = tarPos - oriPos;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.0001f) forward = atkUnitLogic.TargetForward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.0001f) forward = Vector3.forward;
        forward.Normalize();
        right = Vector3.Cross(Vector3.up, forward);

        // The broad-phase circle exactly encloses the forward rectangle.
        Vector3 center = oriPos + forward * (length * 0.5f);
        float radius = Mathf.Sqrt(length * length + width * width) * 0.5f;
        searchRequest = MapCellManager.Instance.RequestSearch(
            center, radius, atkUnitLogic.OtherCampTypeInt);
        searchPending = searchRequest >= 0;
        visualTime = Mathf.Max(0.02f, flyObjectCfg.liveTime);
        if (!string.IsNullOrEmpty(flyObjectCfg.prefab))
            mFlyObjectGob = UnitViewFactory.CreateGob(flyObjectCfg.prefab, oriPos, forward);
    }

    public override void FlyObjectUpdate(float dt)
    {
        if (destroyed) return;
        if (!searchPending && !hitResolved) { Die(); return; }
        visualTime -= dt;
        if (hitResolved && visualTime <= 0f) Die();
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
        hitResolved = true;

        for (int i = 0; i < resultIndices.Count; i++)
        {
            if (!UnitTargetSnapshot.TryGet(resultIndices, resultUids, i, out UnitLogicBase enemy)
                || enemy.CampTypeInt == mAtkUnitLogic.CampTypeInt)
                continue;

            Vector3 offset = enemy.CurPos - mOriPos;
            offset.y = 0f;
            float distance = Vector3.Dot(offset, forward);
            float sideways = Vector3.Dot(offset, right);
            if (distance < 0f || distance > length || Mathf.Abs(sideways) > halfWidth)
                continue;

            BattleLogicDamageTools.DoDamage(mAtkUnitLogic, enemy, GetFinalDamage(enemy),
                resultUids[i], mSkillLogic.SkillCfg.dieType, enemy.CurPos);
            mSkillLogic.ApplyBuffs(enemy);
        }

        if (visualTime <= 0f) Die();
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

    private static bool TryParseSize(string value, out float parsedLength, out float parsedWidth)
    {
        parsedLength = parsedWidth = 0f;
        string[] parts = (value ?? string.Empty).Split(',');
        return parts.Length == 2
            && float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out parsedLength)
            && float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out parsedWidth)
            && parsedLength > 0f && parsedWidth > 0f
            && !float.IsInfinity(parsedLength) && !float.IsNaN(parsedLength)
            && !float.IsInfinity(parsedWidth) && !float.IsNaN(parsedWidth);
    }
}
