/// <summary>
/// 对局玩家数据对象，保存玩家身份和胜点信息。
/// 该对象独立于 UnitLogicBase、UnitView 和预制体，适合单位循环复用以及英雄重生时继续保留归属。
/// </summary>
public sealed class BattlePlayer
{
    /// <summary>玩家头像地址，可以是资源路径或外部头像地址。</summary>
    public string AvatarAddress { get; private set; }

    /// <summary>玩家唯一标识。</summary>
    public int UId { get; private set; }

    /// <summary>玩家名称。</summary>
    public string Name { get; private set; }

    /// <summary>玩家当前胜点。</summary>
    public int VictoryPoint { get; private set; }

    /// <summary>本场战斗的充值金额统计。</summary>
    public int RechargeCount { get; private set; }

    /// <summary>本场战斗分配给该玩家的积分。</summary>
    public int SharedScore { get; private set; }

    /// <summary>本场战斗的击杀数量。</summary>
    public int KillCount { get; private set; }

    /// <summary>玩家当前队形信息，红方默认左侧，蓝方默认右侧。</summary>
    public BattleFormationInfo FormationInfo { get; private set; }

    /// <summary>创建一个玩家数据对象。</summary>
    public BattlePlayer(int uid, string name, string avatarAddress, int victoryPoint = 0, BattleFormationInfo formationInfo = null)
    {
        UId = uid;
        Name = name ?? string.Empty;
        AvatarAddress = avatarAddress ?? string.Empty;
        VictoryPoint = victoryPoint < 0 ? 0 : victoryPoint;
        FormationInfo = formationInfo;
    }

    /// <summary>修改玩家胜点，胜点不会低于零。</summary>
    public void AddVictoryPoint(int value)
    {
        VictoryPoint += value;
        if (VictoryPoint < 0)
        {
            VictoryPoint = 0;
        }
    }

    /// <summary>直接设置玩家胜点。</summary>
    public void SetVictoryPoint(int value)
    {
        VictoryPoint = value < 0 ? 0 : value;
    }

    /// <summary>增加本场战斗充值统计。</summary>
    public void AddRechargeCount(int value)
    {
        RechargeCount = System.Math.Max(0, RechargeCount + value);
    }

    /// <summary>增加本场战斗分配积分。</summary>
    public void AddSharedScore(int value)
    {
        SharedScore = System.Math.Max(0, SharedScore + value);
    }

    /// <summary>增加本场战斗击杀数量。</summary>
    public void AddKillCount(int value = 1)
    {
        KillCount = System.Math.Max(0, KillCount + value);
    }

    /// <summary>清零本场战斗统计数据。</summary>
    public void ResetBattleStats()
    {
        RechargeCount = 0;
        SharedScore = 0;
        VictoryPoint = 0;
        KillCount = 0;
    }

    /// <summary>更新玩家基础资料。</summary>
    public void SetProfile(string name, string avatarAddress)
    {
        Name = name ?? string.Empty;
        AvatarAddress = avatarAddress ?? string.Empty;
    }

    /// <summary>设置玩家当前队形信息。</summary>
    public void SetFormationInfo(BattleFormationInfo formationInfo)
    {
        FormationInfo = formationInfo;
    }
}
