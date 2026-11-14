/// <summary>
/// 普通近战技能类型。
/// 适用于近距离单目标攻击，由基类完成伤害、范围追加伤害和 Buff 处理。
/// </summary>
public class SkillCloseSkill : SkillLogicBase
{
    public SkillCloseSkill(UnitLogicBase ulb, SkillCfg skillCfg) : base(ulb, skillCfg)
    {
    }
}
