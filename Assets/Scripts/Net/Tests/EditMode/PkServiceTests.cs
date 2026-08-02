using ClientProtocol;
using Google.Protobuf;
using NUnit.Framework;
using PlayerProtocol;
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
                    AnchorA = new PKAnchorInfo
                    {
                        AnchorId = "anchor-a",
                        AnchorName = "主播甲",
                        RoomId = "room-a"
                    },
                    AnchorB = new PKAnchorInfo
                    {
                        AnchorId = "anchor-b",
                        AnchorName = "主播乙",
                        RoomId = "room-b"
                    }
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
            Assert.AreEqual(
                "主播甲  VS  主播乙\n房间A：room-a    房间B：room-b    PK：pk-session-1",
                PkMatchBanner.Format(started));
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
            SyncCommand sync = null;
            service.StartResponseReceived += _ => startCount++;
            service.BattleEnded += _ => endCount++;
            service.SyncCommandReceived += value =>
            {
                syncCount++;
                sync = value;
            };

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
            var giftSync = new GiftSyncPayload
            {
                PlayerId = 42,
                GiftId = "13585"
            };
            giftSync.TroopSpawns.Add(new GiftTroopSpawnData
            {
                TroopId = 10001,
                TroopLevel = 3,
                TroopCount = 2
            });
            giftSync.TroopSpawns.Add(new GiftTroopSpawnData
            {
                TroopId = 20001,
                TroopLevel = 2,
                TroopCount = 1
            });
            giftSync.BuffChanges.Add(new CampBuffChange
            {
                ChangeType = PKCampBuffChangeType.Applied,
                Reason = "gift_buff_applied",
                Buff = new CampBuffState
                {
                    InstanceId = "buff:pk-session-1:1:1001",
                    BuffId = 1001,
                    BuffLevel = 1,
                    BuffName = "Battle Will",
                    TargetSide = PKSide.A,
                    StackCount = 1,
                    ExpireTimeMs = 30_000
                }
            });
            Assert.IsTrue(router.Dispatch(new NetPacket(
                RequestCode.S2CPkSync,
                ActionCode.None,
                3,
                new SyncCommand
                {
                    SessionId = "pk-session-1",
                    Sequence = 1,
                    Gift = giftSync
                }.ToByteArray())));

            Assert.AreEqual(3, mainThread.Drain());
            Assert.AreEqual(1, startCount);
            Assert.AreEqual(1, endCount);
            Assert.AreEqual(1, syncCount);
            Assert.AreEqual((ulong)42, sync.Gift.PlayerId);
            Assert.AreEqual(2, sync.Gift.TroopSpawns.Count);
            Assert.AreEqual((uint)3, sync.Gift.TroopSpawns[0].TroopLevel);
            Assert.AreEqual(1, sync.Gift.BuffChanges.Count);
            Assert.AreEqual(PKCampBuffChangeType.Applied, sync.Gift.BuffChanges[0].ChangeType);
        }
    }

    public sealed class PlayerServiceTests
    {
        [Test]
        public void Router_DispatchesPlayerLifecycleAndGiftOnMainThread()
        {
            var router = new MessageRouter();
            var mainThread = new MainThreadDispatcher();
            var service = new PlayerService(router, mainThread);
            LivePlayerEnterNotify entered = null;
            LivePlayerLeaveNotify left = null;
            LivePlayerCampSelectedNotify selected = null;
            LivePlayerGiftNotify gifted = null;
            service.PlayerEntered += value => entered = value;
            service.PlayerLeft += value => left = value;
            service.PlayerCampSelected += value => selected = value;
            service.PlayerGifted += value => gifted = value;

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
                        Nickname = "Alice"
                    },
                    FirstEnter = true,
                    Modules = new PlayerModulesData
                    {
                        Troop = new TroopModuleData
                        {
                            SchemaVersion = 1,
                            Troops =
                            {
                                new TroopData
                                {
                                    TroopId = 10001,
                                    Level = 3,
                                    Exp = 25
                                }
                            }
                        }
                    }
                }.ToByteArray())));
            Assert.IsTrue(router.Dispatch(new NetPacket(
                RequestCode.S2CPlayerGift,
                ActionCode.None,
                13,
                new LivePlayerGiftNotify
                {
                    RoomId = "room-1",
                    PlayerId = 42,
                    GiftId = "13585",
                    GiftCount = 2,
                    GiftValue = 20,
                    EventId = "gift-event-1",
                    TroopSpawn = new GiftTroopSpawnData
                    {
                        TroopId = 10001,
                        TroopLevel = 3,
                        TroopCount = 2
                    }
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
            Assert.IsNull(gifted);
            Assert.AreEqual(4, mainThread.Drain());
            Assert.AreEqual((ulong)42, entered.Player.PlayerId);
            Assert.AreEqual("Alice", entered.Player.Nickname);
            Assert.NotNull(entered.Modules);
            Assert.NotNull(entered.Modules.Troop);
            Assert.AreEqual(1, entered.Modules.Troop.Troops.Count);
            Assert.AreEqual((uint)3, entered.Modules.Troop.Troops[0].Level);
            Assert.AreEqual((ulong)42, left.PlayerId);
            Assert.AreEqual("viewer_leave", left.Reason);
            Assert.AreEqual(LivePlayerCamp.Red, selected.Camp);
            Assert.AreEqual((ulong)42, gifted.PlayerId);
            Assert.AreEqual("13585", gifted.GiftId);
            Assert.NotNull(gifted.TroopSpawn);
            Assert.AreEqual((uint)10001, gifted.TroopSpawn.TroopId);
            Assert.AreEqual((uint)3, gifted.TroopSpawn.TroopLevel);
            Assert.AreEqual(2, gifted.TroopSpawn.TroopCount);
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

    public sealed class EquipmentServiceTests
    {
        [Test]
        public void Router_DispatchesEveryEquipmentResponseOnMainThread()
        {
            using var transport = new TcpTransport();
            var router = new MessageRouter();
            var mainThread = new MainThreadDispatcher();
            var service = new EquipmentService(transport, router, mainThread);

            S2CEquipmentQueryResponse query = null;
            S2CEquipmentCreateResponse create = null;
            S2CEquipmentUpgradeResponse upgrade = null;
            S2CEquipmentEquipResponse equip = null;
            S2CEquipmentUnequipResponse unequip = null;
            S2CEquipmentChangedNotify changed = null;
            service.QueryResponseReceived += value => query = value;
            service.CreateResponseReceived += value => create = value;
            service.UpgradeResponseReceived += value => upgrade = value;
            service.EquipResponseReceived += value => equip = value;
            service.UnequipResponseReceived += value => unequip = value;
            service.Changed += value => changed = value;

            Assert.IsTrue(router.Dispatch(new NetPacket(
                RequestCode.S2CEquipmentQuery,
                ActionCode.None,
                30,
                new S2CEquipmentQueryResponse
                {
                    Accepted = true,
                    PlayerId = 42,
                    ModuleVersion = 7,
                    Module = new EquipmentModuleData()
                }.ToByteArray())));
            Assert.IsTrue(router.Dispatch(new NetPacket(
                RequestCode.S2CEquipmentCreate,
                ActionCode.None,
                31,
                new S2CEquipmentCreateResponse
                {
                    Accepted = true,
                    PlayerId = 42,
                    Equipment = CreateEquipment(10001, 2001)
                }.ToByteArray())));
            Assert.IsTrue(router.Dispatch(new NetPacket(
                RequestCode.S2CEquipmentUpgrade,
                ActionCode.None,
                32,
                new S2CEquipmentUpgradeResponse
                {
                    Accepted = true,
                    PlayerId = 42,
                    Equipment = CreateEquipment(10001, 2001, 2)
                }.ToByteArray())));
            Assert.IsTrue(router.Dispatch(new NetPacket(
                RequestCode.S2CEquipmentEquip,
                ActionCode.None,
                33,
                new S2CEquipmentEquipResponse
                {
                    Accepted = true,
                    PlayerId = 42,
                    Equipment = CreateEquipment(10001, 2001, 2, 1)
                }.ToByteArray())));
            Assert.IsTrue(router.Dispatch(new NetPacket(
                RequestCode.S2CEquipmentUnequip,
                ActionCode.None,
                34,
                new S2CEquipmentUnequipResponse
                {
                    Accepted = true,
                    PlayerId = 42,
                    Equipment = CreateEquipment(10001, 2001, 2)
                }.ToByteArray())));
            Assert.IsTrue(router.Dispatch(new NetPacket(
                RequestCode.S2CEquipmentChanged,
                ActionCode.None,
                35,
                new S2CEquipmentChangedNotify
                {
                    PlayerId = 42,
                    ChangeType = EquipmentChangeType.Upgraded,
                    Equipment = CreateEquipment(10001, 2001, 3)
                }.ToByteArray())));

            Assert.IsNull(query);
            Assert.IsNull(create);
            Assert.IsNull(upgrade);
            Assert.IsNull(equip);
            Assert.IsNull(unequip);
            Assert.IsNull(changed);
            Assert.AreEqual(6, mainThread.Drain());
            Assert.AreEqual((ulong)42, query.PlayerId);
            Assert.AreEqual((ulong)10001, create.Equipment.EquipmentUid);
            Assert.AreEqual((uint)2, upgrade.Equipment.Level);
            Assert.AreEqual((uint)1, equip.Equipment.EquippedSlot);
            Assert.AreEqual((uint)0, unequip.Equipment.EquippedSlot);
            Assert.AreEqual(EquipmentChangeType.Upgraded, changed.ChangeType);
        }

        private static EquipmentData CreateEquipment(
            ulong equipmentUid,
            uint equipmentId,
            uint level = 1,
            uint equippedSlot = 0)
        {
            return new EquipmentData
            {
                EquipmentUid = equipmentUid,
                EquipmentId = equipmentId,
                Level = level,
                Star = 1,
                Quality = EquipmentQuality.Common,
                EquippedSlot = equippedSlot
            };
        }
    }
}
