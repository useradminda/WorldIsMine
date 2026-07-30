using System;

namespace WorldIsMine.Net.Protocol
{
    // Values are wire contracts copied from server pro_skbz@63c3219.
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
        S2CPlayerEnter = 21000,
        S2CPlayerLeave = 21001,
        S2CPlayerCampSelected = 21002,
        C2SLiveClientTest = 21003,
        S2CLiveClientTest = 21004
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
