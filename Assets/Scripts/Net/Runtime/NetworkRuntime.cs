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
        [SerializeField] private bool showLiveTestPanel = true;
        [Header("PK")]
        [SerializeField] private bool autoMatchAfterBind = false;
        [SerializeField] private int defaultPkDurationSeconds = 300;
        [Header("Logging")]
        [SerializeField] private bool logPkProtocolDetails = true;
        [SerializeField] private bool logPlayerProtocolDetails = true;

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
        public event Action<LiveClientTestResponse> LiveTestResponseReceived;

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
            Client.LiveTest.ResponseReceived += OnLiveTestResponse;

            if (testMode && showLiveTestPanel)
            {
                LiveTestPanel panel = GetComponent<LiveTestPanel>();
                if (panel == null)
                    panel = gameObject.AddComponent<LiveTestPanel>();
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

        private void OnPlayerEntered(LivePlayerEnterNotify notify)
        {
            if (logPlayerProtocolDetails)
            {
                Debug.Log(
                    $"[Net][S->C][Player] Enter RoomId={notify.RoomId}, " +
                    $"PlayerId={notify.Player?.PlayerId}, OpenId={notify.Player?.OpenId}, " +
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

        private void OnLiveTestResponse(LiveClientTestResponse response)
        {
            Debug.Log(
                $"[Net][S->C][LiveTest] Action={response.Action}, " +
                $"Accepted={response.Accepted}, Reason={response.Reason}, " +
                $"EventId={response.EventId}, Payload={response}");
            LiveTestResponseReceived?.Invoke(response);
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

    public sealed class LiveTestPanel : MonoBehaviour
    {
        private const float ScreenMargin = 20f;
        private const float PreferredWindowWidth = 560f;
        private const float CollapsedWindowHeight = 520f;
        private const float ExpandedWindowHeight = 680f;

        private static readonly string[] GiftIds =
        {
            "13585", "11584", "12252", "5357", "11585",
            "11586", "5361", "5363", "5364", "12721"
        };

        private NetworkRuntime _runtime;
        private Rect _windowRect;
        private string _openId = "test-user-001";
        private string _nickname = "测试玩家";
        private string _status = "请先连接并 Bind 主播房间";
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
}
