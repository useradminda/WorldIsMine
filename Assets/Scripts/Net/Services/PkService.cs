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
    public sealed class PkService
    {
        private const long DefaultDurationMs = 300_000;

        private readonly TcpTransport _transport;
        private readonly MainThreadDispatcher _mainThread;
        private readonly object _stateGate = new object();

        private string _anchorId = string.Empty;
        private string _roomId = string.Empty;
        private SessionSnapshot _currentSession;
        private bool _waitingForMatch;

        public PkService(
            TcpTransport transport,
            MessageRouter router,
            MainThreadDispatcher mainThread)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _mainThread = mainThread ?? throw new ArgumentNullException(nameof(mainThread));
            if (router == null)
                throw new ArgumentNullException(nameof(router));

            router.Register(
                RequestCode.S2CPkStart,
                ActionCode.None,
                PKStartClientResponse.Parser,
                OnStartResponse);
            router.Register(
                RequestCode.S2CPkEnd,
                ActionCode.None,
                SubmitGiftResponse.Parser,
                OnEndResponse);
            router.Register(
                RequestCode.S2CPkSync,
                ActionCode.None,
                SyncCommand.Parser,
                OnSyncCommand);
        }

        public event Action<PKStartClientResponse> StartResponseReceived;
        public event Action<SessionSnapshot> BattleStarted;
        public event Action<SessionSnapshot> BattleUpdated;
        public event Action<SubmitGiftResponse> BattleEnded;
        public event Action<SyncCommand> SyncCommandReceived;

        public bool WaitingForMatch
        {
            get
            {
                lock (_stateGate)
                    return _waitingForMatch;
            }
        }

        public SessionSnapshot CurrentSession
        {
            get
            {
                lock (_stateGate)
                    return _currentSession?.Clone();
            }
        }

        public Task<long> RegisterMatchAsync(
            long durationMs = DefaultDurationMs,
            CancellationToken cancellationToken = default)
        {
            string roomId = RequireBoundRoom();
            ValidateDuration(durationMs);

            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var request = new PKMatchmakingRegisterRequest
            {
                RequestId = $"match:{roomId}:{nowMs}",
                DurationMs = durationMs,
                EnqueueTimeMs = nowMs
            };

            return _transport.SendAsync(
                RequestCode.C2SPkSession,
                ActionCode.Match,
                request.ToByteArray(),
                cancellationToken);
        }

        public Task<long> CancelMatchAsync(
            string reason = "cancelled",
            CancellationToken cancellationToken = default)
        {
            RequireBoundRoom();
            var request = new PKMatchmakingCancelRequest
            {
                Reason = string.IsNullOrWhiteSpace(reason) ? "cancelled" : reason
            };

            return _transport.SendAsync(
                RequestCode.C2SPkSession,
                ActionCode.Four,
                request.ToByteArray(),
                cancellationToken);
        }

        public Task<long> StartDirectAsync(
            string targetRoomId,
            long durationMs = DefaultDurationMs,
            string sessionId = null,
            CancellationToken cancellationToken = default)
        {
            string roomId = RequireBoundRoom();
            if (string.IsNullOrWhiteSpace(targetRoomId))
                throw new ArgumentException("TargetRoomId is required.", nameof(targetRoomId));
            if (string.Equals(roomId, targetRoomId, StringComparison.Ordinal))
                throw new ArgumentException("TargetRoomId must be different from the bound room.", nameof(targetRoomId));
            ValidateDuration(durationMs);

            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var request = new PKStartClientRequest
            {
                SessionId = string.IsNullOrWhiteSpace(sessionId)
                    ? $"pk:{roomId}:{targetRoomId}:{nowMs}"
                    : sessionId,
                TargetRoomId = targetRoomId,
                StartTimeMs = nowMs,
                DurationMs = durationMs
            };

            return _transport.SendAsync(
                RequestCode.C2SPkSession,
                ActionCode.One,
                request.ToByteArray(),
                cancellationToken);
        }

        public Task<long> EndAsync(
            string sessionId = null,
            CancellationToken cancellationToken = default)
        {
            string roomId = RequireBoundRoom();
            string resolvedSessionId = sessionId;
            if (string.IsNullOrWhiteSpace(resolvedSessionId))
            {
                lock (_stateGate)
                    resolvedSessionId = _currentSession?.SessionId;
            }

            var request = new PKEndClientRequest
            {
                SessionId = resolvedSessionId ?? string.Empty,
                RoomId = string.IsNullOrWhiteSpace(resolvedSessionId) ? roomId : string.Empty,
                EndTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            return _transport.SendAsync(
                RequestCode.C2SPkSession,
                ActionCode.Two,
                request.ToByteArray(),
                cancellationToken);
        }

        internal void SetIdentity(string anchorId, string roomId)
        {
            lock (_stateGate)
            {
                _anchorId = anchorId ?? string.Empty;
                _roomId = roomId ?? string.Empty;
                _currentSession = null;
                _waitingForMatch = false;
            }
        }

        internal void Reset()
        {
            lock (_stateGate)
            {
                _anchorId = string.Empty;
                _roomId = string.Empty;
                _currentSession = null;
                _waitingForMatch = false;
            }
        }

        private void OnStartResponse(PKStartClientResponse response, NetPacket packet)
        {
            SessionSnapshot startedSession = null;
            lock (_stateGate)
            {
                if (response.Accepted && response.Snapshot != null)
                {
                    _currentSession = response.Snapshot.Clone();
                    _waitingForMatch = false;
                    startedSession = _currentSession.Clone();
                }
                else if (response.Accepted &&
                         (string.Equals(response.Reason, "match_queued", StringComparison.Ordinal) ||
                          string.Equals(response.Reason, "already_queued", StringComparison.Ordinal)))
                {
                    _waitingForMatch = true;
                }
                else
                {
                    _waitingForMatch = false;
                }
            }

            _mainThread.Post(() =>
            {
                StartResponseReceived?.Invoke(response);
                if (startedSession != null)
                    BattleStarted?.Invoke(startedSession);
            });
        }

        private void OnEndResponse(SubmitGiftResponse response, NetPacket packet)
        {
            if (response.Accepted)
            {
                lock (_stateGate)
                {
                    _currentSession = null;
                    _waitingForMatch = false;
                }
            }

            _mainThread.Post(() => BattleEnded?.Invoke(response));
        }

        private void OnSyncCommand(SyncCommand command, NetPacket packet)
        {
            SessionSnapshot updated = null;
            lock (_stateGate)
            {
                if (_currentSession != null &&
                    string.Equals(_currentSession.SessionId, command.SessionId, StringComparison.Ordinal))
                {
                    _currentSession.ScoreA = command.ScoreA;
                    _currentSession.ScoreB = command.ScoreB;
                    _currentSession.Sequence = Math.Max(_currentSession.Sequence, command.Sequence);
                    _currentSession.Status = command.Status;
                    if (command.PayloadCase == SyncCommand.PayloadOneofCase.Gift)
                    {
                        _currentSession.FightRank.Clear();
                        _currentSession.FightRank.Add(command.Gift.FightRank);
                    }
                    updated = _currentSession.Clone();
                }
            }

            _mainThread.Post(() =>
            {
                if (updated != null)
                    BattleUpdated?.Invoke(updated);
                SyncCommandReceived?.Invoke(command);
            });
        }

        private string RequireBoundRoom()
        {
            lock (_stateGate)
            {
                if (string.IsNullOrWhiteSpace(_anchorId) || string.IsNullOrWhiteSpace(_roomId))
                    throw new InvalidOperationException("Anchor must be bound before using PK operations.");

                return _roomId;
            }
        }

        private static void ValidateDuration(long durationMs)
        {
            if (durationMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(durationMs), "Duration must be greater than zero.");
        }
    }
}
