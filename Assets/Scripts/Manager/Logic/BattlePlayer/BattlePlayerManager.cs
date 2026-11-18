using System.Collections.Generic;
using ZTools;

/// <summary>
/// 对局玩家全局管理器，负责保存玩家对象并提供阵营默认玩家。
/// 单位只保存 BattlePlayer 引用，玩家对象本身不会随单位回收或英雄重生而销毁。
/// </summary>
public sealed class BattlePlayerManager : Singleton<BattlePlayerManager>
{
    private readonly Dictionary<int, BattlePlayer> playerMap = new Dictionary<int, BattlePlayer>();
    private BattlePlayer redPlayer;
    private BattlePlayer bluePlayer;

    /// <summary>获取红方默认玩家。</summary>
    public BattlePlayer RedPlayer => redPlayer ?? (redPlayer = CreateDefaultPlayer(ECampType.Red));

    /// <summary>获取蓝方默认玩家。</summary>
    public BattlePlayer BluePlayer => bluePlayer ?? (bluePlayer = CreateDefaultPlayer(ECampType.Blue));

    /// <summary>根据阵营获取默认玩家。</summary>
    public BattlePlayer GetCampPlayer(ECampType campType)
    {
        return campType == ECampType.Red ? RedPlayer : BluePlayer;
    }

    /// <summary>根据玩家 UID 获取玩家对象。</summary>
    public BattlePlayer GetPlayer(int uid)
    {
        playerMap.TryGetValue(uid, out BattlePlayer player);
        return player;
    }

    /// <summary>注册或替换一个玩家对象。</summary>
    public void RegisterPlayer(BattlePlayer player)
    {
        playerMap[player.UId] = player;
    }

    /// <summary>设置阵营默认玩家，后续该阵营召唤的单位默认绑定此玩家。</summary>
    public void SetCampPlayer(ECampType campType, BattlePlayer player)
    {
        RegisterPlayer(player);
        if (campType == ECampType.Red)
        {
            redPlayer = player;
        }
        else
        {
            bluePlayer = player;
        }
    }

    /// <summary>重置本场对局的玩家数据。</summary>
    public void Reset()
    {
        playerMap.Clear();
        redPlayer = null;
        bluePlayer = null;
    }

    /// <summary>创建阵营默认玩家。</summary>
    private BattlePlayer CreateDefaultPlayer(ECampType campType)
    {
        int uid = campType == ECampType.Red ? 30001 : 30002;
        string name = campType == ECampType.Red ? "红方玩家" : "蓝方玩家";
        BattlePlayer player = new BattlePlayer(uid, name, string.Empty, 0, new BattleFormationInfo(campType));
        RegisterPlayer(player);
        return player;
    }
}
