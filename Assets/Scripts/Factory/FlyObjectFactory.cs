using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 根据飞行器配置创建并注册运行时飞行器逻辑。
/// </summary>
public static class FlyObjectFactory
{
    public static FlyObjectLogicBase CreateFlyObjectLogic(
        int flyObjectCfgId,
        Vector3 oriPos,
        Vector3 tarPos,
        UnitLogicBase atkUnitLogic,
        List<UnitLogicBase> targetLogicList,
        UnitLogicBase searchTargetLogic,
        SkillLogicBase skillLogic,
        int attackDamage)
    {
        FlyObjectCfg flyObjectCfg = FlyObjectCfgConfig.Ins.SearchById(flyObjectCfgId);
        if (flyObjectCfg == null)
        {
            Debug.LogError($"Fly object config not found. FlyObjectCfgId={flyObjectCfgId}");
            return null;
        }

        FlyObjectLogicBase flyObjectLogic = CreateFlyObjectLogic(flyObjectCfg.flyType);
        int flyUIndex = FlyObjectManager.Instance.AddFlyUnitImmediately(flyObjectLogic);
        flyObjectLogic.SetFlyObjectInfo(
            flyObjectCfg,
            oriPos,
            tarPos,
            atkUnitLogic,
            targetLogicList,
            searchTargetLogic,
            skillLogic,
            attackDamage,
            flyUIndex);
        return flyObjectLogic;
    }

    private static FlyObjectLogicBase CreateFlyObjectLogic(string flyType)
    {
        switch (flyType)
        {
            case "arrow":
                return new ArrowFlyObject();
            case "area":
                return new AreaFlyObject();
            case "stone":
                return new StoneFlyObject();
            case "line":
                return new LineFlyObject();
            case "lineRender":
                return new LineRenderFlyObject();
            case "jumpArrow":
                return new JumpArrowFlyObject();
            case "globalStone":
                return new GlobalStoneFlyObject();
            case "globalArrow":
                return new GlobalArrowFlyObject();
            case "areaRandom":
                return new AreaRandomFlyObject();
            case "aroundArea":
                return new AroundAreaFlyObject();
            case "createWall":
                return new CreateWallFlyObject();
            case "createCircle":
                return new CreateCircleFlyObject();
            case "pointToPoint":
                return new PointToPointFlyObject();
            case "blackHall":
                return new BlackHallFlyObject();
            case "translateArea":
                return new TranslateAreaFlyObject();
            case "swordSpecial":
                return new SwordSpecialFlyObject();
            default:
                Debug.LogWarning($"Unknown fly object type '{flyType}', using base fly object logic.");
                return new FlyObjectLogicBase();
        }
    }
}

