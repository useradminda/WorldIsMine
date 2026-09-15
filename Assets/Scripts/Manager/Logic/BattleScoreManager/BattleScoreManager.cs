using System;
using ZTools;
/// <summary>直播弹幕对局的全局胜点与积分。积分用于排行，胜点用于释放全局技能。</summary>
public class BattleScoreManager : Singleton<BattleScoreManager>
{
    public int Score { get; private set; }
    public int VictoryPoint { get; private set; }
    public event Action<int, int> Changed;

    /// <summary>构造全局战绩管理器。</summary>
    public BattleScoreManager()
    {
    }

    /// <summary>增加积分。</summary>
    public void AddScore(int value)
    {
        ChangeScore(value);
    }

    /// <summary>扣除积分，积分不会低于零。</summary>
    public void RemoveScore(int value)
    {
        ChangeScore(-Math.Abs(value));
    }

    /// <summary>增加胜点。</summary>
    public void AddVictoryPoint(int value)
    {
        ChangeVictoryPoint(value);
    }

    /// <summary>扣除胜点，胜点不会低于零。</summary>
    public void RemoveVictoryPoint(int value)
    {
        ChangeVictoryPoint(-Math.Abs(value));
    }

    /// <summary>根据有效伤害增加积分。</summary>
    public void AddDamageReward(int damage, int scorePerDamage = 1)
    {
        if (damage > 0)
        {
            AddScore(damage * scorePerDamage);
        }
    }

    /// <summary>处理击杀奖励。</summary>
    public void AddKillReward(int victoryPoint, int score)
    {
        AddVictoryPoint(victoryPoint);
        AddScore(score);
    }

    /// <summary>处理关键目标奖励。</summary>
    public void AddObjectiveReward(int victoryPoint, int score)
    {
        AddVictoryPoint(victoryPoint);
        AddScore(score);
    }

    /// <summary>尝试消耗胜点释放全局技能。</summary>
    public bool TryCastGlobalSkill(int cost, Action skill)
    {
        if (cost < 0 || VictoryPoint < cost || skill == null)
        {
            return false;
        }
        VictoryPoint -= cost;
        skill.Invoke();
        Changed?.Invoke(Score, VictoryPoint);
        return true;
    }

    /// <summary>重置本场对局的积分和胜点。</summary>
    public void Reset()
    {
        Score = 0;
        VictoryPoint = 0;
        NotifyChanged();
    }

    private void ChangeScore(int value)
    {
        Score = Math.Max(0, Score + value);
        NotifyChanged();
    }

    private void ChangeVictoryPoint(int value)
    {
        VictoryPoint = Math.Max(0, VictoryPoint + value);
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        if (Changed != null)
        {
            Changed(Score, VictoryPoint);
        }
    }
}
