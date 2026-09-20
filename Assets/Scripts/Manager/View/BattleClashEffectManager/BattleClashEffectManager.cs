using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using ZTools;

/// <summary>交战示意特效参数。</summary>
[Serializable]
public class BattleClashEffectSettings
{
    public GameObject Prefab;
    [Range(1, 10)] public int MaxCount = 10;
    [Range(0f, 1f)] public float SpawnChance = 0.1f;
    [FormerlySerializedAs("ContactLifetime")]
    [Min(0.1f)] public float EffectLifetime = 3f;
    public float HeightOffset = 0.5f;
}

/// <summary>随机接受交战点，使用固定容量缓存和独立计时器显示特效。</summary>
public class BattleClashEffectManager : Singleton<BattleClashEffectManager>, IManager
{
    private class EffectSlot
    {
        public GameObject Object;
        public ParticleSystem[] Particles;
        public float RemainingTime;
        public bool Active;
    }

    private readonly List<EffectSlot> slots = new List<EffectSlot>(10);
    private readonly Stack<EffectSlot> freeSlots = new Stack<EffectSlot>(10);
    private BattleClashEffectSettings settings;
    private Transform root;

    /// <summary>设置参数，下次初始化时应用缓存容量。</summary>
    public void SetSettings(BattleClashEffectSettings config)
    {
        settings = config;
    }

    /// <summary>预创建最多十个特效，避免交战时实例化。</summary>
    public void ManagerInit()
    {
        ManagerDestroy();
        if (settings == null || settings.Prefab == null)
        {
            return;
        }
        root = new GameObject("BattleClashEffects").transform;
        root.gameObject.SetActive(false);
        int count = Mathf.Clamp(settings.MaxCount, 1, 10);
        for (int i = 0; i < count; i++)
        {
            GameObject instance = UnityEngine.Object.Instantiate(settings.Prefab, root);
            EffectSlot slot = new EffectSlot
            {
                Object = instance,
                Particles = instance.GetComponentsInChildren<ParticleSystem>(true)
            };
            for (int j = 0; j < slot.Particles.Length; j++)
            {
                ParticleSystem.MainModule main = slot.Particles[j].main;
                main.stopAction = ParticleSystemStopAction.None;
            }
            instance.SetActive(false);
            slots.Add(slot);
            freeSlots.Push(slot);
        }
        root.gameObject.SetActive(true);
    }

    /// <summary>有空位时按概率接受当前交战点，满员直接忽略，不保存候选点。</summary>
    public void ReportContact(Vector3 position)
    {
        if (root == null || freeSlots.Count == 0)
        {
            return;
        }
        float chance = Mathf.Clamp01(settings.SpawnChance);
        if (chance <= 0f || (chance < 1f && UnityEngine.Random.value >= chance))
        {
            return;
        }
        EffectSlot slot = freeSlots.Pop();
        slot.Active = true;
        slot.RemainingTime = Mathf.Max(0.1f, settings.EffectLifetime);
        slot.Object.transform.position = position + Vector3.up * settings.HeightOffset;
        slot.Object.SetActive(true);
        for (int i = 0; i < slot.Particles.Length; i++)
        {
            slot.Particles[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            slot.Particles[i].Play(false);
        }
    }

    /// <summary>仅更新最多十个特效的独立计时器，到期归还缓存。</summary>
    public void ManagerUpdate(float dt)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            EffectSlot slot = slots[i];
            if (!slot.Active)
            {
                continue;
            }
            slot.RemainingTime -= dt;
            if (slot.RemainingTime <= 0f)
            {
                Recycle(slot);
            }
        }
    }

    /// <summary>无需延迟更新，保留统一管理器接口。</summary>
    public void ManagerLateUpdate(float dt)
    {
    }

    /// <summary>回收所有正在显示的特效，保留缓存供后续使用。</summary>
    public void ManagerRefuse()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            Recycle(slots[i]);
        }
    }

    /// <summary>战斗结束或场景退出时释放缓存，支持重复清理。</summary>
    public void ManagerDestroy()
    {
        ManagerRefuse();
        if (root != null)
        {
            UnityEngine.Object.Destroy(root.gameObject);
        }
        root = null;
        slots.Clear();
        freeSlots.Clear();
    }

    /// <summary>停播并归还特效，防止同一空位重复入池。</summary>
    private void Recycle(EffectSlot slot)
    {
        if (!slot.Active)
        {
            return;
        }
        for (int i = 0; i < slot.Particles.Length; i++)
        {
            slot.Particles[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        slot.Object.SetActive(false);
        slot.Active = false;
        slot.RemainingTime = 0f;
        freeSlots.Push(slot);
    }
}
