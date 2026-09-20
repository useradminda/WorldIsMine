using UnityEngine;

public static class BuffLogicFactory
{
    /// <summary>根据BuffCfg.tyep创建对应的Buff逻辑。</summary>
    public static BuffLogicBase Create(
        int buffCfgId,
        UnitLogicBase unitLogic,
        BuffLogicMachine buffLogicMachine)
    {
        BuffCfg buffCfg = BuffCfgConfig.Ins.SearchById(buffCfgId);
        if (buffCfg == null)
        {
            Debug.LogError($"Buff配置不存在，BuffId={buffCfgId}");
            return null;
        }

        BuffDefine.EBuffType buffType = (BuffDefine.EBuffType)buffCfg.tyep;
        switch (buffType)
        {
            case BuffDefine.EBuffType.Freeze:
                return new IceBuffLogic(unitLogic, buffLogicMachine, buffCfgId);
            case BuffDefine.EBuffType.AddAtk:
                return new AttackBuffLogic(unitLogic, buffLogicMachine, buffCfgId);
            default:
                Debug.LogError($"未实现的Buff类型，BuffId={buffCfgId}，Type={buffCfg.tyep}");
                return null;
        }
    }
}
