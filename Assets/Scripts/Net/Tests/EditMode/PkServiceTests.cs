using ClientProtocol;
using Google.Protobuf;
using NUnit.Framework;
using WorldIsMine.Net.Protocol;
using WorldIsMine.Net.Runtime;
using WorldIsMine.Net.Services;
using WorldIsMine.Net.Transport;

namespace WorldIsMine.Net.Tests
{
    public sealed class PkServiceTests
    {
        [Test]
        public void Router_DispatchesPkStartAndRaisesBattleStartedOnMainThread()
        {
            using var transport = new TcpTransport();
            var router = new MessageRouter();
            var mainThread = new MainThreadDispatcher();
            var service = new PkService(transport, router, mainThread);
            SessionSnapshot started = null;
            service.BattleStarted += snapshot => started = snapshot;

            var response = new PKStartClientResponse
            {
                Accepted = true,
                Reason = "match_success",
                Snapshot = new SessionSnapshot
                {
                    SessionId = "pk-session-1",
                    AnchorA = new PKAnchorInfo { AnchorId = "anchor-a", RoomId = "room-a" },
                    AnchorB = new PKAnchorInfo { AnchorId = "anchor-b", RoomId = "room-b" }
                }
            };

            bool handled = router.Dispatch(new NetPacket(
                RequestCode.S2CPkStart,
                ActionCode.None,
                1,
                response.ToByteArray()));

            Assert.IsTrue(handled);
            Assert.IsNull(started);
            Assert.AreEqual(1, mainThread.Drain());
            Assert.AreEqual("pk-session-1", started.SessionId);
            Assert.AreEqual("pk-session-1", service.CurrentSession.SessionId);
        }

        [Test]
        public void Router_DispatchesEveryPkServerRoute()
        {
            using var transport = new TcpTransport();
            var router = new MessageRouter();
            var mainThread = new MainThreadDispatcher();
            var service = new PkService(transport, router, mainThread);
            int startCount = 0;
            int endCount = 0;
            int syncCount = 0;
            service.StartResponseReceived += _ => startCount++;
            service.BattleEnded += _ => endCount++;
            service.SyncCommandReceived += _ => syncCount++;

            Assert.IsTrue(router.Dispatch(new NetPacket(
                RequestCode.S2CPkStart,
                ActionCode.None,
                1,
                new PKStartClientResponse { Accepted = true, Reason = "match_queued" }.ToByteArray())));
            Assert.IsTrue(router.Dispatch(new NetPacket(
                RequestCode.S2CPkEnd,
                ActionCode.None,
                2,
                new SubmitGiftResponse { Accepted = true, SessionId = "pk-session-1" }.ToByteArray())));
            Assert.IsTrue(router.Dispatch(new NetPacket(
                RequestCode.S2CPkSync,
                ActionCode.None,
                3,
                new SyncCommand { SessionId = "pk-session-1", Sequence = 1 }.ToByteArray())));

            Assert.AreEqual(3, mainThread.Drain());
            Assert.AreEqual(1, startCount);
            Assert.AreEqual(1, endCount);
            Assert.AreEqual(1, syncCount);
        }
    }

    public sealed class PlayerServiceTests
    {
        [Test]
        public void Router_DispatchesPlayerEnterAndLeaveOnMainThread()
        {
            var router = new MessageRouter();
            var mainThread = new MainThreadDispatcher();
            var service = new PlayerService(router, mainThread);
            LivePlayerEnterNotify entered = null;
            LivePlayerLeaveNotify left = null;
            LivePlayerCampSelectedNotify selected = null;
            service.PlayerEntered += value => entered = value;
            service.PlayerLeft += value => left = value;
            service.PlayerCampSelected += value => selected = value;

            Assert.IsTrue(router.Dispatch(new NetPacket(
                RequestCode.S2CPlayerEnter,
                ActionCode.None,
                10,
                new LivePlayerEnterNotify
                {
                    RoomId = "room-1",
                    Player = new PlayerSnapshot
                    {
                        PlayerId = 42,
                        Platform = "dy",
                        OpenId = "open-1",
                        Nickname = "Alice"
                    },
                    FirstEnter = true
                }.ToByteArray())));
            Assert.IsTrue(router.Dispatch(new NetPacket(
                RequestCode.S2CPlayerCampSelected,
                ActionCode.None,
                12,
                new LivePlayerCampSelectedNotify
                {
                    RoomId = "room-1",
                    PlayerId = 42,
                    Camp = LivePlayerCamp.Red
                }.ToByteArray())));
            Assert.IsTrue(router.Dispatch(new NetPacket(
                RequestCode.S2CPlayerLeave,
                ActionCode.None,
                11,
                new LivePlayerLeaveNotify
                {
                    RoomId = "room-1",
                    PlayerId = 42,
                    Reason = "viewer_leave"
                }.ToByteArray())));

            Assert.IsNull(entered);
            Assert.IsNull(left);
            Assert.IsNull(selected);
            Assert.AreEqual(3, mainThread.Drain());
            Assert.AreEqual((ulong)42, entered.Player.PlayerId);
            Assert.AreEqual("Alice", entered.Player.Nickname);
            Assert.AreEqual((ulong)42, left.PlayerId);
            Assert.AreEqual("viewer_leave", left.Reason);
            Assert.AreEqual(LivePlayerCamp.Red, selected.Camp);
        }

        [Test]
        public void Router_DispatchesLiveTestResponseOnMainThread()
        {
            using var transport = new TcpTransport();
            var router = new MessageRouter();
            var mainThread = new MainThreadDispatcher();
            var service = new LiveTestService(transport, router, mainThread);
            LiveClientTestResponse received = null;
            service.ResponseReceived += value => received = value;

            Assert.IsTrue(router.Dispatch(new NetPacket(
                RequestCode.S2CLiveClientTest,
                ActionCode.None,
                20,
                new LiveClientTestResponse
                {
                    Accepted = true,
                    Action = LiveClientTestAction.Gift,
                    EventId = "event-1"
                }.ToByteArray())));

            Assert.IsNull(received);
            Assert.AreEqual(1, mainThread.Drain());
            Assert.AreEqual(LiveClientTestAction.Gift, received.Action);
            Assert.AreEqual("event-1", received.EventId);
        }
    }
}
