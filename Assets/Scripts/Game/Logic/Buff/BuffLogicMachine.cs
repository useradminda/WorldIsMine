
using System.Collections.Generic;
public class BuffLogicMachine
{
    private List<BuffLogicBase> buffList = new List<BuffLogicBase>();

    /// <summary>添加Buff；同配置Buff已存在时只刷新持续时间。</summary>
    public bool AddBuff(BuffLogicBase buffLogic, float duration = -1f)
    {
        if (buffLogic == null)
        {
            return false;
        }

        for (int i = 0; i < buffList.Count; i++)
        {
            if (buffList[i].CfgId == buffLogic.CfgId)
            {
                buffList[i].Refresh(duration);
                return true;
            }
        }

        buffList.Add(buffLogic);
        buffLogic.Refresh(duration);
        buffLogic.Enter();
        return true;
    }

    /// <summary>立即移除指定Buff。</summary>
    public void ExitBuff(BuffLogicBase buffLogic)
    {
        if (buffLogic == null || !buffList.Contains(buffLogic))
        {
            return;
        }

        buffList.Remove(buffLogic);
        buffLogic.Exit();
    }

    /// <summary>更新全部Buff，并安全移除到期Buff。</summary>
    public void UpdateBuffMachine(float dt)
    {
        for (int i = buffList.Count - 1; i >= 0; i--)
        {
            BuffLogicBase buffLogic = buffList[i];
            buffLogic.Update(dt);
            if (buffLogic.IsTimeFlow && buffLogic.RemainTIme <= 0)
            {
                buffList.RemoveAt(i);
                buffLogic.Exit();
            }
        }
    }

    public bool TryPreventDeath()
    {
        for (int i = buffList.Count - 1; i >= 0; i--)
        {
            BuffLogicBase buffLogic = buffList[i];
            if (!buffLogic.TryPreventDeath())
            {
                continue;
            }

            buffList.RemoveAt(i);
            buffLogic.Exit();
            return true;
        }

        return false;
    }

    /// <summary>让所有Buff在伤害扣除前依次处理伤害。</summary>
    public void ModifyDamage(ref int damage)
    {
        for (int i = buffList.Count - 1; i >= 0; i--)
        {
            buffList[i].ModifyDamage(ref damage);
        }
    }

    /// <summary>让所有Buff记录一次受击，并汇总本次需要反击的伤害。</summary>
    public int RecordDamage(int damage, bool isAttack)
    {
        int counterDamage = 0;
        for (int i = buffList.Count - 1; i >= 0; i--)
        {
            int currentCounterDamage = buffList[i].RecordDamage(damage, isAttack);
            if (currentCounterDamage <= 0)
            {
                continue;
            }

            if (int.MaxValue - counterDamage < currentCounterDamage)
            {
                counterDamage = int.MaxValue;
            }
            else
            {
                counterDamage += currentCounterDamage;
            }
        }

        return counterDamage;
    }

    /// <summary>单位死亡或被复用时清理全部Buff。</summary>
    public void Die()
    {
        for (int i = buffList.Count - 1; i >= 0; i--)
        {
            BuffLogicBase buffLogic = buffList[i];
            buffList.RemoveAt(i);
            buffLogic.Exit();
        }
        buffList.Clear();
    }

}
