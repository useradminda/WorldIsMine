/// <summary>
/// 全局积分池显示规则，负责把积分转换成纯数字文本。
/// 该类只处理显示格式，不保存对局积分，也不参与积分增减逻辑。
/// </summary>
public static class BattleScoreRule
{
    /// <summary>将积分转换成亿、万分组显示的数字文本，不拆分千百十。</summary>
    public static string FormatScore(int score)
    {
        long value = score < 0 ? 0L : score;
        if (value < 10000L)
        {
            return value.ToString();
        }

        long yi = value / 100000000L;
        long wan = (value % 100000000L) / 10000L;
        long remainder = value % 10000L;
        string result = string.Empty;

        if (yi > 0L)
        {
            result += yi + "亿";
        }

        if (wan > 0L)
        {
            result += wan + "万";
        }

        if (remainder > 0L)
        {
            result += remainder.ToString("D4");
        }

        return result;
    }
}
