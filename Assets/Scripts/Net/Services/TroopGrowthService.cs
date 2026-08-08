using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using PlayerProtocol;
using WorldIsMine.Net.Protocol;
using WorldIsMine.Net.Runtime;
using WorldIsMine.Net.Transport;

namespace WorldIsMine.Net.Services
{
    public sealed class TroopGrowthService
    {
        private readonly TcpTransport _transport;
        private readonly MainThreadDispatcher _mainThread;

        public TroopGrowthService(
            TcpTransport transport,
            MessageRouter router,
            MainThreadDispatcher mainThread)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _mainThread = mainThread ?? throw new ArgumentNullException(nameof(mainThread));
            if (router == null)
                throw new ArgumentNullException(nameof(router));

            router.Register(
                RequestCode.S2CTroopQuery,
                ActionCode.None,
                S2CTroopQueryResponse.Parser,
                OnQueryResponse);
            router.Register(
                RequestCode.S2CTroopUpgrade,
                ActionCode.None,
                S2CTroopUpgradeResponse.Parser,
                OnUpgradeResponse);
        }

        public event Action<S2CTroopQueryResponse> QueryResponseReceived;
        public event Action<S2CTroopUpgradeResponse> UpgradeResponseReceived;

        public Task<long> QueryAsync(
            ulong playerId,
            CancellationToken cancellationToken = default)
        {
            EnsurePlayerId(playerId);
            return SendAsync(
                RequestCode.C2STroopQuery,
                new C2STroopQueryRequest { PlayerId = playerId },
                cancellationToken);
        }

        public Task<long> UpgradeAsync(
            string operationId,
            ulong playerId,
            uint troopId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(operationId) || operationId.Length > 128)
                throw new ArgumentException("OperationId must contain 1-128 characters.", nameof(operationId));
            EnsurePlayerId(playerId);
            if (troopId == 0)
                throw new ArgumentOutOfRangeException(nameof(troopId));
            return SendAsync(
                RequestCode.C2STroopUpgrade,
                new C2STroopUpgradeRequest
                {
                    OperationId = operationId,
                    PlayerId = playerId,
                    TroopId = troopId
                },
                cancellationToken);
        }

        private Task<long> SendAsync(
            RequestCode requestCode,
            IMessage request,
            CancellationToken cancellationToken) =>
            _transport.SendAsync(
                requestCode,
                ActionCode.None,
                request.ToByteArray(),
                cancellationToken);

        private void OnQueryResponse(S2CTroopQueryResponse response, NetPacket packet)
        {
            _mainThread.Post(() => QueryResponseReceived?.Invoke(response));
        }

        private void OnUpgradeResponse(S2CTroopUpgradeResponse response, NetPacket packet)
        {
            _mainThread.Post(() => UpgradeResponseReceived?.Invoke(response));
        }

        private static void EnsurePlayerId(ulong playerId)
        {
            if (playerId == 0)
                throw new ArgumentOutOfRangeException(nameof(playerId));
        }
    }
}
