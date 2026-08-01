using System;
using System.Collections.Generic;
using ClientProtocol;
using PlayerProtocol;
using UnityEngine;
using WorldIsMine.Net.Runtime;

namespace WorldIsMine.Game.PK
{
    /// <summary>
    /// Converts server PK synchronization messages into game-layer events.
    /// Add this component to the battle scene and assign NetworkRuntime.
    /// </summary>
    public sealed class PkBattleNetworkAdapter : MonoBehaviour
    {
        [Serializable]
        public sealed class TroopUnitMapping
        {
            [Tooltip("Troop ID sent by the server.")]
            public uint troopId;

            [Tooltip("Unity SoliderCfg ID used by BattleEngine.CreateUnit.")]
            public int unitConfigId;
        }

        [Header("Dependencies")]
        [SerializeField] private NetworkRuntime networkRuntime;
        [SerializeField] private BattleEngine battleEngine;

        [Header("Side Mapping")]
        [SerializeField] private ECampType sideACamp = ECampType.Red;
        [SerializeField] private ECampType sideBCamp = ECampType.Blue;

        [Header("Troop -> Battle Unit")]
        [SerializeField] private bool createUnitsFromGifts = true;
        [Min(1)]
        [SerializeField] private int maxUnitsPerGiftEvent = 100;
        [Tooltip("Optional Inspector overrides. Built-in mappings cover the ten server troop IDs.")]
        [SerializeField] private TroopUnitMapping[] troopUnitMappings = Array.Empty<TroopUnitMapping>();

        [Header("Logging")]
        [SerializeField] private bool logGamePkEvents = true;

        private bool _subscribed;
        private readonly Dictionary<uint, int> _unitConfigIdByTroopId =
            new Dictionary<uint, int>();
        private readonly Dictionary<ulong, ECampType> _playerCamps =
            new Dictionary<ulong, ECampType>();
        private readonly Dictionary<string, CampBuffState> _activeCampBuffs =
            new Dictionary<string, CampBuffState>(StringComparer.Ordinal);

        public string SessionId { get; private set; } = string.Empty;
        public long LastSequence { get; private set; } = -1;
        public long ScoreA { get; private set; }
        public long ScoreB { get; private set; }
        public PKSessionStatus Status { get; private set; } = PKSessionStatus.Created;
        public PKSide WinnerSide { get; private set; }
        public bool BattleRunning => Status == PKSessionStatus.Running;
        public IReadOnlyCollection<CampBuffState> ActiveCampBuffs => _activeCampBuffs.Values;

        public event Action<SessionSnapshot> BattleStarted;
        public event Action<SyncCommand> SyncReceived;
        public event Action<GiftSyncPayload> GiftReceived;
        public event Action<GiftSyncPayload, GiftTroopSpawnData> TroopSpawnReceived;
        public event Action<AttackSyncPayload> AttackReceived;
        public event Action<MonsterSpawnSyncPayload> MonsterSpawnReceived;
        public event Action<MonsterDamageSyncPayload> MonsterDamageReceived;
        public event Action<BuffApplySyncPayload> BuffApplyReceived;
        public event Action<CampBuffChange> CampBuffChanged;
        public event Action<BossAttackSyncPayload> BossAttackReceived;
        public event Action<WinSyncPayload> WinReceived;
        public event Action<EndSyncPayload> EndReceived;
        public event Action<SubmitGiftResponse> BattleEnded;

        private void Awake()
        {
            RebuildTroopUnitMappings();

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
            maxUnitsPerGiftEvent = Math.Max(1, maxUnitsPerGiftEvent);
            RebuildTroopUnitMappings();
        }

        private void Subscribe()
        {
            if (_subscribed || networkRuntime == null)
                return;

            networkRuntime.PkBattleStarted += OnBattleStarted;
            networkRuntime.PkSyncReceived += OnSyncReceived;
            networkRuntime.PkBattleEnded += OnBattleEnded;
            networkRuntime.PlayerCampSelected += OnPlayerCampSelected;
            networkRuntime.PlayerLeft += OnPlayerLeft;
            networkRuntime.PlayerGifted += OnLocalPlayerGifted;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || networkRuntime == null)
                return;

            networkRuntime.PkBattleStarted -= OnBattleStarted;
            networkRuntime.PkSyncReceived -= OnSyncReceived;
            networkRuntime.PkBattleEnded -= OnBattleEnded;
            networkRuntime.PlayerCampSelected -= OnPlayerCampSelected;
            networkRuntime.PlayerLeft -= OnPlayerLeft;
            networkRuntime.PlayerGifted -= OnLocalPlayerGifted;
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
            _activeCampBuffs.Clear();
            foreach (CampBuffState buff in snapshot.ActiveBuffs)
            {
                if (buff != null)
                    _activeCampBuffs[BuildBuffKey(buff)] = buff.Clone();
            }

            Log(
                $"BattleStarted SessionId={SessionId}, Sequence={LastSequence}, " +
                $"Score={ScoreA}:{ScoreB}, Status={Status}, " +
                $"ActiveCampBuffs={_activeCampBuffs.Count}");
            foreach (CampBuffState buff in _activeCampBuffs.Values)
                LogBuffState("Snapshot", buff, string.Empty);
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
                case SyncCommand.PayloadOneofCase.CampBuffChanged:
                    HandleCampBuffChanges(command.CampBuffChanged?.Changes);
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
            HandleCampBuffChanges(gift.BuffChanges);
            if (gift.TroopSpawns.Count == 0)
            {
                Log(
                    $"Gift has no troop spawn. GiftId={gift.GiftId}, " +
                    $"PlayerId={gift.PlayerId}");
                return;
            }

            foreach (GiftTroopSpawnData spawn in gift.TroopSpawns)
            {
                TroopSpawnReceived?.Invoke(gift, spawn);
                Log(
                    $"TroopSpawn GiftId={gift.GiftId}, PlayerId={gift.PlayerId}, " +
                    $"TroopId={spawn.TroopId}, TroopLevel={spawn.TroopLevel}, " +
                    $"TroopCount={spawn.TroopCount}, Side={gift.SourceSide}");
                CreateGiftTroops(gift, spawn);
            }
        }

        private void CreateGiftTroops(GiftSyncPayload gift, GiftTroopSpawnData spawn)
        {
            if (gift.SourceSide != PKSide.A && gift.SourceSide != PKSide.B)
            {
                Debug.LogWarning(
                    $"[PK][Game] Cannot create troop for unknown side. " +
                    $"TroopId={spawn.TroopId}, Side={gift.SourceSide}");
                return;
            }

            ECampType camp = ResolveCamp(gift.SourceSide);
            CreateTroops(
                spawn,
                camp,
                gift.PlayerId,
                $"PK GiftId={gift.GiftId}");
        }

        private void OnPlayerCampSelected(LivePlayerCampSelectedNotify notify)
        {
            if (notify == null)
                return;

            switch (notify.Camp)
            {
                case LivePlayerCamp.Red:
                    _playerCamps[notify.PlayerId] = ECampType.Red;
                    break;
                case LivePlayerCamp.Blue:
                    _playerCamps[notify.PlayerId] = ECampType.Blue;
                    break;
                default:
                    _playerCamps.Remove(notify.PlayerId);
                    break;
            }
        }

        private void OnPlayerLeft(LivePlayerLeaveNotify notify)
        {
            if (notify != null)
                _playerCamps.Remove(notify.PlayerId);
        }

        private void OnLocalPlayerGifted(LivePlayerGiftNotify notify)
        {
            if (notify?.TroopSpawn == null)
                return;

            // PK-running gifts arrive through SyncCommand for both sides.
            if (BattleRunning)
            {
                Log(
                    $"Ignored local gift troop while PK is running. EventId={notify.EventId}, " +
                    $"TroopId={notify.TroopSpawn.TroopId}");
                return;
            }

            if (!_playerCamps.TryGetValue(notify.PlayerId, out ECampType camp))
            {
                Debug.LogWarning(
                    $"[PK][Game] Local gift cannot create troops before camp selection. " +
                    $"PlayerId={notify.PlayerId}, EventId={notify.EventId}, " +
                    $"TroopId={notify.TroopSpawn.TroopId}");
                return;
            }

            CreateTroops(
                notify.TroopSpawn,
                camp,
                notify.PlayerId,
                $"Local GiftId={notify.GiftId}, EventId={notify.EventId}");
        }

        private void CreateTroops(
            GiftTroopSpawnData spawn,
            ECampType camp,
            ulong playerId,
            string source)
        {
            if (!createUnitsFromGifts)
            {
                Log($"Troop creation is disabled. TroopId={spawn.TroopId}, Source={source}");
                return;
            }

            if (!TryResolveUnitConfigId(spawn.TroopId, out int unitConfigId))
            {
                Debug.LogWarning(
                    $"[PK][Game] No Unity unit mapping for server TroopId={spawn.TroopId}; " +
                    "the troop event was dispatched but no unit was created.");
                return;
            }

            if (spawn.TroopCount <= 0)
            {
                Debug.LogWarning(
                    $"[PK][Game] Ignored invalid troop count. " +
                    $"TroopId={spawn.TroopId}, TroopCount={spawn.TroopCount}");
                return;
            }

            if (battleEngine == null)
                battleEngine = FindObjectOfType<BattleEngine>();
            if (battleEngine == null)
            {
                Debug.LogWarning(
                    $"[PK][Game] BattleEngine is unavailable; TroopId={spawn.TroopId} " +
                    "cannot create units.");
                return;
            }

            int unitCount = Math.Min(spawn.TroopCount, Math.Max(1, maxUnitsPerGiftEvent));
            try
            {
                battleEngine.CreateUnit(unitConfigId, camp, unitCount);
                Log(
                    $"TroopSpawn created units. ServerTroopId={spawn.TroopId}, " +
                    $"UnitConfigId={unitConfigId}, TroopLevel={spawn.TroopLevel}, " +
                    $"UnitCount={unitCount}, Camp={camp}, PlayerId={playerId}, Source={source}");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[PK][Game] TroopSpawn creation failed. ServerTroopId={spawn.TroopId}, " +
                    $"UnitConfigId={unitConfigId}, UnitCount={unitCount}, Camp={camp}, " +
                    $"PlayerId={playerId}, Source={source}, Error={exception}");
            }
        }

        public bool TryResolveUnitConfigId(uint troopId, out int unitConfigId)
        {
            if (_unitConfigIdByTroopId.Count == 0)
                RebuildTroopUnitMappings();

            return _unitConfigIdByTroopId.TryGetValue(troopId, out unitConfigId);
        }

        public void RebuildTroopUnitMappings()
        {
            _unitConfigIdByTroopId.Clear();

            // Server TroopIds are stable business IDs. Unity IDs are presentation/config IDs.
            for (uint index = 0; index < 5; index++)
            {
                _unitConfigIdByTroopId[10001 + index] = 1 + (int)index;
                _unitConfigIdByTroopId[20001 + index] = 6 + (int)index;
            }

            if (troopUnitMappings == null)
                return;

            foreach (TroopUnitMapping mapping in troopUnitMappings)
            {
                if (mapping == null || mapping.troopId == 0 || mapping.unitConfigId <= 0)
                    continue;

                _unitConfigIdByTroopId[mapping.troopId] = mapping.unitConfigId;
            }
        }

        private void HandleCampBuffChanges(IEnumerable<CampBuffChange> changes)
        {
            if (changes == null)
                return;

            foreach (CampBuffChange change in changes)
            {
                if (change == null)
                    continue;

                if (change.ChangeType == PKCampBuffChangeType.ClearAll)
                {
                    _activeCampBuffs.Clear();
                    Log($"CampBuff ClearAll Reason={change.Reason}");
                    CampBuffChanged?.Invoke(change);
                    continue;
                }

                CampBuffState buff = change.Buff;
                if (buff == null)
                    continue;

                string key = BuildBuffKey(buff);
                if (change.ChangeType == PKCampBuffChangeType.Removed)
                    _activeCampBuffs.Remove(key);
                else
                    _activeCampBuffs[key] = buff.Clone();

                LogBuffState(change.ChangeType.ToString(), buff, change.Reason);
                CampBuffChanged?.Invoke(change);
            }
        }

        private static string BuildBuffKey(CampBuffState buff)
        {
            return $"{(int)buff.TargetSide}:{buff.BuffId}";
        }

        private void LogBuffState(string operation, CampBuffState buff, string reason)
        {
            Log(
                $"CampBuff {operation} InstanceId={buff.InstanceId}, BuffId={buff.BuffId}, " +
                $"Name={buff.BuffName}, Level={buff.BuffLevel}, TargetSide={buff.TargetSide}, " +
                $"SourceSide={buff.SourceSide}, PlayerId={buff.SourcePlayerId}, " +
                $"Stacks={buff.StackCount}, Duration={buff.DurationMs}, " +
                $"Start={buff.StartTimeMs}, Expire={buff.ExpireTimeMs}, " +
                $"Version={buff.Version}, Effect={buff.EffectType}:{buff.EffectValue}, Reason={reason}");
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
            _activeCampBuffs.Clear();
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

        public void ResetSession()
        {
            SessionId = string.Empty;
            LastSequence = -1;
            ScoreA = 0;
            ScoreB = 0;
            Status = PKSessionStatus.Created;
            WinnerSide = PKSide.Unknown;
            _activeCampBuffs.Clear();
        }

        private void Log(string message)
        {
            if (logGamePkEvents)
                Debug.Log($"[PK][Game] {message}");
        }
    }
}
