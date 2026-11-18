/// <summary>
/// 玩家当前队形信息。
/// 当前规则为红方位于左侧、蓝方位于右侧，后续可在此对象中继续扩展队列和站位配置。
/// </summary>
public sealed class BattleFormationInfo
{
    /// <summary>队形所属阵营。</summary>
    public ECampType CampType { get; private set; }

    /// <summary>是否位于战场左侧。</summary>
    public bool IsLeftSide => CampType == ECampType.Red;

    /// <summary>是否位于战场右侧。</summary>
    public bool IsRightSide => CampType == ECampType.Blue;

    /// <summary>创建当前阵营的默认队形信息。</summary>
    public BattleFormationInfo(ECampType campType)
    {
        CampType = campType;
    }

    /// <summary>更新队形所属阵营。</summary>
    public void SetCampType(ECampType campType)
    {
        CampType = campType;
    }
}
