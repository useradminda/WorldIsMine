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
    public sealed class PlayerService
    {
        private readonly TcpTransport _transport;
        private readonly TimeSpan _requestTimeout;
        private readonly MainThreadDispatcher _mainThread;
        private readonly RequestAwaiter<PlayerLoadResponse> _response =
            new RequestAwaiter<PlayerLoadResponse>();

        public event Action<PlayerLoadResponse> LoginCompleted;

        public PlayerService(
            TcpTransport transport,
            MessageRouter router,
            MainThreadDispatcher mainThread,
            TimeSpan requestTimeout)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _mainThread = mainThread ?? throw new ArgumentNullException(nameof(mainThread));
            _requestTimeout = requestTimeout;

            router.Register(
                RequestCode.User,
                ActionCode.Login,
                PlayerLoadResponse.Parser,
                OnResponse);
        }

        // Current server "Login" loads or creates player data; it is not token authentication.
        public async Task<PlayerLoadResponse> LoginOrCreateAsync(
            PlayerLoginOptions options,
            CancellationToken cancellationToken = default)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            options.Validate();

            var request = new PlayerLoadRequest
            {
                RoleId = options.RoleId,
                RoleType = options.RoleType,
                AccountId = string.IsNullOrWhiteSpace(options.AccountId)
                    ? options.RoleId
                    : options.AccountId,
                Platform = options.Platform ?? string.Empty,
                OpenId = string.IsNullOrWhiteSpace(options.OpenId)
                    ? options.RoleId
                    : options.OpenId,
                Nickname = string.IsNullOrWhiteSpace(options.Nickname)
                    ? options.RoleId
                    : options.Nickname,
                Avatar = options.Avatar ?? string.Empty,
                CreateIfMissing = options.CreateIfMissing
            };

            TaskCompletionSource<PlayerLoadResponse> pending = _response.Begin();
            try
            {
                await _transport.SendAsync(
                        RequestCode.User,
                        ActionCode.Login,
                        request.ToByteArray(),
                        cancellationToken)
                    .ConfigureAwait(false);
                return await RequestAwaiter<PlayerLoadResponse>.WaitAsync(
                        pending.Task,
                        _requestTimeout,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                _response.End(pending);
            }
        }

        internal void FailPending(Exception exception)
        {
            _response.Fail(exception);
        }

        private void OnResponse(PlayerLoadResponse response, NetPacket packet)
        {
            _response.Complete(response);
            _mainThread.Post(() => LoginCompleted?.Invoke(response));
        }
    }
}
