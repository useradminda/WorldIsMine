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
    public sealed class BindService
    {
        private readonly TcpTransport _transport;
        private readonly TimeSpan _requestTimeout;
        private readonly MainThreadDispatcher _mainThread;
        private readonly RequestAwaiter<ClientBindResponse> _response =
            new RequestAwaiter<ClientBindResponse>();

        public event Action<ClientBindResponse> BindCompleted;

        public BindService(
            TcpTransport transport,
            MessageRouter router,
            MainThreadDispatcher mainThread,
            TimeSpan requestTimeout)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _mainThread = mainThread ?? throw new ArgumentNullException(nameof(mainThread));
            _requestTimeout = requestTimeout;

            router.Register(
                RequestCode.Bind,
                ActionCode.None,
                ClientBindResponse.Parser,
                OnResponse);
        }

        public async Task<ClientBindResponse> BindAsync(
            BindOptions options,
            CancellationToken cancellationToken = default)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            options.Validate();

            var request = new ClientBindRequest
            {
                AnchorId = options.AnchorId,
                AnchorName = options.AnchorName ?? string.Empty,
                Platform = options.Platform ?? string.Empty,
                RoomId = options.RoomId,
                AuthTicket = options.AuthTicket ?? string.Empty
            };

            TaskCompletionSource<ClientBindResponse> pending = _response.Begin();
            try
            {
                await _transport.SendAsync(
                        RequestCode.Bind,
                        ActionCode.Bind,
                        request.ToByteArray(),
                        cancellationToken)
                    .ConfigureAwait(false);
                return await RequestAwaiter<ClientBindResponse>.WaitAsync(
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

        private void OnResponse(ClientBindResponse response, NetPacket packet)
        {
            _response.Complete(response);
            _mainThread.Post(() => BindCompleted?.Invoke(response));
        }
    }
}
