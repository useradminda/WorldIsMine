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
}
