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
    public sealed class HeartbeatService : IDisposable
    {
        private readonly TcpTransport _transport;
        private readonly TimeSpan _interval;
        private readonly MainThreadDispatcher _mainThread;
        private CancellationTokenSource _lifetime;
        private Task _loop;
        private string _clientId = string.Empty;
        private string _roomId = string.Empty;

        public event Action<ClientHeartbeat> HeartbeatReceived;
        public event Action<Exception> Error;

        public HeartbeatService(
            TcpTransport transport,
            MessageRouter router,
            MainThreadDispatcher mainThread,
            TimeSpan interval)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _mainThread = mainThread ?? throw new ArgumentNullException(nameof(mainThread));
            _interval = interval;

            router.Register(
                RequestCode.Beat,
                ActionCode.None,
                ClientHeartbeat.Parser,
                OnResponse);
        }

        public void Start(string clientId, string roomId)
        {
            Stop();
            _clientId = clientId ?? string.Empty;
            _roomId = roomId ?? string.Empty;
            _lifetime = new CancellationTokenSource();
            _loop = Task.Run(() => RunAsync(_lifetime.Token));
        }

        public void Stop()
        {
            try
            {
                _lifetime?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }

            _lifetime?.Dispose();
            _lifetime = null;
            _loop = null;
        }

        public async Task SendOnceAsync(CancellationToken cancellationToken = default)
        {
            var heartbeat = new ClientHeartbeat
            {
                ClientId = _clientId,
                RoomId = _roomId,
                Tag = "unity",
                ClientTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            await _transport.SendAsync(
                    RequestCode.Beat,
                    ActionCode.Beat,
                    heartbeat.ToByteArray(),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(_interval, cancellationToken).ConfigureAwait(false);
                    await SendOnceAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _mainThread.Post(() => Error?.Invoke(ex));
            }
        }

        private void OnResponse(ClientHeartbeat heartbeat, NetPacket packet)
        {
            _mainThread.Post(() => HeartbeatReceived?.Invoke(heartbeat));
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
