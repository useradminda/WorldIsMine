using System;
using System.Collections.Generic;
using UnityEngine;
using ZTools;
using ZTools;
[Serializable]
public class FootSmokeSettings
{
    public GameObject Prefab;
    [Range(1, 300)] public int MaxCount = 80;
    public Vector3 LocalOffset = Vector3.zero;
}

public class FootSmokeManager : Singleton<FootSmokeManager>, IManager
{
    private class SmokeSlot
    {
        public GameObject Object;
        public ParticleSystem[] Particles;
        public UnitView Owner;
    }

    private readonly Stack<SmokeSlot> freeSlots = new Stack<SmokeSlot>();
    private readonly Dictionary<UnitView, SmokeSlot> ownerSlots = new Dictionary<UnitView, SmokeSlot>();
    private FootSmokeSettings settings;
    private Transform root;

    public void SetSettings(FootSmokeSettings config)
    {
        settings = config;
    }

    public void ManagerInit()
    {
        ManagerDestroy();
        if (settings == null)
        {
            settings = new FootSmokeSettings();
        }
        if (settings.Prefab == null)
        {
            settings.Prefab = Resources.Load<GameObject>("Effects/Foot_Smoke");
        }
        if (settings.Prefab == null)
        {
            Debug.LogWarning("FootSmokeManager: 未找到 Resources/Effects/Foot_Smoke.prefab");
            return;
        }
        root = new GameObject("FootSmokes").transform;
        root.gameObject.SetActive(false);
        int count = Mathf.Clamp(settings.MaxCount, 1, 300);
        for (int i = 0; i < count; i++)
        {
            GameObject smokeObject = UnityEngine.Object.Instantiate(settings.Prefab, root);
            SmokeSlot slot = new SmokeSlot { Object = smokeObject, Particles = smokeObject.GetComponentsInChildren<ParticleSystem>(true) };
            smokeObject.SetActive(false);
            freeSlots.Push(slot);
        }
        root.gameObject.SetActive(true);
    }

    public void StartForSoldier(UnitView unitView, UnitLogicBase unitLogic)
    {
        if (unitView == null || unitLogic == null || unitLogic.IsDead || UnitFactory.IsHero(unitLogic.SoliderCfg.id) || ownerSlots.ContainsKey(unitView) || freeSlots.Count == 0)
        {
            return;
        }
        SmokeSlot slot = freeSlots.Pop();
        slot.Owner = unitView;
        ownerSlots[unitView] = slot;
        slot.Object.transform.SetParent(unitView.transform, false);
        slot.Object.transform.localPosition = settings != null ? settings.LocalOffset : Vector3.zero;
        slot.Object.transform.localRotation = Quaternion.identity;
        slot.Object.SetActive(true);
        for (int i = 0; i < slot.Particles.Length; i++)
        {
            slot.Particles[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            slot.Particles[i].Play(false);
        }
    }

    public void StopForUnit(UnitView unitView)
    {
        if (unitView == null || !ownerSlots.TryGetValue(unitView, out SmokeSlot slot))
        {
            return;
        }
        ownerSlots.Remove(unitView);
        slot.Owner = null;
        for (int i = 0; i < slot.Particles.Length; i++)
        {
            slot.Particles[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        slot.Object.SetActive(false);
        slot.Object.transform.SetParent(root, false);
        freeSlots.Push(slot);
    }

    public void ManagerUpdate(float dt) { }
    public void ManagerLateUpdate(float dt) { }

    public void ManagerRefuse()
    {
        if (ownerSlots.Count == 0)
        {
            return;
        }
        List<UnitView> owners = new List<UnitView>(ownerSlots.Keys);
        for (int i = 0; i < owners.Count; i++)
        {
            StopForUnit(owners[i]);
        }
    }

    public void ManagerDestroy()
    {
        ManagerRefuse();
        if (root != null)
        {
            UnityEngine.Object.Destroy(root.gameObject);
        }
        root = null;
        freeSlots.Clear();
        ownerSlots.Clear();
    }
}
