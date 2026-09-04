using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaRandomFlyObject : FlyObjectLogicBase
{
    private float liveTime = 0;
    private float damageClipTime = 0;

    public override void SetFlyObjectInfo(FlyObjectCfg flyObjectCfg, Vector3 oriPos, Vector3 tarPos, UnitLogicBase atkUnitLogic, List<UnitLogicBase> targetLogicList, UnitLogicBase searchTargetUnit, SkillLogicBase skillLogic, int damage, int uIndex)
    {
        base.SetFlyObjectInfo(flyObjectCfg, oriPos, tarPos, atkUnitLogic, targetLogicList, searchTargetUnit, skillLogic, damage, uIndex);
        mFlyObjectGob = UnitViewFactory.CreateGob(flyObjectCfg.prefab, tarPos, Vector3.zero);
        liveTime = flyObjectCfg.liveTime;
        damageClipTime = flyObjectCfg.damClipTime;
    }

    public override void FlyObjectUpdate(float dt)
    {
        reqSearchTar();
        setSearchTar();

        damageClipTime -= dt;
        if (damageClipTime < 0)
        {
            damage();
            damageClipTime = mFlyObjectCfg.damClipTime;
            setRandomPos();
        }

        liveTime -= dt;
        if (liveTime < 0)
            die();
    }

    private int searchReqIndex = -1;
    List<int> resultUnitIndexList = new List<int>();
    private int neastIndex = -1;
    private int randomIndex = -1;
    private void reqSearchTar()
    {
        searchReqIndex = MapCellManager.Instance.RequestSearch(mTarPos, mSkillLogic.SkillCfg.skillArea, mAtkUnitLogic.OtherCampTypeInt);
    }

    private void setSearchTar()
    {
        resultUnitIndexList.Clear();
        neastIndex = -1;
        randomIndex = -1;
        MapCellManager.Instance.GetResult(searchReqIndex, resultUnitIndexList, ref neastIndex, ref randomIndex);
    }

    private void damage()
    {
        if (resultUnitIndexList.Count > 0)
        {
            for (int i = 0; i < resultUnitIndexList.Count; i++)
            {
                int unitIndex = resultUnitIndexList[i];
                UnitLogicBase tarUnitLogic = UnitManager.Instance.UnitList[unitIndex];
                BattleLogicDamageTools.DoDamage(mAtkUnitLogic, tarUnitLogic, mDamage, tarUnitLogic.UId);
            }
        }
    }

    private void die()
    {
        UnitFactory.RemoveFlyObjectLogic(this);
        UnitViewFactory.RemoveGob(mFlyObjectCfg.prefab, mFlyObjectGob);
    }

    private void setRandomPos()
    {
        mTarPos = getRandomPos();
        mFlyObjectGob.transform.position = mTarPos;
    }

    private Vector3 getRandomPos()
    {
        if (randomIndex <= 0)
            return mTarPos;
        UnitLogicBase tarUnitLogic = UnitManager.Instance.UnitList[randomIndex];
        return tarUnitLogic.CurPos;
    }
}