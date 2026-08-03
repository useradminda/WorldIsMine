using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ClientProtocol;
using WorldIsMine.Net.Protocol;
using WorldIsMine.Net.Runtime;

namespace WorldIsMine.Net.Services
{
    public sealed class ClientGameConfigSnapshot
    {
        private readonly Dictionary<int, GameGiftConfig> _giftsById;
        private readonly Dictionary<int, GameEquipmentConfig> _equipmentsById;
        private readonly Dictionary<int, GameBuffConfig> _buffsById;
        private readonly Dictionary<int, GameFlyObjectConfig> _flyObjectsById;
        private readonly Dictionary<int, GameSkillConfig> _skillsById;
        private readonly Dictionary<int, GameSoldierConfig> _soldiersById;

        private ClientGameConfigSnapshot(S2CGameConfigPush push)
        {
            if (push == null)
                throw new ArgumentNullException(nameof(push));
            if (string.IsNullOrWhiteSpace(push.Version))
                throw new InvalidOperationException("Game config version is required.");

            Version = push.Version;
            Gifts = CloneRows(push.Gifts, row => row.Clone());
            Equipments = CloneRows(push.Equipments, row => row.Clone());
            Buffs = CloneRows(push.Buffs, row => row.Clone());
            FlyObjects = CloneRows(push.FlyObjects, row => row.Clone());
            Skills = CloneRows(push.Skills, row => row.Clone());
            Soldiers = CloneRows(push.Soldiers, row => row.Clone());

            if (Gifts.Count == 0)
                throw new InvalidOperationException("Game gift config is empty.");

            _giftsById = BuildIndex(Gifts, row => row.Id, "gift");
            _equipmentsById = BuildIndex(Equipments, row => row.Id, "equipment");
            _buffsById = BuildIndex(Buffs, row => row.Id, "buff");
            _flyObjectsById = BuildIndex(FlyObjects, row => row.Id, "fly_object");
            _skillsById = BuildIndex(Skills, row => row.Id, "skill");
            _soldiersById = BuildIndex(Soldiers, row => row.Id, "soldier");

            foreach (GameSkillConfig skill in Skills)
            {
                if (skill.FlyObjectId != 0 && !_flyObjectsById.ContainsKey(skill.FlyObjectId))
                {
                    throw new InvalidOperationException(
                        $"Skill {skill.Id} references missing fly object {skill.FlyObjectId}.");
                }
            }

            foreach (GameSoldierConfig soldier in Soldiers)
            {
                foreach (int skillId in soldier.SkillIds)
                {
                    if (skillId != 0 && !_skillsById.ContainsKey(skillId))
                    {
                        throw new InvalidOperationException(
                            $"Soldier {soldier.Id} references missing skill {skillId}.");
                    }
                }
            }
        }

        public string Version { get; }
        public IReadOnlyList<GameGiftConfig> Gifts { get; }
        public IReadOnlyList<GameEquipmentConfig> Equipments { get; }
        public IReadOnlyList<GameBuffConfig> Buffs { get; }
        public IReadOnlyList<GameFlyObjectConfig> FlyObjects { get; }
        public IReadOnlyList<GameSkillConfig> Skills { get; }
        public IReadOnlyList<GameSoldierConfig> Soldiers { get; }

        internal static ClientGameConfigSnapshot Create(S2CGameConfigPush push)
        {
            return new ClientGameConfigSnapshot(push);
        }

        public bool TryGetGift(int id, out GameGiftConfig value) =>
            _giftsById.TryGetValue(id, out value);

        public bool TryGetEquipment(int id, out GameEquipmentConfig value) =>
            _equipmentsById.TryGetValue(id, out value);

        public bool TryGetBuff(int id, out GameBuffConfig value) =>
            _buffsById.TryGetValue(id, out value);

        public bool TryGetFlyObject(int id, out GameFlyObjectConfig value) =>
            _flyObjectsById.TryGetValue(id, out value);

        public bool TryGetSkill(int id, out GameSkillConfig value) =>
            _skillsById.TryGetValue(id, out value);

        public bool TryGetSoldier(int id, out GameSoldierConfig value) =>
            _soldiersById.TryGetValue(id, out value);

        private static IReadOnlyList<T> CloneRows<T>(
            IEnumerable<T> source,
            Func<T, T> clone)
        {
            return Array.AsReadOnly(source.Select(clone).ToArray());
        }

        private static Dictionary<int, T> BuildIndex<T>(
            IEnumerable<T> rows,
            Func<T, int> getId,
            string table)
        {
            var result = new Dictionary<int, T>();
            foreach (T row in rows)
            {
                int id = getId(row);
                if (id <= 0 || result.ContainsKey(id))
                    throw new InvalidOperationException($"Invalid or duplicate {table} config id: {id}.");
                result.Add(id, row);
            }
            return result;
        }
    }

    public sealed class GameConfigService
    {
        private readonly MainThreadDispatcher _mainThread;
        private ClientGameConfigSnapshot _current;
        private int _generation;

        public GameConfigService(
            MessageRouter router,
            MainThreadDispatcher mainThread)
        {
            if (router == null)
                throw new ArgumentNullException(nameof(router));
            _mainThread = mainThread ?? throw new ArgumentNullException(nameof(mainThread));

            router.Register(
                RequestCode.S2CGameConfig,
                ActionCode.None,
                S2CGameConfigPush.Parser,
                OnConfigPush);
        }

        public event Action<ClientGameConfigSnapshot> Updated;

        public ClientGameConfigSnapshot Current => Volatile.Read(ref _current);
        public bool IsReady => Current != null;

        internal void Reset()
        {
            Interlocked.Increment(ref _generation);
            Interlocked.Exchange(ref _current, null);
        }

        private void OnConfigPush(S2CGameConfigPush push, NetPacket packet)
        {
            ClientGameConfigSnapshot next = ClientGameConfigSnapshot.Create(push);
            int generation = Volatile.Read(ref _generation);
            _mainThread.Post(() =>
            {
                if (generation != Volatile.Read(ref _generation))
                    return;

                Interlocked.Exchange(ref _current, next);
                Updated?.Invoke(next);
            });
        }
    }
}
