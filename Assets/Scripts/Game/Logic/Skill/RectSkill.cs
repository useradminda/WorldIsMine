using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// 近战矩形范围技能。技能释放时立即完成一次范围判定，不创建飞行器。
/// skillParam 格式：矩形宽度,矩形前方长度。
/// </summary>
public class RectSkill : SkillLogicBase
{
    private readonly HashSet<int> damagedUnitIndices = new HashSet<int>();
    private Vector3 castForward = Vector3.forward;
    private float rectWidth;
    private float rectLength;
    private bool paramLogged;

    public RectSkill(UnitLogicBase unitLogic, SkillCfg skillCfg) : base(unitLogic, skillCfg)
    {
    }

    public override bool RequiresAreaSearch => false;

    public override void OnSkillDoEffect(UnitLogicBase target, List<int> resultUnitIndexList)
    {
        if (!TryParseRect(out rectWidth, out rectLength))
        {
            return;
        }

        // 技能状态在执行技能前已经将角色转向释放方向，这里直接记录本次释放方向。
        castForward = GetCastForward();
        castForward.y = 0f;
        castForward.Normalize();
        damagedUnitIndices.Clear();

        int attackDamage = GetAttackDamageSnapshot();
        // 这是低频瞬发技能，直接遍历敌方阵营列表，保证长矩形范围内的目标全部参与判定。
        List<UnitLogicBase> enemyUnits = UnitManager.Instance.GetCampUnitList(
            UnitLogic.CampType == ECampType.Red ? ECampType.Blue : ECampType.Red);
        for (int i = 0; i < enemyUnits.Count; i++)
        {
            TryDamageTarget(enemyUnits[i], attackDamage);
        }
    }

    protected override void PlayStartEffect()
    {
        if (SkillCfg == null || string.IsNullOrWhiteSpace(SkillCfg.startEffect)
            || UnitLogic.UnitView == null)
        {
            return;
        }

        // 将技能特效挂在角色胸前，角色移动时特效同步移动并由对象池回收。
        UnitLogic.UnitView.PlayAttachEffect(
            SkillCfg.startEffect,
            5f,
            new Vector3(0f, 0.8f, 0.5f));
    }

    private void TryDamageTarget(UnitLogicBase target, int attackDamage)
    {
        if (target == null || target.IsDead || target.CampTypeInt == UnitLogic.CampTypeInt
            || damagedUnitIndices.Contains(target.Index))
        {
            return;
        }

        Vector3 offset = target.CurPos - UnitLogic.CurPos;
        offset.y = 0f;
        float forwardDistance = Vector3.Dot(offset, castForward);
        Vector3 right = Vector3.Cross(Vector3.up, castForward).normalized;
        float sideDistance = Vector3.Dot(offset, right);
        if (forwardDistance < 0f || forwardDistance > rectLength
            || Mathf.Abs(sideDistance) > rectWidth * 0.5f)
        {
            return;
        }

        damagedUnitIndices.Add(target.Index);
        BattleLogicDamageTools.DoDamage(
            UnitLogic,
            target,
            GetFinalDamage(attackDamage, target),
            target.UId,
            SkillCfg.dieType,
            target.CurPos);
        ApplyBuffs(target);
    }

    private Vector3 GetCastForward()
    {
        Vector3 direction = UnitLogic.TargetForward;
        if (UnitLogic.SearchTarget != null && !UnitLogic.SearchTarget.IsDead)
        {
            direction = UnitLogic.SearchTarget.GetClosestPoint(UnitLogic.CurPos) - UnitLogic.CurPos;
        }

        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f && UnitLogic.UnitView != null)
        {
            direction = UnitLogic.UnitView.transform.forward;
            direction.y = 0f;
        }
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector3.forward;
        }

        return direction.normalized;
    }

    private bool TryParseRect(out float width, out float length)
    {
        width = 0f;
        length = 0f;
        string[] values = (SkillCfg != null ? SkillCfg.skillParam : string.Empty)
            .Split(new[] { ',', '|', ';', '/', ' ', '_' }, StringSplitOptions.RemoveEmptyEntries);
        if (values.Length != 2
            || !float.TryParse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out width)
            || !float.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out length)
            || width <= 0f || length <= 0f
            || float.IsNaN(width) || float.IsNaN(length)
            || float.IsInfinity(width) || float.IsInfinity(length))
        {
            if (!paramLogged)
            {
                Debug.LogError($"矩形技能 skillParam 配置无效，格式应为：宽度,前方长度，SkillId={SkillCfg?.id}");
                paramLogged = true;
            }
            return false;
        }

        return true;
    }
}
