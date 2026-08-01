using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ClientProtocol;
using Google.Protobuf;
using PlayerProtocol;
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
        [SerializeField] private bool showLiveTestPanel = true;
        [SerializeField] private bool showEquipmentGmPanel = true;
        [SerializeField] private bool showRuntimeLogPanel = true;
        [Header("PK")]
        [SerializeField] private bool autoMatchAfterBind = false;
        [SerializeField] private int defaultPkDurationSeconds = 300;
        [Header("Logging")]
        [SerializeField] private bool logPkProtocolDetails = true;
        [SerializeField] private bool logPlayerProtocolDetails = true;
        [SerializeField] private bool logEquipmentProtocolDetails = true;

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
        public event Action<LivePlayerEnterNotify> PlayerEntered;
        public event Action<LivePlayerLeaveNotify> PlayerLeft;
        public event Action<LivePlayerCampSelectedNotify> PlayerCampSelected;
        public event Action<LivePlayerGiftNotify> PlayerGifted;
        public event Action<LiveClientTestResponse> LiveTestResponseReceived;
        public event Action<S2CEquipmentQueryResponse> EquipmentQueryResponseReceived;
        public event Action<S2CEquipmentCreateResponse> EquipmentCreateResponseReceived;
        public event Action<S2CEquipmentUpgradeResponse> EquipmentUpgradeResponseReceived;
        public event Action<S2CEquipmentEquipResponse> EquipmentEquipResponseReceived;
        public event Action<S2CEquipmentUnequipResponse> EquipmentUnequipResponseReceived;
        public event Action<S2CEquipmentChangedNotify> EquipmentChanged;

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
            Client.Player.PlayerEntered += OnPlayerEntered;
            Client.Player.PlayerLeft += OnPlayerLeft;
            Client.Player.PlayerCampSelected += OnPlayerCampSelected;
            Client.Player.PlayerGifted += OnPlayerGifted;
            Client.LiveTest.ResponseReceived += OnLiveTestResponse;
            Client.Equipment.QueryResponseReceived += OnEquipmentQueryResponse;
            Client.Equipment.CreateResponseReceived += OnEquipmentCreateResponse;
            Client.Equipment.UpgradeResponseReceived += OnEquipmentUpgradeResponse;
            Client.Equipment.EquipResponseReceived += OnEquipmentEquipResponse;
            Client.Equipment.UnequipResponseReceived += OnEquipmentUnequipResponse;
            Client.Equipment.Changed += OnEquipmentChanged;

            if (testMode && showRuntimeLogPanel &&
                GetComponent<RuntimeLogPanel>() == null)
            {
                gameObject.AddComponent<RuntimeLogPanel>();
            }

            if (testMode && showLiveTestPanel)
            {
                LiveTestPanel panel = GetComponent<LiveTestPanel>();
                if (panel == null)
                    panel = gameObject.AddComponent<LiveTestPanel>();
                panel.Initialize(this);
            }

            if (testMode && showEquipmentGmPanel)
            {
                EquipmentGmPanel panel = GetComponent<EquipmentGmPanel>();
                if (panel == null)
                    panel = gameObject.AddComponent<EquipmentGmPanel>();
                panel.Initialize(this);
            }
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

        public async Task<AnchorSessionStartResult> ReconnectTestSessionAsync()
        {
            if (!testMode)
                throw new InvalidOperationException("ReconnectTestSessionAsync requires TestMode.");

            Debug.Log("[Net][Flow] Reconnecting and rebinding the configured anchor session.");
            LastStartResult = null;
            await StopClientAsync();
            return await StartTestSessionAsync();
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

        public Task<long> TestPlayerEnterAsync(
            string openId,
            string nickname,
            string avatar = "")
        {
            return SendLiveTestAsync(new LiveClientTestRequest
            {
                Action = LiveClientTestAction.Enter,
                OpenId = openId ?? string.Empty,
                Nickname = nickname ?? string.Empty,
                Avatar = avatar ?? string.Empty
            });
        }

        public Task<long> TestPlayerSelectCampAsync(
            string openId,
            string nickname,
            int camp)
        {
            if (camp is not (1 or 2))
                throw new ArgumentOutOfRangeException(nameof(camp), "Camp must be 1 or 2.");

            return SendLiveTestAsync(new LiveClientTestRequest
            {
                Action = LiveClientTestAction.SelectCamp,
                OpenId = openId ?? string.Empty,
                Nickname = nickname ?? string.Empty,
                Camp = camp == 1 ? LivePlayerCamp.Red : LivePlayerCamp.Blue
            });
        }

        public Task<long> TestPlayerGiftAsync(
            string openId,
            string nickname,
            string giftId,
            int giftCount = 1,
            int giftValue = 10)
        {
            return SendLiveTestAsync(new LiveClientTestRequest
            {
                Action = LiveClientTestAction.Gift,
                OpenId = openId ?? string.Empty,
                Nickname = nickname ?? string.Empty,
                GiftId = giftId ?? string.Empty,
                GiftCount = giftCount,
                GiftValue = giftValue
            });
        }

        public Task<long> GmQueryEquipmentAsync(ulong playerId)
        {
            EnsureEquipmentGmReady();
            Debug.Log($"[Net][C->S][Equipment] Query PlayerId={playerId}");
            return Client.Equipment.QueryAsync(playerId);
        }

        public Task<long> GmCreateEquipmentAsync(ulong playerId, uint equipmentId)
        {
            EnsureEquipmentGmReady();
            string operationId = CreateOperationId();
            Debug.Log(
                $"[Net][C->S][Equipment] Create PlayerId={playerId}, " +
                $"EquipmentId={equipmentId}, OperationId={operationId}");
            return Client.Equipment.CreateAsync(operationId, playerId, equipmentId);
        }

        public Task<long> GmUpgradeEquipmentAsync(ulong playerId, ulong equipmentUid)
        {
            EnsureEquipmentGmReady();
            string operationId = CreateOperationId();
            Debug.Log(
                $"[Net][C->S][Equipment] Upgrade PlayerId={playerId}, " +
                $"EquipmentUid={equipmentUid}, OperationId={operationId}");
            return Client.Equipment.UpgradeAsync(
                operationId,
                playerId,
                equipmentUid);
        }

        public Task<long> GmEquipEquipmentAsync(
            ulong playerId,
            ulong equipmentUid,
            uint targetSlot)
        {
            EnsureEquipmentGmReady();
            string operationId = CreateOperationId();
            Debug.Log(
                $"[Net][C->S][Equipment] Equip PlayerId={playerId}, " +
                $"EquipmentUid={equipmentUid}, TargetSlot={targetSlot}, " +
                $"OperationId={operationId}");
            return Client.Equipment.EquipAsync(
                operationId,
                playerId,
                equipmentUid,
                targetSlot);
        }

        public Task<long> GmUnequipEquipmentAsync(ulong playerId, ulong equipmentUid)
        {
            EnsureEquipmentGmReady();
            string operationId = CreateOperationId();
            Debug.Log(
                $"[Net][C->S][Equipment] Unequip PlayerId={playerId}, " +
                $"EquipmentUid={equipmentUid}, OperationId={operationId}");
            return Client.Equipment.UnequipAsync(
                operationId,
                playerId,
                equipmentUid);
        }

        private void OnPlayerEntered(LivePlayerEnterNotify notify)
        {
            if (logPlayerProtocolDetails)
            {
                Debug.Log(
                    $"[Net][S->C][Player] Enter RoomId={notify.RoomId}, " +
                    $"PlayerId={notify.Player?.PlayerId}, " +
                    $"Nickname={notify.Player?.Nickname}, FirstEnter={notify.FirstEnter}, " +
                    $"Payload={notify}");
            }
            PlayerEntered?.Invoke(notify);
        }

        private void OnPlayerLeft(LivePlayerLeaveNotify notify)
        {
            if (logPlayerProtocolDetails)
            {
                Debug.Log(
                    $"[Net][S->C][Player] Leave RoomId={notify.RoomId}, " +
                    $"PlayerId={notify.PlayerId}, Reason={notify.Reason}, Payload={notify}");
            }
            PlayerLeft?.Invoke(notify);
        }

        private void OnPlayerCampSelected(LivePlayerCampSelectedNotify notify)
        {
            if (logPlayerProtocolDetails)
            {
                Debug.Log(
                    $"[Net][S->C][Player] CampSelected RoomId={notify.RoomId}, " +
                    $"PlayerId={notify.PlayerId}, Camp={notify.Camp}({(int)notify.Camp}), " +
                    $"Previous={notify.PreviousCamp}({(int)notify.PreviousCamp}), " +
                    $"Changed={notify.Changed}, Payload={notify}");
            }
            PlayerCampSelected?.Invoke(notify);
        }

        private void OnPlayerGifted(LivePlayerGiftNotify notify)
        {
            if (logPlayerProtocolDetails)
            {
                var troopSpawn = notify.TroopSpawn;
                var troopText = troopSpawn == null
                    ? "TroopSpawn=None"
                    : $"TroopId={troopSpawn.TroopId}, TroopLevel={troopSpawn.TroopLevel}, " +
                      $"TroopCount={troopSpawn.TroopCount}";
                Debug.Log(
                    $"[Net][S->C][Player] Gift RoomId={notify.RoomId}, " +
                    $"PlayerId={notify.PlayerId}, GiftId={notify.GiftId}, " +
                    $"Count={notify.GiftCount}, Value={notify.GiftValue}, " +
                    $"EventId={notify.EventId}, {troopText}, Payload={notify}");
            }
            PlayerGifted?.Invoke(notify);
        }

        private void OnLiveTestResponse(LiveClientTestResponse response)
        {
            Debug.Log(
                $"[Net][S->C][LiveTest] Action={response.Action}, " +
                $"Accepted={response.Accepted}, Reason={response.Reason}, " +
                $"EventId={response.EventId}, Payload={response}");
            LiveTestResponseReceived?.Invoke(response);
        }

        private void OnEquipmentQueryResponse(S2CEquipmentQueryResponse response)
        {
            LogEquipmentDetail(
                $"Query Accepted={response.Accepted}, Reason={response.Reason}, " +
                $"PlayerId={response.PlayerId}, Version={response.ModuleVersion}, " +
                $"Payload={response}");
            EquipmentQueryResponseReceived?.Invoke(response);
        }

        private void OnEquipmentCreateResponse(S2CEquipmentCreateResponse response)
        {
            LogEquipmentDetail(
                $"Create Accepted={response.Accepted}, Reason={response.Reason}, " +
                $"PlayerId={response.PlayerId}, Version={response.ModuleVersion}, " +
                $"OperationId={response.OperationId}, Payload={response}");
            EquipmentCreateResponseReceived?.Invoke(response);
        }

        private void OnEquipmentUpgradeResponse(S2CEquipmentUpgradeResponse response)
        {
            LogEquipmentDetail(
                $"Upgrade Accepted={response.Accepted}, Reason={response.Reason}, " +
                $"PlayerId={response.PlayerId}, Version={response.ModuleVersion}, " +
                $"OperationId={response.OperationId}, Payload={response}");
            EquipmentUpgradeResponseReceived?.Invoke(response);
        }

        private void OnEquipmentEquipResponse(S2CEquipmentEquipResponse response)
        {
            LogEquipmentDetail(
                $"Equip Accepted={response.Accepted}, Reason={response.Reason}, " +
                $"PlayerId={response.PlayerId}, Version={response.ModuleVersion}, " +
                $"OperationId={response.OperationId}, Payload={response}");
            EquipmentEquipResponseReceived?.Invoke(response);
        }

        private void OnEquipmentUnequipResponse(S2CEquipmentUnequipResponse response)
        {
            LogEquipmentDetail(
                $"Unequip Accepted={response.Accepted}, Reason={response.Reason}, " +
                $"PlayerId={response.PlayerId}, Version={response.ModuleVersion}, " +
                $"OperationId={response.OperationId}, Payload={response}");
            EquipmentUnequipResponseReceived?.Invoke(response);
        }

        private void OnEquipmentChanged(S2CEquipmentChangedNotify notify)
        {
            LogEquipmentDetail(
                $"Changed Type={notify.ChangeType}, PlayerId={notify.PlayerId}, " +
                $"Version={notify.ModuleVersion}, OperationId={notify.OperationId}, " +
                $"Reason={notify.Reason}, Payload={notify}");
            EquipmentChanged?.Invoke(notify);
        }

        private Task<long> SendLiveTestAsync(LiveClientTestRequest request)
        {
            if (!testMode)
                throw new InvalidOperationException("Live test actions require TestMode.");
            if (LastStartResult?.Success != true)
                throw new InvalidOperationException("Bind the anchor session before using the live test panel.");
            if (string.IsNullOrWhiteSpace(request.OpenId))
                throw new ArgumentException("OpenId is required.", nameof(request));

            Debug.Log(
                $"[Net][C->S][LiveTest] Action={request.Action}, OpenId={request.OpenId}, " +
                $"Camp={request.Camp}, GiftId={request.GiftId}, Count={request.GiftCount}");
            return Client.LiveTest.SendAsync(request);
        }

        private void EnsurePkReady()
        {
            if (Client == null)
                throw new InvalidOperationException("Network client is not initialized.");
            if (LastStartResult?.Success != true)
                throw new InvalidOperationException("Anchor session must be bound before using PK operations.");
        }

        private void EnsureEquipmentGmReady()
        {
            if (!testMode)
                throw new InvalidOperationException("Equipment GM operations require TestMode.");
            if (Client == null)
                throw new InvalidOperationException("Network client is not initialized.");
            if (LastStartResult?.Success != true)
            {
                throw new InvalidOperationException(
                    "Bind the anchor session before using equipment GM operations.");
            }
        }

        private void LogEquipmentDetail(string message)
        {
            if (logEquipmentProtocolDetails)
                Debug.Log($"[Net][S->C][Equipment] {message}");
        }

        private static string CreateOperationId()
        {
            return Guid.NewGuid().ToString("N");
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
#elif UNITY_STANDALONE_WIN
            return Path.Combine(
                Application.streamingAssetsPath,
                Path.GetFileName(testIdentityMarkdownPath));
#else
            return Path.Combine(
                Application.persistentDataPath,
                Path.GetFileName(testIdentityMarkdownPath));
#endif
        }
    }

    public sealed class RuntimeLogPanel : MonoBehaviour
    {
        private const int MaxEntries = 300;
        private const float ScreenMargin = 20f;
        private const float PreferredHeightRatio = 0.42f;
        private const float MinimumHeight = 280f;

        private readonly object _syncRoot = new object();
        private readonly List<LogEntry> _entries = new List<LogEntry>();
        private LogEntry[] _snapshot = Array.Empty<LogEntry>();
        private int _logVersion;
        private int _snapshotVersion = -1;
        private int _displayedVersion = -1;
        private bool _autoScroll = true;
        private Vector2 _scrollPosition;
        private Rect _windowRect;

        private GUIStyle _windowStyle;
        private GUIStyle _toolbarStyle;
        private GUIStyle _toolbarButtonStyle;
        private GUIStyle _logStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _errorStyle;
        private GUIStyle _stackTraceStyle;

        private void Awake()
        {
            Application.logMessageReceivedThreaded += OnLogMessageReceived;
        }

        private void OnDestroy()
        {
            Application.logMessageReceivedThreaded -= OnLogMessageReceived;
        }

        private void OnGUI()
        {
            EnsureStyles();
            RefreshSnapshot();

            float availableWidth = Mathf.Max(320f, Screen.width - ScreenMargin * 2f);
            float availableHeight = Mathf.Max(240f, Screen.height - ScreenMargin * 2f);
            float preferredHeight = Mathf.Max(
                MinimumHeight,
                Screen.height * PreferredHeightRatio);
            float windowHeight = Mathf.Min(preferredHeight, availableHeight);

            _windowRect = new Rect(
                ScreenMargin,
                ScreenMargin,
                availableWidth,
                windowHeight);
            _windowRect = GUI.Window(
                GetInstanceID(),
                _windowRect,
                DrawWindow,
                "运行日志",
                _windowStyle);
        }

        private void DrawWindow(int id)
        {
            int warningCount = 0;
            int errorCount = 0;
            for (int i = 0; i < _snapshot.Length; i++)
            {
                if (_snapshot[i].Type == LogType.Warning)
                    warningCount++;
                else if (IsError(_snapshot[i].Type))
                    errorCount++;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(
                    "清空",
                    _toolbarButtonStyle,
                    GUILayout.Width(90f),
                    GUILayout.Height(36f)))
            {
                ClearLogs();
            }

            _autoScroll = GUILayout.Toggle(
                _autoScroll,
                "自动滚动",
                _toolbarButtonStyle,
                GUILayout.Width(140f),
                GUILayout.Height(36f));
            GUILayout.Space(12f);
            GUILayout.Label(
                $"总数 {_snapshot.Length}    警告 {warningCount}    错误 {errorCount}",
                _toolbarStyle,
                GUILayout.Height(36f));
            GUILayout.EndHorizontal();

            if (_autoScroll && _displayedVersion != _snapshotVersion)
                _scrollPosition.y = float.MaxValue;

            _scrollPosition = GUILayout.BeginScrollView(
                _scrollPosition,
                false,
                true,
                GUILayout.ExpandHeight(true));
            for (int i = 0; i < _snapshot.Length; i++)
            {
                LogEntry entry = _snapshot[i];
                GUILayout.Label(
                    $"[{entry.Time:HH:mm:ss}] [{entry.Type}] {entry.Message}",
                    ResolveLogStyle(entry.Type));

                if (IsError(entry.Type) &&
                    !string.IsNullOrWhiteSpace(entry.StackTrace))
                {
                    GUILayout.Label(entry.StackTrace, _stackTraceStyle);
                }
            }
            GUILayout.EndScrollView();
            _displayedVersion = _snapshotVersion;
        }

        private void OnLogMessageReceived(
            string condition,
            string stackTrace,
            LogType type)
        {
            var entry = new LogEntry(
                DateTime.Now,
                condition ?? string.Empty,
                stackTrace ?? string.Empty,
                type);

            lock (_syncRoot)
            {
                if (_entries.Count >= MaxEntries)
                    _entries.RemoveRange(0, Math.Min(50, _entries.Count));
                _entries.Add(entry);
                _logVersion++;
            }
        }

        private void RefreshSnapshot()
        {
            lock (_syncRoot)
            {
                if (_snapshotVersion == _logVersion)
                    return;

                _snapshot = _entries.ToArray();
                _snapshotVersion = _logVersion;
            }
        }

        private void ClearLogs()
        {
            lock (_syncRoot)
            {
                _entries.Clear();
                _logVersion++;
            }

            _snapshot = Array.Empty<LogEntry>();
            _snapshotVersion = -1;
            _displayedVersion = -1;
            _scrollPosition = Vector2.zero;
        }

        private GUIStyle ResolveLogStyle(LogType type)
        {
            if (type == LogType.Warning)
                return _warningStyle;
            return IsError(type) ? _errorStyle : _logStyle;
        }

        private static bool IsError(LogType type)
        {
            return type == LogType.Error ||
                   type == LogType.Assert ||
                   type == LogType.Exception;
        }

        private void EnsureStyles()
        {
            if (_windowStyle != null)
                return;

            int fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height / 42f), 16, 22);
            _windowStyle = new GUIStyle(GUI.skin.window)
            {
                fontSize = fontSize + 2,
                padding = new RectOffset(14, 14, 28, 12)
            };
            _toolbarStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                alignment = TextAnchor.MiddleLeft
            };
            _toolbarButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = fontSize,
                alignment = TextAnchor.MiddleCenter
            };
            _logStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                wordWrap = true,
                richText = false
            };
            _logStyle.normal.textColor = Color.white;
            _warningStyle = new GUIStyle(_logStyle);
            _warningStyle.normal.textColor = new Color(1f, 0.78f, 0.25f);
            _errorStyle = new GUIStyle(_logStyle);
            _errorStyle.normal.textColor = new Color(1f, 0.35f, 0.35f);
            _stackTraceStyle = new GUIStyle(_logStyle)
            {
                fontSize = Mathf.Max(14, fontSize - 2)
            };
            _stackTraceStyle.normal.textColor = new Color(1f, 0.58f, 0.58f);
        }

        private readonly struct LogEntry
        {
            public LogEntry(
                DateTime time,
                string message,
                string stackTrace,
                LogType type)
            {
                Time = time;
                Message = message;
                StackTrace = stackTrace;
                Type = type;
            }

            public DateTime Time { get; }
            public string Message { get; }
            public string StackTrace { get; }
            public LogType Type { get; }
        }
    }

    public sealed class LiveTestPanel : MonoBehaviour
    {
        private const float ScreenMargin = 20f;
        private const float PreferredWindowWidth = 560f;
        private const float CollapsedWindowHeight = 590f;
        private const float ExpandedWindowHeight = 750f;

        private static readonly string[] GiftIds =
        {
            "13585", "11584", "12252", "5357", "11585",
            "11586", "5361", "5363", "5364", "12721"
        };

        private NetworkRuntime _runtime;
        private Rect _windowRect;
        private string _openId = "test-user-001";
        private string _nickname = "测试玩家";
        private string _status = "等待玩家测试操作";
        private bool _reconnecting;
        private int _giftIndex;
        private bool _giftMenuOpen;
        private Vector2 _scrollPosition;
        private GUIStyle _windowStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _textFieldStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _statusStyle;

        public void Initialize(NetworkRuntime runtime)
        {
            if (_runtime == runtime)
                return;
            if (_runtime != null)
                _runtime.LiveTestResponseReceived -= OnResponse;
            _runtime = runtime;
            if (_runtime != null)
                _runtime.LiveTestResponseReceived += OnResponse;
        }

        private void OnGUI()
        {
            if (_runtime == null)
                return;

            EnsureStyles();
            float availableWidth = Mathf.Max(320f, Screen.width - ScreenMargin * 2f);
            float availableHeight = Mathf.Max(320f, Screen.height - ScreenMargin * 2f);
            float windowWidth = Mathf.Min(PreferredWindowWidth, availableWidth);
            float preferredHeight = _giftMenuOpen
                ? ExpandedWindowHeight
                : CollapsedWindowHeight;
            float windowHeight = Mathf.Min(preferredHeight, availableHeight);

            _windowRect = new Rect(
                ScreenMargin,
                Mathf.Max(ScreenMargin, Screen.height - windowHeight - ScreenMargin),
                windowWidth,
                windowHeight);
            _windowRect = GUI.Window(
                GetInstanceID(),
                _windowRect,
                DrawWindow,
                "直播玩家协议测试",
                _windowStyle);
        }

        private void DrawWindow(int id)
        {
            _scrollPosition = GUILayout.BeginScrollView(
                _scrollPosition,
                false,
                true);
            GUILayout.Space(8f);
            GUILayout.Label("玩家 OpenId", _labelStyle);
            _openId = GUILayout.TextField(
                _openId,
                _textFieldStyle,
                GUILayout.Height(40f));
            GUILayout.Space(6f);
            GUILayout.Label("玩家昵称", _labelStyle);
            _nickname = GUILayout.TextField(
                _nickname,
                _textFieldStyle,
                GUILayout.Height(40f));
            GUILayout.Space(10f);

            if (GUILayout.Button(
                    "1. 进入直播间",
                    _buttonStyle,
                    GUILayout.Height(44f)))
                SendEnter();

            GUILayout.Space(10f);
            GUILayout.Label("2. 选择阵营", _labelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(
                    "红方（弹幕 1）",
                    _buttonStyle,
                    GUILayout.Height(44f)))
                SendCamp(1);
            if (GUILayout.Button(
                    "蓝方（弹幕 2）",
                    _buttonStyle,
                    GUILayout.Height(44f)))
                SendCamp(2);
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);
            GUILayout.Label("3. 选择礼物", _labelStyle);
            if (GUILayout.Button(
                    $"礼物 ID：{GiftIds[_giftIndex]} ▼",
                    _buttonStyle,
                    GUILayout.Height(42f)))
                _giftMenuOpen = !_giftMenuOpen;

            if (_giftMenuOpen)
            {
                GUILayout.Space(6f);
                int selected = GUILayout.SelectionGrid(
                    _giftIndex,
                    GiftIds,
                    3,
                    _buttonStyle,
                    GUILayout.Height(130f));
                if (selected != _giftIndex)
                {
                    _giftIndex = selected;
                    _giftMenuOpen = false;
                }
            }

            GUILayout.Space(10f);
            if (GUILayout.Button(
                    "4. 点击送礼",
                    _buttonStyle,
                    GUILayout.Height(46f)))
                SendGift();

            GUILayout.Space(12f);
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && !_reconnecting;
            if (GUILayout.Button(
                    _reconnecting ? "主播重新连接中..." : "主播重新连接",
                    _buttonStyle,
                    GUILayout.Height(46f)))
                ReconnectAnchor();
            GUI.enabled = previousEnabled;

            GUILayout.Space(12f);
            GUILayout.Label($"状态：{_status}", _statusStyle);
            GUILayout.EndScrollView();
        }

        private void EnsureStyles()
        {
            if (_windowStyle != null)
                return;

            int fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height / 32f), 18, 24);
            _windowStyle = new GUIStyle(GUI.skin.window)
            {
                fontSize = fontSize + 2,
                padding = new RectOffset(18, 18, 28, 16)
            };
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                alignment = TextAnchor.MiddleLeft
            };
            _textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = fontSize,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 10, 6, 6)
            };
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = fontSize,
                alignment = TextAnchor.MiddleCenter
            };
            _statusStyle = new GUIStyle(_labelStyle)
            {
                wordWrap = true
            };
        }

        private async void SendEnter()
        {
            try
            {
                long msgId = await _runtime.TestPlayerEnterAsync(_openId, ResolveNickname());
                _status = $"进入请求已发送，MsgId={msgId}";
            }
            catch (Exception ex)
            {
                _status = ex.Message;
                Debug.LogException(ex);
            }
        }

        private async void SendCamp(int camp)
        {
            try
            {
                long msgId = await _runtime.TestPlayerSelectCampAsync(
                    _openId,
                    ResolveNickname(),
                    camp);
                _status = $"阵营请求已发送，Camp={camp}，MsgId={msgId}";
            }
            catch (Exception ex)
            {
                _status = ex.Message;
                Debug.LogException(ex);
            }
        }

        private async void SendGift()
        {
            try
            {
                long msgId = await _runtime.TestPlayerGiftAsync(
                    _openId,
                    ResolveNickname(),
                    GiftIds[_giftIndex]);
                _status = $"送礼请求已发送，Gift={GiftIds[_giftIndex]}，MsgId={msgId}";
            }
            catch (Exception ex)
            {
                _status = ex.Message;
                Debug.LogException(ex);
            }
        }

        private async void ReconnectAnchor()
        {
            if (_reconnecting)
                return;

            try
            {
                _reconnecting = true;
                _status = "正在重新连接并 Bind 主播...";
                AnchorSessionStartResult result =
                    await _runtime.ReconnectTestSessionAsync();
                _status = result.Success
                    ? "主播重新连接并 Bind 成功"
                    : $"主播重新 Bind 失败：{result.Reason}";
            }
            catch (Exception ex)
            {
                _status = $"主播重新连接失败：{ex.Message}";
                Debug.LogException(ex);
            }
            finally
            {
                _reconnecting = false;
            }
        }

        private void OnResponse(LiveClientTestResponse response)
        {
            _status = response.Accepted
                ? $"{response.Action} 成功，EventId={response.EventId}"
                : $"{response.Action} 失败：{response.Reason}";
        }

        private string ResolveNickname()
        {
            return string.IsNullOrWhiteSpace(_nickname) ? _openId : _nickname;
        }

        private void OnDestroy()
        {
            if (_runtime != null)
                _runtime.LiveTestResponseReceived -= OnResponse;
        }
    }

    public sealed class EquipmentGmPanel : MonoBehaviour
    {
        private const float ScreenMargin = 20f;
        private const float PreferredWindowWidth = 560f;
        private const float PreferredWindowHeight = 590f;

        private NetworkRuntime _runtime;
        private Rect _windowRect;
        private Vector2 _scrollPosition;
        private string _playerId = "1";
        private string _equipmentId = "1001";
        private string _equipmentUid = string.Empty;
        private string _targetSlot = "1";
        private string _status = "先填写指定玩家的 PlayerId";
        private bool _sending;

        private GUIStyle _windowStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _textFieldStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _statusStyle;

        public void Initialize(NetworkRuntime runtime)
        {
            if (_runtime == runtime)
                return;

            Unsubscribe();
            _runtime = runtime;
            if (_runtime == null)
                return;

            _runtime.EquipmentQueryResponseReceived += OnQueryResponse;
            _runtime.EquipmentCreateResponseReceived += OnCreateResponse;
            _runtime.EquipmentUpgradeResponseReceived += OnUpgradeResponse;
            _runtime.EquipmentEquipResponseReceived += OnEquipResponse;
            _runtime.EquipmentUnequipResponseReceived += OnUnequipResponse;
            _runtime.EquipmentChanged += OnChanged;
        }

        private void OnGUI()
        {
            if (_runtime == null)
                return;

            EnsureStyles();
            float availableWidth = Mathf.Max(320f, Screen.width - ScreenMargin * 2f);
            float availableHeight = Mathf.Max(320f, Screen.height - ScreenMargin * 2f);
            float windowWidth = Mathf.Min(PreferredWindowWidth, availableWidth);
            float windowHeight = Mathf.Min(PreferredWindowHeight, availableHeight);

            _windowRect = new Rect(
                Mathf.Max(ScreenMargin, Screen.width - windowWidth - ScreenMargin),
                Mathf.Max(ScreenMargin, Screen.height - windowHeight - ScreenMargin),
                windowWidth,
                windowHeight);
            _windowRect = GUI.Window(
                GetInstanceID(),
                _windowRect,
                DrawWindow,
                "指定玩家装备 GM",
                _windowStyle);
        }

        private void DrawWindow(int id)
        {
            _scrollPosition = GUILayout.BeginScrollView(
                _scrollPosition,
                false,
                true);

            GUILayout.Space(8f);
            GUILayout.Label("玩家 PlayerId（必填，不是 OpenId）", _labelStyle);
            _playerId = GUILayout.TextField(
                _playerId,
                _textFieldStyle,
                GUILayout.Height(40f));
            GUILayout.Space(8f);

            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && !_sending;
            if (GUILayout.Button(
                    "查询该玩家装备",
                    _buttonStyle,
                    GUILayout.Height(44f)))
            {
                RunRequest(
                    () => _runtime.GmQueryEquipmentAsync(ParsePlayerId()),
                    "查询");
            }

            GUILayout.Space(14f);
            GUILayout.Label("装备配置 EquipmentId", _labelStyle);
            _equipmentId = GUILayout.TextField(
                _equipmentId,
                _textFieldStyle,
                GUILayout.Height(40f));
            if (GUILayout.Button(
                    "创建装备",
                    _buttonStyle,
                    GUILayout.Height(44f)))
            {
                RunRequest(
                    () => _runtime.GmCreateEquipmentAsync(
                        ParsePlayerId(),
                        ParsePositiveUInt(_equipmentId, "EquipmentId")),
                    "创建");
            }

            GUILayout.Space(14f);
            GUILayout.Label("装备实例 EquipmentUid", _labelStyle);
            _equipmentUid = GUILayout.TextField(
                _equipmentUid,
                _textFieldStyle,
                GUILayout.Height(40f));
            if (GUILayout.Button(
                    "升级装备",
                    _buttonStyle,
                    GUILayout.Height(44f)))
            {
                RunRequest(
                    () => _runtime.GmUpgradeEquipmentAsync(
                        ParsePlayerId(),
                        ParseEquipmentUid()),
                    "升级");
            }

            GUILayout.Space(10f);
            GUILayout.Label("目标槽位 TargetSlot", _labelStyle);
            _targetSlot = GUILayout.TextField(
                _targetSlot,
                _textFieldStyle,
                GUILayout.Height(40f));

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(
                    "穿戴装备",
                    _buttonStyle,
                    GUILayout.Height(44f)))
            {
                RunRequest(
                    () => _runtime.GmEquipEquipmentAsync(
                        ParsePlayerId(),
                        ParseEquipmentUid(),
                        ParsePositiveUInt(_targetSlot, "TargetSlot")),
                    "穿戴");
            }
            if (GUILayout.Button(
                    "脱下装备",
                    _buttonStyle,
                    GUILayout.Height(44f)))
            {
                RunRequest(
                    () => _runtime.GmUnequipEquipmentAsync(
                        ParsePlayerId(),
                        ParseEquipmentUid()),
                    "脱下");
            }
            GUILayout.EndHorizontal();
            GUI.enabled = previousEnabled;

            GUILayout.Space(14f);
            GUILayout.Label($"状态：{_status}", _statusStyle);
            GUILayout.EndScrollView();
        }

        private async void RunRequest(Func<Task<long>> send, string operation)
        {
            if (_sending)
                return;

            try
            {
                _sending = true;
                long msgId = await send();
                _status = $"{operation}请求已发送，MsgId={msgId}，等待服务器回包";
            }
            catch (Exception ex)
            {
                _status = $"{operation}请求失败：{ex.Message}";
                Debug.LogException(ex);
            }
            finally
            {
                _sending = false;
            }
        }

        private void OnQueryResponse(S2CEquipmentQueryResponse response)
        {
            if (response.PlayerId != 0)
                _playerId = response.PlayerId.ToString();

            int count = response.Module?.Equipments.Count ?? 0;
            if (response.Accepted && count > 0)
                _equipmentUid = response.Module.Equipments[0].EquipmentUid.ToString();

            _status = response.Accepted
                ? $"查询成功：{count} 件装备，版本={response.ModuleVersion}"
                : $"查询失败：{response.Reason}";
        }

        private void OnCreateResponse(S2CEquipmentCreateResponse response)
        {
            UpdateIdentity(response.PlayerId, response.Equipment);
            _status = FormatMutationResult(
                "创建",
                response.Accepted,
                response.Reason,
                response.ModuleVersion,
                response.Equipment);
        }

        private void OnUpgradeResponse(S2CEquipmentUpgradeResponse response)
        {
            UpdateIdentity(response.PlayerId, response.Equipment);
            _status = FormatMutationResult(
                "升级",
                response.Accepted,
                response.Reason,
                response.ModuleVersion,
                response.Equipment);
        }

        private void OnEquipResponse(S2CEquipmentEquipResponse response)
        {
            UpdateIdentity(response.PlayerId, response.Equipment);
            _status = FormatMutationResult(
                "穿戴",
                response.Accepted,
                response.Reason,
                response.ModuleVersion,
                response.Equipment);
        }

        private void OnUnequipResponse(S2CEquipmentUnequipResponse response)
        {
            UpdateIdentity(response.PlayerId, response.Equipment);
            _status = FormatMutationResult(
                "脱下",
                response.Accepted,
                response.Reason,
                response.ModuleVersion,
                response.Equipment);
        }

        private void OnChanged(S2CEquipmentChangedNotify notify)
        {
            if (!ulong.TryParse(_playerId, out ulong selectedPlayerId) ||
                selectedPlayerId != notify.PlayerId)
            {
                return;
            }

            UpdateIdentity(notify.PlayerId, notify.Equipment);
            _status =
                $"服务器推送：{notify.ChangeType}，版本={notify.ModuleVersion}，" +
                FormatEquipment(notify.Equipment);
        }

        private void UpdateIdentity(ulong playerId, EquipmentData equipment)
        {
            if (playerId != 0)
                _playerId = playerId.ToString();
            if (equipment?.EquipmentUid > 0)
                _equipmentUid = equipment.EquipmentUid.ToString();
        }

        private static string FormatMutationResult(
            string operation,
            bool accepted,
            string reason,
            ulong moduleVersion,
            EquipmentData equipment)
        {
            return accepted
                ? $"{operation}成功：版本={moduleVersion}，{FormatEquipment(equipment)}"
                : $"{operation}失败：{reason}";
        }

        private static string FormatEquipment(EquipmentData equipment)
        {
            if (equipment == null)
                return "无装备数据";

            return
                $"Uid={equipment.EquipmentUid}，Id={equipment.EquipmentId}，" +
                $"等级={equipment.Level}，星级={equipment.Star}，" +
                $"品质={equipment.Quality}，槽位={equipment.EquippedSlot}";
        }

        private ulong ParsePlayerId()
        {
            return ParsePositiveULong(_playerId, "PlayerId");
        }

        private ulong ParseEquipmentUid()
        {
            return ParsePositiveULong(_equipmentUid, "EquipmentUid");
        }

        private static ulong ParsePositiveULong(string text, string fieldName)
        {
            if (!ulong.TryParse(text, out ulong value) || value == 0)
                throw new FormatException($"{fieldName} 必须是大于 0 的整数。");
            return value;
        }

        private static uint ParsePositiveUInt(string text, string fieldName)
        {
            if (!uint.TryParse(text, out uint value) || value == 0)
                throw new FormatException($"{fieldName} 必须是大于 0 的整数。");
            return value;
        }

        private void EnsureStyles()
        {
            if (_windowStyle != null)
                return;

            int fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height / 34f), 17, 23);
            _windowStyle = new GUIStyle(GUI.skin.window)
            {
                fontSize = fontSize + 2,
                padding = new RectOffset(18, 18, 28, 16)
            };
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                alignment = TextAnchor.MiddleLeft
            };
            _textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = fontSize,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 10, 6, 6)
            };
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = fontSize,
                alignment = TextAnchor.MiddleCenter
            };
            _statusStyle = new GUIStyle(_labelStyle)
            {
                wordWrap = true
            };
        }

        private void Unsubscribe()
        {
            if (_runtime == null)
                return;

            _runtime.EquipmentQueryResponseReceived -= OnQueryResponse;
            _runtime.EquipmentCreateResponseReceived -= OnCreateResponse;
            _runtime.EquipmentUpgradeResponseReceived -= OnUpgradeResponse;
            _runtime.EquipmentEquipResponseReceived -= OnEquipResponse;
            _runtime.EquipmentUnequipResponseReceived -= OnUnequipResponse;
            _runtime.EquipmentChanged -= OnChanged;
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }
    }
}
