using System.Collections.Generic;
using UnityEngine;

/// <summary>Teleports enemies found around the caster to their own camp door.</summary>
public class TranslateAreaFlyObject : FlyObjectLogicBase
{
    private int searchRequest = -1;
    private bool searchPending;
    private bool destroyed;
    private float liveTime;
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
        destroyed = false;
        searchPending = false;
        liveTime = flyObjectCfg.liveTime;
        resultIndices.Clear();
        resultUids.Clear();
        mTarPos = oriPos;
        mFlyObjectGob = UnitViewFactory.CreateGob(flyObjectCfg.prefab, mTarPos, Vector3.zero);
    }

    public override void FlyObjectUpdate(float dt)
    {
        if (destroyed) return;
        liveTime -= dt;
        if (liveTime <= 0f) { Die(); return; }
        if (!searchPending)
        {
            searchRequest = MapCellManager.Instance.RequestSearch(
                mTarPos, mSkillLogic.SkillCfg.skillArea, mAtkUnitLogic.OtherCampTypeInt);
            searchPending = searchRequest >= 0;
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
            if (!UnitTargetSnapshot.TryGet(resultIndices, resultUids, i, out UnitLogicBase enemy)
                || enemy == null || enemy.IsDead || enemy.CampTypeInt == mAtkUnitLogic.CampTypeInt
                || BattleEngine.Instance == null || enemy.Agenter == null)
            {
                continue;
            }

            Vector3 doorPoint = BattleEngine.Instance.GetCityDoorPoint(enemy.CampType);
            Vector3 doorForward = BattleEngine.Instance.GetCityDoorForward(enemy.CampType);
            enemy.TeleportTo(doorPoint, doorForward);
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
