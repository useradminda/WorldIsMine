using System.Collections.Generic;
using UnityEngine;

/// <summary>记录同一目标下已经进入弓手攻击状态的弓手位置，避免新弓手重叠。</summary>
public static class ActiveArcherPositionRegistry
{
    private sealed class ArcherRecord
    {
        public int ArcherUid;
        public Vector3 Position;
        public float Radius;
    }

    private static readonly Dictionary<int, List<ArcherRecord>> RecordsByTarget = new Dictionary<int, List<ArcherRecord>>();

    /// <summary>当前位置合法时记录弓手，返回是否可以进入攻击状态。</summary>
    public static bool TryRegisterCurrentPosition(UnitLogicBase archer, UnitLogicBase target)
    {
        if (archer == null || target == null)
        {
            return false;
        }

        if (!RecordsByTarget.TryGetValue(target.UId, out List<ArcherRecord> records))
        {
            records = new List<ArcherRecord>();
            RecordsByTarget.Add(target.UId, records);
        }

        float radius = Mathf.Max(0.1f, archer.Prop.Radius);
        for (int i = 0; i < records.Count; i++)
        {
            float minDistance = radius + records[i].Radius;
            if ((archer.CurPos - records[i].Position).sqrMagnitude < minDistance * minDistance)
            {
                return false;
            }
        }

        records.Add(new ArcherRecord { ArcherUid = archer.UId, Position = archer.CurPos, Radius = radius });
        return true;
    }

    /// <summary>释放弓手攻击位置记录。</summary>
    public static void Release(UnitLogicBase archer, UnitLogicBase target)
    {
        if (archer == null || target == null || !RecordsByTarget.TryGetValue(target.UId, out List<ArcherRecord> records))
        {
            return;
        }

        for (int i = records.Count - 1; i >= 0; i--)
        {
            if (records[i].ArcherUid == archer.UId)
            {
                records.RemoveAt(i);
                break;
            }
        }

        if (records.Count == 0)
        {
            RecordsByTarget.Remove(target.UId);
        }
    }
}
