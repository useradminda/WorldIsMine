using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// 矩形范围瞬发技能类型。
/// 适用于长条形斩击、扇形替代判定和前方区域攻击；释放时立即判定一次，不创建飞行器。
/// skillParam 格式：矩形前方长度,矩形宽度。
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

    /// <summary>
    /// 执行矩形技能的范围判定和伤害结算。
    /// </summary>
    public override void OnSkillDoEffect(UnitLogicBase target, List<int> resultUnitIndexList)
    {
        ExecuteRectEffect();
    }

    private void ExecuteRectEffect()
    {
        if (!TryParseRect(out rectLength, out rectWidth))
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

    /// <summary>
    /// 解析矩形技能参数，参数顺序为前方长度、横向宽度。
    /// </summary>
    private bool TryParseRect(out float length, out float width)
    {
        length = 0f;
        width = 0f;
        string[] values = (SkillCfg != null ? SkillCfg.skillParam : string.Empty)
            .Split(new[] { ',', '|', ';', '/', ' ', '_' }, StringSplitOptions.RemoveEmptyEntries);
        if (values.Length != 2
            || !float.TryParse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out length)
            || !float.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out width)
            || width <= 0f || length <= 0f
            || float.IsNaN(width) || float.IsNaN(length)
            || float.IsInfinity(width) || float.IsInfinity(length))
        {
            if (!paramLogged)
            {
                Debug.LogError($"矩形技能 skillParam 配置无效，格式应为：前方长度,宽度，SkillId={SkillCfg?.id}");
                paramLogged = true;
            }
            return false;
        }

        return true;
    }
}
