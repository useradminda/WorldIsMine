using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ClientProtocol;
using Google.Protobuf;
using WorldIsMine.Net.Config;
using WorldIsMine.Net.Protocol;
using WorldIsMine.Net.Services;
using WorldIsMine.Net.Transport;

namespace WorldIsMine.Net.Runtime
{
    public sealed class AnchorSessionStartResult
    {
        public AnchorSessionStartResult(ClientBindResponse bind)
        {
            Bind = bind;
        }

        public ClientBindResponse Bind { get; }
        public bool Success => Bind?.Accepted == true;
        public string Reason => Bind?.Accepted == false ? Bind.Reason : string.Empty;
    }

    public sealed class NetworkClient : IDisposable
    {
        private readonly NetworkConfig _config;
        private readonly MessageRouter _router;
        private readonly TcpTransport _transport;
        private bool _disposed;

        public NetworkClient(NetworkConfig config, MainThreadDispatcher mainThread)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            MainThread = mainThread ?? throw new ArgumentNullException(nameof(mainThread));
            _config.Validate();

            _router = new MessageRouter();
            _transport = new TcpTransport();
            Player = new PlayerService(_router, MainThread);
            LiveTest = new LiveTestService(_transport, _router, MainThread);
            Bind = new BindService(_transport, _router, MainThread, _config.RequestTimeout);
            Pk = new PkService(_transport, _router, MainThread);
            Heartbeat = new HeartbeatService(
                _transport,
                _router,
                MainThread,
                _config.HeartbeatInterval);

            _transport.PacketReceived += OnPacketReceived;
            _transport.PacketSent += OnPacketSent;
            _transport.StateChanged += OnTransportStateChanged;
            _transport.Error += OnTransportError;
        }

        public event Action<TransportState> TransportStateChanged;
        public event Action<BindOptions> BindStarted;
        public event Action<NetPacket> PacketSent;
        public event Action<NetPacket> PacketReceived;
        public event Action<NetPacket> UnhandledPacket;
        public event Action<Exception> Error;

        public MainThreadDispatcher MainThread { get; }
        public PlayerService Player { get; }
        public LiveTestService LiveTest { get; }
        public BindService Bind { get; }
        public PkService Pk { get; }
        public HeartbeatService Heartbeat { get; }
        public TransportState State => _transport.State;

        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return _transport.ConnectAsync(
                _config.Host,
                _config.Port,
                _config.ConnectTimeout,
                cancellationToken);
        }

        public async Task<AnchorSessionStartResult> ConnectAndBindAsync(
            BindOptions bind,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            await ConnectAsync(cancellationToken).ConfigureAwait(false);

            MainThread.Post(() => BindStarted?.Invoke(bind));
            ClientBindResponse bindResponse =
                await Bind.BindAsync(bind, cancellationToken).ConfigureAwait(false);
            if (bindResponse.Accepted)
            {
                Pk.SetIdentity(bindResponse.AnchorId, bindResponse.RoomId);
                Heartbeat.Start(bindResponse.AnchorId, bindResponse.RoomId);
            }

            return new AnchorSessionStartResult(bindResponse);
        }

        public async Task StopAsync()
        {
            Heartbeat.Stop();
            await _transport.StopAsync().ConfigureAwait(false);
        }

        private void OnPacketReceived(NetPacket packet)
        {
            MainThread.Post(() => PacketReceived?.Invoke(packet));

            try
            {
                if (!_router.Dispatch(packet))
                    MainThread.Post(() => UnhandledPacket?.Invoke(packet));
            }
            catch (InvalidProtocolBufferException ex)
            {
                MainThread.Post(() => Error?.Invoke(ex));
            }
            catch (Exception ex)
            {
                MainThread.Post(() => Error?.Invoke(ex));
            }
        }

        private void OnPacketSent(NetPacket packet)
        {
            MainThread.Post(() => PacketSent?.Invoke(packet));
        }

        private void OnTransportStateChanged(TransportState state)
        {
            if (state == TransportState.Disconnected)
            {
                var exception = new IOException("Network connection was closed.");
                Bind.FailPending(exception);
                Pk.Reset();
                Heartbeat.Stop();
            }

            MainThread.Post(() => TransportStateChanged?.Invoke(state));
        }

        private void OnTransportError(Exception exception)
        {
            Bind.FailPending(exception);
            MainThread.Post(() => Error?.Invoke(exception));
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NetworkClient));
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Heartbeat.Dispose();
            _transport.Dispose();
            _disposed = true;
        }
    }
}
