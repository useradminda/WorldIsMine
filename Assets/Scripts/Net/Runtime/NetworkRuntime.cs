using System;
using System.IO;
using System.Threading.Tasks;
using ClientProtocol;
using Google.Protobuf;
using UnityEngine;
using WorldIsMine.Net.Config;
using WorldIsMine.Net.Protocol;
using WorldIsMine.Net.Services;

namespace WorldIsMine.Net.Runtime
{
    public sealed class NetworkRuntime : MonoBehaviour
    {
        [Header("Connection")]
        [SerializeField] private NetworkConfig network = new NetworkConfig();
        [Header("DY Anchor")]
        [SerializeField] private bool testMode = true;
        [SerializeField] private string testIdentityMarkdownPath = "TestData/dy-test-identity.md";
        [SerializeField] private bool connectOnStart = false;
        [Header("PK")]
        [SerializeField] private bool autoMatchAfterBind = false;
        [SerializeField] private int defaultPkDurationSeconds = 300;
        [Header("Logging")]
        [SerializeField] private bool logPkProtocolDetails = true;

        private MainThreadDispatcher _mainThread;

        public NetworkClient Client { get; private set; }
        public AnchorSessionStartResult LastStartResult { get; private set; }
        public bool TestMode => testMode;
        public string TestIdentityPath => ResolveTestIdentityPath();
        public bool PkProtocolDetailsEnabled
        {
            get => logPkProtocolDetails;
            set => logPkProtocolDetails = value;
        }
        public event Action<SessionSnapshot> PkBattleStarted;
        public event Action<SubmitGiftResponse> PkBattleEnded;
        public event Action<SyncCommand> PkSyncReceived;

        private void Awake()
        {
            _mainThread = new MainThreadDispatcher();
            Client = new NetworkClient(network, _mainThread);
            Client.Error += exception => Debug.LogException(exception);
            Client.TransportStateChanged +=
                state => Debug.Log($"[Net][State] Transport: {state}");
            Client.BindStarted += options => Debug.Log(
                $"[Net][C->S] Sending Bind. AnchorId={options.AnchorId}, RoomId={options.RoomId}");
            Client.PacketSent += packet => LogPacket("C->S", packet);
            Client.PacketReceived += packet => LogPacket("S->C", packet);
            Client.Pk.StartResponseReceived += OnPkStartResponse;
            Client.Pk.BattleStarted += OnPkBattleStarted;
            Client.Pk.BattleEnded += OnPkBattleEnded;
            Client.Pk.SyncCommandReceived += OnPkSyncCommand;
        }

        private async void Start()
        {
            if (connectOnStart)
                await StartConfiguredSessionAsync();
        }

        private void Update()
        {
            _mainThread?.Drain();
        }

        public Task<AnchorSessionStartResult> StartConfiguredSessionAsync()
        {
            if (!testMode)
            {
                throw new InvalidOperationException(
                    "TestMode is disabled. Pass AnchorId and RoomId from the DY integration " +
                    "to StartAnchorSessionAsync.");
            }

            return StartTestSessionAsync();
        }

        public Task<AnchorSessionStartResult> StartTestSessionAsync()
        {
            string path = ResolveTestIdentityPath();
            if (!File.Exists(path))
            {
                DyTestIdentityStore.WriteTemplate(path);
                throw new FileNotFoundException(
                    $"Created DY test identity template. Fill AnchorId and RoomId, then retry: {path}",
                    path);
            }

            DyAnchorIdentity identity = DyTestIdentityStore.Load(path);
            return StartAnchorSessionAsync(identity.AnchorId, identity.RoomId);
        }

        public async Task<AnchorSessionStartResult> StartAnchorSessionAsync(
            string anchorId,
            string roomId)
        {
            try
            {
                Debug.Log(
                    $"[Net][Flow] Starting anchor session. Server={network.Host}:{network.Port}, " +
                    $"AnchorId={anchorId}, RoomId={roomId}");

                var bind = new BindOptions
                {
                    AnchorId = anchorId,
                    AnchorName = anchorId,
                    Platform = "dy",
                    RoomId = roomId
                };

                Debug.Log($"[Net][C->S] Connecting to {network.Host}:{network.Port}.");
                LastStartResult = await Client.ConnectAndBindAsync(bind);
                if (!LastStartResult.Success)
                {
                    Debug.LogError(
                        $"[Net][S->C] Bind rejected. AnchorId={anchorId}, RoomId={roomId}, " +
                        $"Reason={LastStartResult.Reason}");
                }
                else
                {
                    Debug.Log(
                        $"[Net][S->C] Bind accepted. AnchorId={LastStartResult.Bind.AnchorId}, " +
                        $"RoomId={LastStartResult.Bind.RoomId}");
                    Debug.Log(
                        $"[Net][Flow] Heartbeat started. Interval={network.HeartbeatIntervalSeconds}s");

                    if (autoMatchAfterBind)
                        await RegisterPkMatchAsync();
                }

                return LastStartResult;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                throw;
            }
        }

        public void SaveTestIdentity(string anchorId, string roomId)
        {
            DyTestIdentityStore.Save(
                ResolveTestIdentityPath(),
                new DyAnchorIdentity(anchorId, roomId));
        }

        public Task StopClientAsync()
        {
            Debug.Log("[Net][Flow] Stopping network client.");
            return Client == null ? Task.CompletedTask : Client.StopAsync();
        }

        public Task<long> RegisterPkMatchAsync()
        {
            EnsurePkReady();
            long durationMs = ResolvePkDurationMs();
            LogPkDetail("C->S", $"Registering match. Duration={durationMs}ms");
            return Client.Pk.RegisterMatchAsync(durationMs);
        }

        public Task<long> CancelPkMatchAsync(string reason = "cancelled")
        {
            EnsurePkReady();
            LogPkDetail("C->S", $"Cancelling match. Reason={reason}");
            return Client.Pk.CancelMatchAsync(reason);
        }

        public Task<long> StartPkWithRoomAsync(string targetRoomId)
        {
            EnsurePkReady();
            long durationMs = ResolvePkDurationMs();
            LogPkDetail(
                "C->S",
                $"Starting with room. TargetRoomId={targetRoomId}, " +
                $"Duration={durationMs}ms");
            return Client.Pk.StartDirectAsync(targetRoomId, durationMs);
        }

        public Task<long> EndPkAsync(string sessionId = null)
        {
            EnsurePkReady();
            LogPkDetail(
                "C->S",
                string.IsNullOrWhiteSpace(sessionId)
                    ? "Ending PK for the bound room."
                    : $"Ending PK. SessionId={sessionId}");
            return Client.Pk.EndAsync(sessionId);
        }

        private void OnPkStartResponse(PKStartClientResponse response)
        {
            string sessionId = response.Snapshot?.SessionId ?? string.Empty;
            LogPkDetail(
                "S->C",
                $"Start response. Accepted={response.Accepted}, " +
                $"Reason={response.Reason}, SessionId={sessionId}");
        }

        private void OnPkBattleStarted(SessionSnapshot snapshot)
        {
            Debug.Log(
                $"[Net][Flow] PK battle started. SessionId={snapshot.SessionId}, " +
                $"RoomA={snapshot.AnchorA?.RoomId}, RoomB={snapshot.AnchorB?.RoomId}, " +
                $"Score={snapshot.ScoreA}:{snapshot.ScoreB}");
            PkBattleStarted?.Invoke(snapshot);
        }

        private void OnPkBattleEnded(SubmitGiftResponse response)
        {
            Debug.Log(
                $"[Net][Flow] PK battle ended. Accepted={response.Accepted}, " +
                $"Reason={response.Reason}, SessionId={response.SessionId}");
            PkBattleEnded?.Invoke(response);
        }

        private void OnPkSyncCommand(SyncCommand command)
        {
            PkSyncReceived?.Invoke(command);
        }

        private void EnsurePkReady()
        {
            if (Client == null)
                throw new InvalidOperationException("Network client is not initialized.");
            if (LastStartResult?.Success != true)
                throw new InvalidOperationException("Anchor session must be bound before using PK operations.");
        }

        private long ResolvePkDurationMs()
        {
            if (defaultPkDurationSeconds <= 0)
                throw new InvalidOperationException("Default PK duration must be greater than zero.");

            return checked((long)defaultPkDurationSeconds * 1000L);
        }

        private void LogPacket(string direction, NetPacket packet)
        {
            Debug.Log(
                $"[Net][{direction}] " +
                $"Request={packet.RequestCode}({(int)packet.RequestCode}), " +
                $"Action={packet.ActionCode}({(int)packet.ActionCode}), " +
                $"MsgId={packet.MessageId}, Body={packet.Body.Length}B");

            if (!logPkProtocolDetails || !IsPkPacket(packet))
                return;

            try
            {
                IMessage message = ParsePkMessage(packet, out string operation);
                Debug.Log(
                    $"[Net][{direction}][PK] Operation={operation}, " +
                    $"Request={packet.RequestCode}({(int)packet.RequestCode}), " +
                    $"Action={packet.ActionCode}({(int)packet.ActionCode}), " +
                    $"MsgId={packet.MessageId}, Type={message.Descriptor.Name}, " +
                    $"Payload={message}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[Net][{direction}][PK] Failed to parse packet. " +
                    $"Request={packet.RequestCode}({(int)packet.RequestCode}), " +
                    $"Action={packet.ActionCode}({(int)packet.ActionCode}), " +
                    $"MsgId={packet.MessageId}, Body={packet.Body.Length}B, Error={ex.Message}");
            }
        }

        private void LogPkDetail(string direction, string message)
        {
            if (logPkProtocolDetails)
                Debug.Log($"[Net][{direction}][PK] {message}");
        }

        private static bool IsPkPacket(NetPacket packet)
        {
            return packet.RequestCode == RequestCode.C2SPkSession ||
                   packet.RequestCode == RequestCode.S2CPkStart ||
                   packet.RequestCode == RequestCode.S2CPkEnd ||
                   packet.RequestCode == RequestCode.S2CPkSync;
        }

        private static IMessage ParsePkMessage(NetPacket packet, out string operation)
        {
            switch (packet.RequestCode)
            {
                case RequestCode.C2SPkSession when packet.ActionCode == ActionCode.Match:
                    operation = "RegisterMatch";
                    return PKMatchmakingRegisterRequest.Parser.ParseFrom(packet.Body);

                case RequestCode.C2SPkSession when packet.ActionCode == ActionCode.Four:
                    operation = "CancelMatch";
                    return PKMatchmakingCancelRequest.Parser.ParseFrom(packet.Body);

                case RequestCode.C2SPkSession when packet.ActionCode == ActionCode.One:
                    operation = "StartDirect";
                    return PKStartClientRequest.Parser.ParseFrom(packet.Body);

                case RequestCode.C2SPkSession when packet.ActionCode == ActionCode.Two:
                    operation = "End";
                    return PKEndClientRequest.Parser.ParseFrom(packet.Body);

                case RequestCode.S2CPkStart:
                    operation = "StartResponse";
                    return PKStartClientResponse.Parser.ParseFrom(packet.Body);

                case RequestCode.S2CPkEnd:
                    operation = "EndResponse";
                    return SubmitGiftResponse.Parser.ParseFrom(packet.Body);

                case RequestCode.S2CPkSync:
                    operation = "Sync";
                    return SyncCommand.Parser.ParseFrom(packet.Body);

                default:
                    throw new InvalidOperationException(
                        $"Unsupported PK route: {packet.RequestCode}/{packet.ActionCode}");
            }
        }

        private void OnDestroy()
        {
            Client?.Dispose();
            Client = null;
        }

        private string ResolveTestIdentityPath()
        {
            if (Path.IsPathRooted(testIdentityMarkdownPath))
                return Path.GetFullPath(testIdentityMarkdownPath);

#if UNITY_EDITOR
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, testIdentityMarkdownPath));
#else
            return Path.Combine(
                Application.persistentDataPath,
                Path.GetFileName(testIdentityMarkdownPath));
#endif
        }
    }
}
