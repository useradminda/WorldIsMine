using System;
using System.Threading;
using System.Threading.Tasks;
using ClientProtocol;
using Google.Protobuf;
using WorldIsMine.Net.Protocol;
using WorldIsMine.Net.Runtime;
using WorldIsMine.Net.Transport;

namespace WorldIsMine.Net.Services
{
    public sealed class ScoreRankService
    {
        private readonly TcpTransport _transport;
        private readonly MainThreadDispatcher _mainThread;

        public ScoreRankService(
            TcpTransport transport,
            MessageRouter router,
            MainThreadDispatcher mainThread)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _mainThread = mainThread ?? throw new ArgumentNullException(nameof(mainThread));
            if (router == null)
                throw new ArgumentNullException(nameof(router));

            router.Register(
                RequestCode.S2CScoreRankQuery,
                ActionCode.None,
                S2CScoreRankQueryResponse.Parser,
                OnResponse);
        }

        public event Action<S2CScoreRankQueryResponse> ResponseReceived;

        public Task<long> QueryAsync(
            int limit = 10,
            CancellationToken cancellationToken = default)
        {
            var request = new C2SScoreRankQueryRequest
            {
                Limit = Math.Max(1, Math.Min(limit, 50))
            };
            return _transport.SendAsync(
                RequestCode.C2SScoreRankQuery,
                ActionCode.None,
                request.ToByteArray(),
                cancellationToken);
        }

        private void OnResponse(S2CScoreRankQueryResponse response, NetPacket packet)
        {
            _mainThread.Post(() => ResponseReceived?.Invoke(response));
        }
    }
}
