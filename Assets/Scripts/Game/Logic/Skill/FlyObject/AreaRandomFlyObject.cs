using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaRandomFlyObject : FlyObjectLogicBase
{
    private float liveTime = 0;
    private float damageClipTime = 0;
    private List<GameObject> flyObjectGobList = new List<GameObject>();

    public override void SetFlyObjectInfo(FlyObjectCfg flyObjectCfg, Vector3 oriPos, Vector3 tarPos, UnitLogicBase atkUnitLogic, List<UnitLogicBase> targetLogicList, UnitLogicBase searchTargetUnit, SkillLogicBase skillLogic, int damageValue, int uIndex)
    {
        base.SetFlyObjectInfo(flyObjectCfg, oriPos, tarPos, atkUnitLogic, targetLogicList, searchTargetUnit, skillLogic, damageValue, uIndex);
        flyObjectGobList.Clear();
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
            setRandomPos();
            damage();
            damageClipTime = mFlyObjectCfg.damClipTime;   
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
                BattleLogicDamageTools.DoDamage(mAtkUnitLogic, tarUnitLogic, mDamage, tarUnitLogic.UId, mSkillLogic.SkillCfg.dieType, mTarPos);
            }
        }
    }

    private void die()
    {
        UnitFactory.RemoveFlyObjectLogic(this);
        for (int i = 0; i < flyObjectGobList.Count; i++)
        {
            UnitViewFactory.RemoveGob(mFlyObjectCfg.prefab, flyObjectGobList[i]);
        }
        flyObjectGobList.Clear();
    }

    private void setRandomPos()
    {
        mTarPos = getRandomPos();
        GameObject flyObject = UnitViewFactory.CreateGob(mFlyObjectCfg.prefab, mTarPos, Vector3.zero);
        flyObjectGobList.Add(flyObject);
    }

    private Vector3 getRandomPos()
    {
        if (randomIndex <= 0)
            return mTarPos;
        UnitLogicBase tarUnitLogic = UnitManager.Instance.UnitList[randomIndex];
        return tarUnitLogic.CurPos;
    }
}