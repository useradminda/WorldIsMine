using System;
using System.Collections.Generic;
using ClientProtocol;
using UnityEngine;
using WorldIsMine.Net.Runtime;

namespace WorldIsMine.Game.PK
{
    /// <summary>
    /// Converts server PK synchronization messages into game-layer events.
    /// Add this component to the battle scene and assign NetworkRuntime.
    /// </summary>
    public sealed class PkBattleSyncController : MonoBehaviour
    {
        [Serializable]
        public sealed class GiftUnitRule
        {
            [Tooltip("Gift ID sent by the PK server, for example 13585.")]
            public string giftId = string.Empty;

            [Tooltip("Battle unit configuration ID.")]
            public int unitId = 1;

            [Min(1)]
            [Tooltip("Units created for each gift.")]
            public int unitsPerGift = 1;

            [Min(1)]
            [Tooltip("Safety limit for one gift synchronization message.")]
            public int maxUnitsPerEvent = 100;
        }

        [Header("Dependencies")]
        [SerializeField] private NetworkRuntime networkRuntime;
        [SerializeField] private BattleEngine battleEngine;

        [Header("Side Mapping")]
        [SerializeField] private ECampType sideACamp = ECampType.Red;
        [SerializeField] private ECampType sideBCamp = ECampType.Blue;

        [Header("Gift -> Battle Unit")]
        [SerializeField] private bool createUnitsFromGifts = true;
        [SerializeField] private GiftUnitRule[] giftUnitRules =
        {
            new GiftUnitRule
            {
                giftId = "13585",
                unitId = 1,
                unitsPerGift = 1,
                maxUnitsPerEvent = 100
            }
        };

        [Header("Logging")]
        [SerializeField] private bool logGamePkEvents = true;

        private readonly Dictionary<string, GiftUnitRule> _giftRuleById =
            new Dictionary<string, GiftUnitRule>(StringComparer.Ordinal);

        private bool _subscribed;

        public string SessionId { get; private set; } = string.Empty;
        public long LastSequence { get; private set; } = -1;
        public long ScoreA { get; private set; }
        public long ScoreB { get; private set; }
        public PKSessionStatus Status { get; private set; } = PKSessionStatus.Created;
        public PKSide WinnerSide { get; private set; }
        public bool BattleRunning => Status == PKSessionStatus.Running;

        public event Action<SessionSnapshot> BattleStarted;
        public event Action<SyncCommand> SyncReceived;
        public event Action<GiftSyncPayload> GiftReceived;
        public event Action<AttackSyncPayload> AttackReceived;
        public event Action<MonsterSpawnSyncPayload> MonsterSpawnReceived;
        public event Action<MonsterDamageSyncPayload> MonsterDamageReceived;
        public event Action<BuffApplySyncPayload> BuffApplyReceived;
        public event Action<BossAttackSyncPayload> BossAttackReceived;
        public event Action<WinSyncPayload> WinReceived;
        public event Action<EndSyncPayload> EndReceived;
        public event Action<SubmitGiftResponse> BattleEnded;

        private void Awake()
        {
            RebuildGiftRuleCache();

            if (networkRuntime == null)
                networkRuntime = FindObjectOfType<NetworkRuntime>();
            if (battleEngine == null)
                battleEngine = FindObjectOfType<BattleEngine>();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void Start()
        {
            // Covers a NetworkRuntime instantiated after this component's Awake.
            if (!_subscribed)
            {
                if (networkRuntime == null)
                    networkRuntime = FindObjectOfType<NetworkRuntime>();
                Subscribe();
            }

            SessionSnapshot currentSession = networkRuntime?.Client?.Pk?.CurrentSession;
            if (currentSession != null)
                OnBattleStarted(currentSession);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnValidate()
        {
            RebuildGiftRuleCache();
        }

        private void Subscribe()
        {
            if (_subscribed || networkRuntime == null)
                return;

            networkRuntime.PkBattleStarted += OnBattleStarted;
            networkRuntime.PkSyncReceived += OnSyncReceived;
            networkRuntime.PkBattleEnded += OnBattleEnded;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || networkRuntime == null)
                return;

            networkRuntime.PkBattleStarted -= OnBattleStarted;
            networkRuntime.PkSyncReceived -= OnSyncReceived;
            networkRuntime.PkBattleEnded -= OnBattleEnded;
            _subscribed = false;
        }

        private void OnBattleStarted(SessionSnapshot snapshot)
        {
            if (snapshot == null)
                return;

            SessionId = snapshot.SessionId ?? string.Empty;
            LastSequence = snapshot.Sequence;
            ScoreA = snapshot.ScoreA;
            ScoreB = snapshot.ScoreB;
            Status = snapshot.Status;
            WinnerSide = PKSide.Unknown;

            Log(
                $"BattleStarted SessionId={SessionId}, Sequence={LastSequence}, " +
                $"Score={ScoreA}:{ScoreB}, Status={Status}");
            BattleStarted?.Invoke(snapshot);
        }

        private void OnSyncReceived(SyncCommand command)
        {
            if (command == null || !Accept(command))
                return;

            ScoreA = command.ScoreA;
            ScoreB = command.ScoreB;
            Status = command.Status;

            Log(
                $"Sync SessionId={command.SessionId}, Sequence={command.Sequence}, " +
                $"Kind={command.CommandKind}, Side={command.SourceSide}, " +
                $"Score={ScoreA}:{ScoreB}, Reason={command.Reason}");

            SyncReceived?.Invoke(command);

            switch (command.PayloadCase)
            {
                case SyncCommand.PayloadOneofCase.Gift:
                    HandleGift(command.Gift);
                    break;
                case SyncCommand.PayloadOneofCase.Attack:
                    AttackReceived?.Invoke(command.Attack);
                    break;
                case SyncCommand.PayloadOneofCase.MonsterSpawn:
                    MonsterSpawnReceived?.Invoke(command.MonsterSpawn);
                    break;
                case SyncCommand.PayloadOneofCase.MonsterDamage:
                    MonsterDamageReceived?.Invoke(command.MonsterDamage);
                    break;
                case SyncCommand.PayloadOneofCase.BuffApply:
                    BuffApplyReceived?.Invoke(command.BuffApply);
                    break;
                case SyncCommand.PayloadOneofCase.BossAttack:
                    BossAttackReceived?.Invoke(command.BossAttack);
                    break;
                case SyncCommand.PayloadOneofCase.Win:
                    HandleWin(command.Win);
                    break;
                case SyncCommand.PayloadOneofCase.End:
                    HandleEnd(command.End);
                    break;
                case SyncCommand.PayloadOneofCase.None:
                    Debug.LogWarning(
                        $"[PK][Game] Sync has no typed payload. " +
                        $"Kind={command.CommandKind}, EventId={command.EventId}");
                    break;
                default:
                    Debug.LogWarning(
                        $"[PK][Game] Unsupported payload: {command.PayloadCase}");
                    break;
            }
        }

        private bool Accept(SyncCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.SessionId))
            {
                Debug.LogWarning("[PK][Game] Ignored sync without SessionId.");
                return false;
            }

            if (string.IsNullOrEmpty(SessionId))
            {
                SessionId = command.SessionId;
                LastSequence = -1;
            }
            else if (!string.Equals(SessionId, command.SessionId, StringComparison.Ordinal))
            {
                Debug.LogWarning(
                    $"[PK][Game] Ignored sync from another session. " +
                    $"Current={SessionId}, Received={command.SessionId}");
                return false;
            }

            if (command.Sequence <= LastSequence)
            {
                Debug.LogWarning(
                    $"[PK][Game] Ignored duplicate/out-of-order sync. " +
                    $"SessionId={SessionId}, Last={LastSequence}, Received={command.Sequence}");
                return false;
            }

            LastSequence = command.Sequence;
            return true;
        }

        private void HandleGift(GiftSyncPayload gift)
        {
            if (gift == null)
                return;

            GiftReceived?.Invoke(gift);
            if (!createUnitsFromGifts)
                return;

            if (!_giftRuleById.TryGetValue(gift.GiftId, out GiftUnitRule rule))
            {
                Debug.LogWarning(
                    $"[PK][Game] No GiftUnitRule for GiftId={gift.GiftId}; " +
                    "the gift event was dispatched but no unit was created.");
                return;
            }

            if (battleEngine == null)
                battleEngine = FindObjectOfType<BattleEngine>();
            if (battleEngine == null)
            {
                Debug.LogWarning(
                    $"[PK][Game] BattleEngine is unavailable; GiftId={gift.GiftId} " +
                    "cannot create a unit.");
                return;
            }

            long requested = Math.Max(1L, gift.GiftCount) * Math.Max(1, rule.unitsPerGift);
            int count = (int)Math.Min(requested, Math.Max(1, rule.maxUnitsPerEvent));
            ECampType camp = ResolveCamp(gift.SourceSide);

            battleEngine.CreateUnit(rule.unitId, camp, count);
            Log(
                $"Gift created units. GiftId={gift.GiftId}, GiftCount={gift.GiftCount}, " +
                $"UnitId={rule.unitId}, UnitCount={count}, Camp={camp}");
        }

        private void HandleWin(WinSyncPayload win)
        {
            if (win == null)
                return;

            WinnerSide = win.WinnerSide;
            ScoreA = win.ScoreA;
            ScoreB = win.ScoreB;
            Status = win.Status;
            WinReceived?.Invoke(win);
        }

        private void HandleEnd(EndSyncPayload end)
        {
            if (end == null)
                return;

            WinnerSide = end.WinnerSide;
            ScoreA = end.ScoreA;
            ScoreB = end.ScoreB;
            Status = end.Status;
            EndReceived?.Invoke(end);
        }

        private void OnBattleEnded(SubmitGiftResponse response)
        {
            if (response == null)
                return;

            Log(
                $"BattleEnded SessionId={response.SessionId}, " +
                $"Accepted={response.Accepted}, Reason={response.Reason}");
            BattleEnded?.Invoke(response);

            if (response.Accepted)
                ResetSession();
        }

        public ECampType ResolveCamp(PKSide side)
        {
            switch (side)
            {
                case PKSide.A:
                    return sideACamp;
                case PKSide.B:
                    return sideBCamp;
                default:
                    Debug.LogWarning(
                        $"[PK][Game] Side={side} has no camp mapping; using Side A camp.");
                    return sideACamp;
            }
        }

        public void RebuildGiftRuleCache()
        {
            _giftRuleById.Clear();
            if (giftUnitRules == null)
                return;

            foreach (GiftUnitRule rule in giftUnitRules)
            {
                if (rule == null || string.IsNullOrWhiteSpace(rule.giftId))
                    continue;

                _giftRuleById[rule.giftId.Trim()] = rule;
            }
        }

        public void ResetSession()
        {
            SessionId = string.Empty;
            LastSequence = -1;
            ScoreA = 0;
            ScoreB = 0;
            Status = PKSessionStatus.Created;
            WinnerSide = PKSide.Unknown;
        }

        private void Log(string message)
        {
            if (logGamePkEvents)
                Debug.Log($"[PK][Game] {message}");
        }
    }
}
