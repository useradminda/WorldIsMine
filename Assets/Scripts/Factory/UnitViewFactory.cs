using System;
using UnityEngine;

public class UnitViewFactory
{
    public static UnitView CreateUnitView(
        string prefab,
        Vector3 initPos,
        Vector3 initForward,
        UnitLogicBase unitBase)
    {
        if (string.IsNullOrWhiteSpace(prefab))
            throw new ArgumentException("Unit prefab path is required.", nameof(prefab));
        if (unitBase == null)
            throw new ArgumentNullException(nameof(unitBase));

        GameObject template = Resources.Load<GameObject>(prefab);
        if (template == null && prefab.StartsWith("Soldier/", StringComparison.Ordinal))
        {
            string correctedPrefab = "Soliders/" + prefab.Substring("Soldier/".Length);
            template = Resources.Load<GameObject>(correctedPrefab);
        }

        if (template == null)
        {
            throw new InvalidOperationException(
                $"Unit prefab was not found at Resources/{prefab}.prefab");
        }

        Quaternion rotation = initForward.sqrMagnitude > 0f
            ? Quaternion.LookRotation(initForward.normalized)
            : Quaternion.identity;
        GameObject unitGob = UnityEngine.Object.Instantiate(
            template,
            initPos,
            rotation);
        UnitView unitView = unitGob.GetOrAddComponent<UnitView>();
        unitView.Init(unitBase);
        return unitView;
    }
}
