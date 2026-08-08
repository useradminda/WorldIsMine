using System;

namespace WorldIsMine.Net.Protocol
{
    // Values are wire contracts synchronized from the server working tree based on pro_skbz@c069542.
    public enum RequestCode
    {
        None = 0,
        Beat = 1,
        User = 2,
        Bind = 6,
        [Obsolete("Use Bind. Blind is a legacy server spelling.")]
        Blind = Bind,
        QueryReward = 16,
        DyEnterRoomMsg = 10000,
        C2SRoleFightMsg = 10001,
        S2CPkSync = 20000,
        C2SPkSession = 20001,
        S2CPkStart = 20002,
        S2CPkEnd = 20003,
        C2SClientDebug = 20004,
        S2CClientDebug = 20005,
        S2CPkCommandAck = 20006,
        S2CPlayerEnter = 21000,
        S2CPlayerLeave = 21001,
        S2CPlayerCampSelected = 21002,
        C2SLiveClientTest = 21003,
        S2CLiveClientTest = 21004,
        S2CPlayerGift = 21005,
        S2CGameConfig = 21100,
        C2SEquipmentQuery = 21200,
        S2CEquipmentQuery = 21201,
        C2SEquipmentCreate = 21202,
        S2CEquipmentCreate = 21203,
        C2SEquipmentUpgrade = 21204,
        S2CEquipmentUpgrade = 21205,
        C2SEquipmentEquip = 21206,
        S2CEquipmentEquip = 21207,
        C2SEquipmentUnequip = 21208,
        S2CEquipmentUnequip = 21209,
        S2CEquipmentChanged = 21210,
        C2SScoreRankQuery = 21300,
        S2CScoreRankQuery = 21301,
        C2STroopQuery = 21400,
        S2CTroopQuery = 21401,
        C2STroopUpgrade = 21402,
        S2CTroopUpgrade = 21403
    }

    public enum ActionCode
    {
        None = 0,
        Beat = 1,
        Login = 2,
        Match = 3,
        MatchBoss = 4,
        MatchSuccess = 5,
        Bind = 6,
        [Obsolete("Use Bind. Blind is a legacy server spelling.")]
        Blind = Bind,
        GetUserInfo = 7,
        ChangeUser = 8,
        One = 9,
        Two = 10,
        Three = 11,
        Four = 12,
        UserGift = 13,
        UserBarrage = 14
    }
}
