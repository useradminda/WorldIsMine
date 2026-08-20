
using System.Collections.Generic;
using UnityEngine;
// Area Object
public class AreaFlyObject : FlyObjectLogicBase
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
        }

        liveTime -= dt;
        if (liveTime < 0)
            die();
    }

    private int searchReqIndex = -1;
    List<int> resultUnitIndexList = new List<int>();
    private int neastIndex = -1;
    private void reqSearchTar()
    {
        searchReqIndex = MapCellManager.Instance.RequestSearch(mTarPos, mSkillLogic.SkillCfg.skillArea, mAtkUnitLogic.OtherCampTypeInt);
    }

    private void setSearchTar()
    {
        MapCellManager.Instance.GetResult(searchReqIndex, resultUnitIndexList, ref neastIndex);
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
    //private void initFlyObjectInfo()
    //{
    //if (flyState == false)
    //{
    //    curFlyTime = 0;
    //    dis = Vector3.Distance(new Vector3(mOriPos.x, 0, mOriPos.z), new Vector3(mTarPos.x, 0, mTarPos.z));
    //    dis = Mathf.Max(dis, 0.05f);
    //    flyTime = dis / mFlyObjectCfg.speed;
    //    // 防止距离太近
    //    height = Mathf.Sqrt(dis) * 1.5f;

    //    lastPos = mOriPos;
    //    flyState = true;
    //}
    //}

    //private void updateFlyTime(float dt)
    //{
    //if (!flyState)
    //    return;
    //curFlyTime += dt;
    //float t = Mathf.Clamp01(curFlyTime / flyTime);

    //// 水平移动（包含Y插值）
    //curPos = Vector3.Lerp(new Vector3(mOriPos.x, 0, mOriPos.z), new Vector3(mTarPos.x, 0, mTarPos.z), t);
    //// 抛物线
    //curPos.y += Mathf.Sin(t * Mathf.PI) * height;
    //curDir = curPos - lastPos;
    //lastPos = curPos;

    //if (curFlyTime > flyTime)
    //{
    //    flyState = false;
    //    TouchTarUnit();
    //}
    //}

    //public override void TouchTarUnit()
    //{
    //    BattleLogicDamageTools.DoDamage(mAtkUnitLogic, mTargetLogicList, mSkillLogic);
    //}
}
