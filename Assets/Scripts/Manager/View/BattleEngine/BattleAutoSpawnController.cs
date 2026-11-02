using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战场自动出兵控制器。
/// 每条配置同时为红蓝双方安排同样数量和节奏的单位，双方只使用各自的单位配置编号。
/// </summary>
public class BattleAutoSpawnController : MonoBehaviour
{
    [Serializable]
    public class AutoSpawnEntry
    {
        [Header("红方单位配置编号"), Tooltip("红方单位配置编号")]
        public int redUnitId;
        [Header("蓝方单位配置编号"), Tooltip("蓝方单位配置编号")]
        public int blueUnitId;
        [Header("每波基础出兵数量"), Min(1), Tooltip("每一波的基础出兵数量")]
        public int countPerWave = 1;
        [Header("波次间隔（秒）"), Min(0.1f), Tooltip("两波之间的时间间隔，单位为秒")]
        public float waveInterval = 5f;
        [Header("同波单位间隔（秒）"), Min(0f), Tooltip("同一波中两个单位之间的出生间隔，单位为秒")]
        public float unitInterval = 0.2f;
        [Header("首次出兵延迟（秒）"), Min(0f), Tooltip("战斗开始后首次出兵延迟，单位为秒")]
        public float firstSpawnDelay;
    }

    [Serializable]
    public class SpawnTimeMultiplier
    {
        [Header("生效战场时间（秒）"), Min(0f), Tooltip("达到这个战场时间后生效，单位为秒")]
        public float battleTimeSeconds;
        [Header("出兵数量倍数"), Min(1f), Tooltip("达到时间后出兵数量乘以这个倍数，可叠加")]
        public float countMultiplier = 2f;
    }

    private class RuntimeEntry
    {
        public AutoSpawnEntry Config;
        public float NextWaveTime;
        public float NextUnitTime;
        public int SpawnedInWave;
        public int WaveCount;
        public bool WaveActive;
    }

    [Header("自动出兵配置")]
    [Header("启用自动出兵"), SerializeField] private bool enableAutoSpawn = true;
    [SerializeField] private List<AutoSpawnEntry> spawnEntries = new List<AutoSpawnEntry>();

    [Header("战场时间数量倍增")]
    [SerializeField] private List<SpawnTimeMultiplier> timeMultipliers = new List<SpawnTimeMultiplier>();

    [Header("出生位置")]
    [Header("出生横向半宽"), SerializeField, Min(0f)] private float spawnHalfWidth = BattleDefine.AreaTotalWith;
    [Header("出生边界留白"), SerializeField, Min(0f)] private float spawnBoundaryPadding = 0.5f;

    private readonly List<RuntimeEntry> runtimeEntries = new List<RuntimeEntry>();
    private BattleEngine battleEngine;
    private float battleElapsedTime;
    private bool initialized;
    private bool battleFinished;

    /// <summary>
    /// 由战斗引擎在初始化完成后调用。
    /// </summary>
    public void Initialize(BattleEngine engine)
    {
        if (battleEngine != null)
        {
            battleEngine.BattleFinishedFunction -= OnBattleFinished;
        }

        battleEngine = engine;
        battleFinished = false;
        battleElapsedTime = 0f;
        runtimeEntries.Clear();

        if (battleEngine == null || !enableAutoSpawn)
        {
            initialized = false;
            return;
        }

        battleEngine.BattleFinishedFunction += OnBattleFinished;
        for (int i = 0; i < spawnEntries.Count; i++)
        {
            AutoSpawnEntry config = spawnEntries[i];
            if (!IsValidConfig(config))
            {
                continue;
            }

            runtimeEntries.Add(new RuntimeEntry
            {
                Config = config,
                NextWaveTime = Mathf.Max(0f, config.firstSpawnDelay),
                NextUnitTime = 0f,
                SpawnedInWave = 0,
                WaveCount = 0,
                WaveActive = false
            });
        }

        initialized = runtimeEntries.Count > 0;
    }

    private void Update()
    {
        if (!initialized || battleFinished || battleEngine == null)
        {
            return;
        }

        battleElapsedTime += Time.deltaTime;
        for (int i = 0; i < runtimeEntries.Count; i++)
        {
            ProcessEntry(runtimeEntries[i]);
        }
    }

    private void ProcessEntry(RuntimeEntry runtimeEntry)
    {
        AutoSpawnEntry config = runtimeEntry.Config;
        if (!runtimeEntry.WaveActive)
        {
            if (battleElapsedTime < runtimeEntry.NextWaveTime)
            {
                return;
            }

            runtimeEntry.WaveActive = true;
            runtimeEntry.SpawnedInWave = 0;
            runtimeEntry.WaveCount = Mathf.Max(1, Mathf.CeilToInt(
                config.countPerWave * GetCurrentCountMultiplier()));
            runtimeEntry.NextUnitTime = battleElapsedTime;
        }

        if (battleElapsedTime < runtimeEntry.NextUnitTime)
        {
            return;
        }

        SpawnUnitPair(config);
        runtimeEntry.SpawnedInWave++;
        if (runtimeEntry.SpawnedInWave >= runtimeEntry.WaveCount)
        {
            runtimeEntry.WaveActive = false;
            runtimeEntry.NextWaveTime = battleElapsedTime + Mathf.Max(0.1f, config.waveInterval);
            return;
        }

        runtimeEntry.NextUnitTime = battleElapsedTime + Mathf.Max(0f, config.unitInterval);
    }

    private void SpawnUnitPair(AutoSpawnEntry config)
    {
        SpawnUnit(config.redUnitId, ECampType.Red);
        SpawnUnit(config.blueUnitId, ECampType.Blue);
    }

    private void SpawnUnit(int unitId, ECampType campType)
    {
        if (unitId <= 0 || UnitFactory.IsHero(unitId))
        {
            return;
        }

        Vector3 forward = battleEngine.BornConfigIns.GetForward(campType).normalized;
        Vector3 bornPoint = battleEngine.BornConfigIns.GetBornPoint(campType);
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        SoliderCfg soliderCfg = SoliderCfgConfig.Ins.SearchById(unitId);
        if (soliderCfg == null)
        {
            return;
        }

        float radius = soliderCfg != null ? soliderCfg.radius : 1.5f;
        float halfWidth = Mathf.Max(0f, Mathf.Min(
            spawnHalfWidth,
            BattleDefine.AreaTotalWith) - spawnBoundaryPadding - radius);
        float bornCoordinate = Vector3.Dot(bornPoint, right);
        float minOffset = -halfWidth - bornCoordinate;
        float maxOffset = halfWidth - bornCoordinate;
        float randomOffset = minOffset <= maxOffset
            ? UnityEngine.Random.Range(minOffset, maxOffset)
            : 0f;
        bornPoint += right * randomOffset;
        battleEngine.CreateUnitAtPoint(unitId, campType, bornPoint, forward);
    }

    private float GetCurrentCountMultiplier()
    {
        float multiplier = 1f;
        for (int i = 0; i < timeMultipliers.Count; i++)
        {
            SpawnTimeMultiplier config = timeMultipliers[i];
            if (config != null && battleElapsedTime >= config.battleTimeSeconds)
            {
                multiplier *= Mathf.Max(1f, config.countMultiplier);
            }
        }

        return multiplier;
    }

    private bool IsValidConfig(AutoSpawnEntry config)
    {
        return config != null
            && config.redUnitId > 0
            && config.blueUnitId > 0
            && config.countPerWave > 0
            && config.waveInterval > 0f;
    }

    private void OnValidate()
    {
        for (int i = 0; i < spawnEntries.Count; i++)
        {
            AutoSpawnEntry config = spawnEntries[i];
            if (config == null)
            {
                continue;
            }

            config.countPerWave = Mathf.Max(1, config.countPerWave);
            config.waveInterval = Mathf.Max(0.1f, config.waveInterval);
            config.unitInterval = Mathf.Max(0f, config.unitInterval);
            config.firstSpawnDelay = Mathf.Max(0f, config.firstSpawnDelay);
        }

        for (int i = 0; i < timeMultipliers.Count; i++)
        {
            SpawnTimeMultiplier config = timeMultipliers[i];
            if (config != null)
            {
                config.battleTimeSeconds = Mathf.Max(0f, config.battleTimeSeconds);
                config.countMultiplier = Mathf.Max(1f, config.countMultiplier);
            }
        }
    }

    private void OnBattleFinished(ECampType winnerCamp)
    {
        battleFinished = true;
    }

    private void OnDestroy()
    {
        if (battleEngine != null)
        {
            battleEngine.BattleFinishedFunction -= OnBattleFinished;
        }
    }
}
