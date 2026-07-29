using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using WorldIsMine.Net.Protocol;

namespace WorldIsMine.Net.Transport
{
    public sealed class TcpTransport : IDisposable
    {
        private readonly PacketReader _reader = new PacketReader();
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        private readonly object _stateLock = new object();

        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _lifetime;
        private Task _readTask;
        private long _nextMessageId;
        private TransportState _state = TransportState.Disconnected;
        private bool _disposed;

        public event Action<TransportState> StateChanged;
        public event Action<NetPacket> PacketSent;
        public event Action<NetPacket> PacketReceived;
        public event Action<Exception> Error;

        public TransportState State
        {
            get
            {
                lock (_stateLock)
                    return _state;
            }
        }

        public async Task ConnectAsync(
            string host,
            int port,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (State == TransportState.Connected || State == TransportState.Connecting)
                throw new InvalidOperationException($"Transport is already {State}.");

            await StopAsync().ConfigureAwait(false);
            SetState(TransportState.Connecting);

            _reader.Reset();
            Interlocked.Exchange(ref _nextMessageId, 0);
            _lifetime = new CancellationTokenSource();
            _client = new TcpClient { NoDelay = true };

            try
            {
                Task connectTask = _client.ConnectAsync(host, port);
                Task timeoutTask = Task.Delay(timeout, cancellationToken);
                Task completed = await Task.WhenAny(connectTask, timeoutTask).ConfigureAwait(false);
                if (completed != connectTask)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    throw new TimeoutException($"Connect to {host}:{port} timed out after {timeout}.");
                }

                await connectTask.ConfigureAwait(false);
                _stream = _client.GetStream();
                SetState(TransportState.Connected);
                _readTask = Task.Run(() => ReadLoopAsync(_lifetime.Token));
            }
            catch
            {
                CloseCore();
                SetState(TransportState.Disconnected);
                throw;
            }
        }

        public async Task<long> SendAsync(
            RequestCode requestCode,
            ActionCode actionCode,
            byte[] body,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            NetworkStream stream = _stream;
            if (State != TransportState.Connected || stream == null)
                throw new InvalidOperationException("Transport is not connected.");

            await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                long messageId = Interlocked.Increment(ref _nextMessageId);
                byte[] frame = PacketCodec.Encode(requestCode, actionCode, messageId, body);
                await stream.WriteAsync(frame, 0, frame.Length, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                PacketSent?.Invoke(new NetPacket(requestCode, actionCode, messageId, body));
                return messageId;
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public async Task StopAsync()
        {
            Task readTask = _readTask;
            if (_client == null && readTask == null)
            {
                if (State != TransportState.Stopped)
                    SetState(TransportState.Stopped);
                return;
            }

            SetState(TransportState.Stopping);
            CloseCore();
            if (readTask != null)
            {
                try
                {
                    await readTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    Error?.Invoke(ex);
                }
            }

            _readTask = null;
            SetState(TransportState.Stopped);
        }

        private async Task ReadLoopAsync(CancellationToken cancellationToken)
        {
            var chunk = new byte[8192];
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    NetworkStream stream = _stream;
                    if (stream == null)
                        return;

                    int count = await stream.ReadAsync(chunk, 0, chunk.Length, cancellationToken)
                        .ConfigureAwait(false);
                    if (count <= 0)
                        throw new EndOfStreamException("Server closed the TCP connection.");

                    _reader.Append(chunk, 0, count);
                    while (true)
                    {
                        PacketReadStatus status = _reader.TryRead(out NetPacket packet);
                        if (status == PacketReadStatus.NeedMoreData)
                            break;
                        if (status == PacketReadStatus.InvalidPacket)
                            throw new InvalidDataException("Server sent an invalid packet length.");

                        PacketReceived?.Invoke(packet);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex);
            }
            finally
            {
                CloseCore();
                SetState(TransportState.Disconnected);
            }
        }

        private void CloseCore()
        {
            try
            {
                _lifetime?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }

            try
            {
                _stream?.Close();
            }
            catch
            {
            }

            try
            {
                _client?.Close();
            }
            catch
            {
            }

            _stream = null;
            _client = null;
            _lifetime?.Dispose();
            _lifetime = null;
            _reader.Reset();
        }

        private void SetState(TransportState state)
        {
            bool changed;
            lock (_stateLock)
            {
                changed = _state != state;
                _state = state;
            }

            if (changed)
                StateChanged?.Invoke(state);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TcpTransport));
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            StopAsync().GetAwaiter().GetResult();
            _sendLock.Dispose();
            _disposed = true;
        }
    }
}
