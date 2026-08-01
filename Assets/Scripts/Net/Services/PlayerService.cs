using System;
using System.Threading;
using System.Threading.Tasks;
using ClientProtocol;
using Google.Protobuf;
using PlayerProtocol;
using WorldIsMine.Net.Protocol;
using WorldIsMine.Net.Runtime;
using WorldIsMine.Net.Transport;

namespace WorldIsMine.Net.Services
{
    public sealed class PlayerService
    {
        private readonly MainThreadDispatcher _mainThread;

        public PlayerService(
            MessageRouter router,
            MainThreadDispatcher mainThread)
        {
            if (router == null)
                throw new ArgumentNullException(nameof(router));
            _mainThread = mainThread ?? throw new ArgumentNullException(nameof(mainThread));

            router.Register(
                RequestCode.S2CPlayerEnter,
                ActionCode.None,
                LivePlayerEnterNotify.Parser,
                OnPlayerEnter);
            router.Register(
                RequestCode.S2CPlayerLeave,
                ActionCode.None,
                LivePlayerLeaveNotify.Parser,
                OnPlayerLeave);
            router.Register(
                RequestCode.S2CPlayerCampSelected,
                ActionCode.None,
                LivePlayerCampSelectedNotify.Parser,
                OnPlayerCampSelected);
            router.Register(
                RequestCode.S2CPlayerGift,
                ActionCode.None,
                LivePlayerGiftNotify.Parser,
                OnPlayerGift);
        }

        public event Action<LivePlayerEnterNotify> PlayerEntered;
        public event Action<LivePlayerLeaveNotify> PlayerLeft;
        public event Action<LivePlayerCampSelectedNotify> PlayerCampSelected;
        public event Action<LivePlayerGiftNotify> PlayerGifted;

        private void OnPlayerEnter(LivePlayerEnterNotify notify, NetPacket packet)
        {
            _mainThread.Post(() => PlayerEntered?.Invoke(notify));
        }

        private void OnPlayerLeave(LivePlayerLeaveNotify notify, NetPacket packet)
        {
            _mainThread.Post(() => PlayerLeft?.Invoke(notify));
        }

        private void OnPlayerCampSelected(
            LivePlayerCampSelectedNotify notify,
            NetPacket packet)
        {
            _mainThread.Post(() => PlayerCampSelected?.Invoke(notify));
        }

        private void OnPlayerGift(LivePlayerGiftNotify notify, NetPacket packet)
        {
            _mainThread.Post(() => PlayerGifted?.Invoke(notify));
        }
    }

    public sealed class LiveTestService
    {
        private readonly TcpTransport _transport;
        private readonly MainThreadDispatcher _mainThread;

        public LiveTestService(
            TcpTransport transport,
            MessageRouter router,
            MainThreadDispatcher mainThread)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _mainThread = mainThread ?? throw new ArgumentNullException(nameof(mainThread));
            if (router == null)
                throw new ArgumentNullException(nameof(router));

            router.Register(
                RequestCode.S2CLiveClientTest,
                ActionCode.None,
                LiveClientTestResponse.Parser,
                OnResponse);
        }

        public event Action<LiveClientTestResponse> ResponseReceived;

        public Task<long> SendAsync(
            LiveClientTestRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return _transport.SendAsync(
                RequestCode.C2SLiveClientTest,
                ActionCode.None,
                request.ToByteArray(),
                cancellationToken);
        }

        private void OnResponse(LiveClientTestResponse response, NetPacket packet)
        {
            _mainThread.Post(() => ResponseReceived?.Invoke(response));
        }
    }

    public sealed class EquipmentService
    {
        private readonly TcpTransport _transport;
        private readonly MainThreadDispatcher _mainThread;

        public EquipmentService(
            TcpTransport transport,
            MessageRouter router,
            MainThreadDispatcher mainThread)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _mainThread = mainThread ?? throw new ArgumentNullException(nameof(mainThread));
            if (router == null)
                throw new ArgumentNullException(nameof(router));

            router.Register(
                RequestCode.S2CEquipmentQuery,
                ActionCode.None,
                S2CEquipmentQueryResponse.Parser,
                OnQueryResponse);
            router.Register(
                RequestCode.S2CEquipmentCreate,
                ActionCode.None,
                S2CEquipmentCreateResponse.Parser,
                OnCreateResponse);
            router.Register(
                RequestCode.S2CEquipmentUpgrade,
                ActionCode.None,
                S2CEquipmentUpgradeResponse.Parser,
                OnUpgradeResponse);
            router.Register(
                RequestCode.S2CEquipmentEquip,
                ActionCode.None,
                S2CEquipmentEquipResponse.Parser,
                OnEquipResponse);
            router.Register(
                RequestCode.S2CEquipmentUnequip,
                ActionCode.None,
                S2CEquipmentUnequipResponse.Parser,
                OnUnequipResponse);
            router.Register(
                RequestCode.S2CEquipmentChanged,
                ActionCode.None,
                S2CEquipmentChangedNotify.Parser,
                OnChanged);
        }

        public event Action<S2CEquipmentQueryResponse> QueryResponseReceived;
        public event Action<S2CEquipmentCreateResponse> CreateResponseReceived;
        public event Action<S2CEquipmentUpgradeResponse> UpgradeResponseReceived;
        public event Action<S2CEquipmentEquipResponse> EquipResponseReceived;
        public event Action<S2CEquipmentUnequipResponse> UnequipResponseReceived;
        public event Action<S2CEquipmentChangedNotify> Changed;

        public Task<long> QueryAsync(
            ulong playerId,
            CancellationToken cancellationToken = default)
        {
            EnsurePlayerId(playerId);
            return SendAsync(
                RequestCode.C2SEquipmentQuery,
                new C2SEquipmentQueryRequest { PlayerId = playerId },
                cancellationToken);
        }

        public Task<long> CreateAsync(
            string operationId,
            ulong playerId,
            uint equipmentId,
            CancellationToken cancellationToken = default)
        {
            EnsureOperationId(operationId);
            EnsurePlayerId(playerId);
            if (equipmentId == 0)
                throw new ArgumentOutOfRangeException(
                    nameof(equipmentId),
                    "EquipmentId must be greater than zero.");

            return SendAsync(
                RequestCode.C2SEquipmentCreate,
                new C2SEquipmentCreateRequest
                {
                    OperationId = operationId,
                    PlayerId = playerId,
                    EquipmentId = equipmentId
                },
                cancellationToken);
        }

        public Task<long> UpgradeAsync(
            string operationId,
            ulong playerId,
            ulong equipmentUid,
            CancellationToken cancellationToken = default)
        {
            EnsureOperationId(operationId);
            EnsurePlayerId(playerId);
            EnsureEquipmentUid(equipmentUid);

            return SendAsync(
                RequestCode.C2SEquipmentUpgrade,
                new C2SEquipmentUpgradeRequest
                {
                    OperationId = operationId,
                    PlayerId = playerId,
                    EquipmentUid = equipmentUid
                },
                cancellationToken);
        }

        public Task<long> EquipAsync(
            string operationId,
            ulong playerId,
            ulong equipmentUid,
            uint targetSlot,
            CancellationToken cancellationToken = default)
        {
            EnsureOperationId(operationId);
            EnsurePlayerId(playerId);
            EnsureEquipmentUid(equipmentUid);
            if (targetSlot == 0)
                throw new ArgumentOutOfRangeException(
                    nameof(targetSlot),
                    "TargetSlot must be greater than zero.");

            return SendAsync(
                RequestCode.C2SEquipmentEquip,
                new C2SEquipmentEquipRequest
                {
                    OperationId = operationId,
                    PlayerId = playerId,
                    EquipmentUid = equipmentUid,
                    TargetSlot = targetSlot
                },
                cancellationToken);
        }

        public Task<long> UnequipAsync(
            string operationId,
            ulong playerId,
            ulong equipmentUid,
            CancellationToken cancellationToken = default)
        {
            EnsureOperationId(operationId);
            EnsurePlayerId(playerId);
            EnsureEquipmentUid(equipmentUid);

            return SendAsync(
                RequestCode.C2SEquipmentUnequip,
                new C2SEquipmentUnequipRequest
                {
                    OperationId = operationId,
                    PlayerId = playerId,
                    EquipmentUid = equipmentUid
                },
                cancellationToken);
        }

        private Task<long> SendAsync(
            RequestCode requestCode,
            IMessage request,
            CancellationToken cancellationToken)
        {
            return _transport.SendAsync(
                requestCode,
                ActionCode.None,
                request.ToByteArray(),
                cancellationToken);
        }

        private void OnQueryResponse(S2CEquipmentQueryResponse response, NetPacket packet)
        {
            _mainThread.Post(() => QueryResponseReceived?.Invoke(response));
        }

        private void OnCreateResponse(S2CEquipmentCreateResponse response, NetPacket packet)
        {
            _mainThread.Post(() => CreateResponseReceived?.Invoke(response));
        }

        private void OnUpgradeResponse(S2CEquipmentUpgradeResponse response, NetPacket packet)
        {
            _mainThread.Post(() => UpgradeResponseReceived?.Invoke(response));
        }

        private void OnEquipResponse(S2CEquipmentEquipResponse response, NetPacket packet)
        {
            _mainThread.Post(() => EquipResponseReceived?.Invoke(response));
        }

        private void OnUnequipResponse(S2CEquipmentUnequipResponse response, NetPacket packet)
        {
            _mainThread.Post(() => UnequipResponseReceived?.Invoke(response));
        }

        private void OnChanged(S2CEquipmentChangedNotify notify, NetPacket packet)
        {
            _mainThread.Post(() => Changed?.Invoke(notify));
        }

        private static void EnsureOperationId(string operationId)
        {
            if (string.IsNullOrWhiteSpace(operationId))
                throw new ArgumentException("OperationId is required.", nameof(operationId));
        }

        private static void EnsurePlayerId(ulong playerId)
        {
            if (playerId == 0)
                throw new ArgumentOutOfRangeException(
                    nameof(playerId),
                    "PlayerId must be greater than zero.");
        }

        private static void EnsureEquipmentUid(ulong equipmentUid)
        {
            if (equipmentUid == 0)
                throw new ArgumentOutOfRangeException(
                    nameof(equipmentUid),
                    "EquipmentUid must be greater than zero.");
        }
    }
}
