
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

        buffLogic.Refresh(duration);
        buffLogic.Enter();
        buffList.Add(buffLogic);
        return true;
    }

    /// <summary>立即移除指定Buff。</summary>
    public void ExitBuff(BuffLogicBase buffLogic)
    {
        if (buffLogic == null || !buffList.Contains(buffLogic))
        {
            return;
        }

        buffLogic.Exit();
        buffList.Remove(buffLogic);
    }

    /// <summary>更新全部Buff，并安全移除到期Buff。</summary>
    public void UpdateBuffMachine(float dt)
    {
        for (int i = buffList.Count - 1; i >= 0; i--)
        {
            BuffLogicBase buffLogic = buffList[i];
            buffLogic.Update(dt);
            if (buffLogic.IsExpired)
            {
                buffLogic.Exit();
                buffList.RemoveAt(i);
            }
        }
    }

    /// <summary>单位死亡或被复用时清理全部Buff。</summary>
    public void Die()
    {
        for (int i = buffList.Count - 1; i >= 0; i--)
        {
            buffList[i].Exit();
        }
        buffList.Clear();
    }
}
