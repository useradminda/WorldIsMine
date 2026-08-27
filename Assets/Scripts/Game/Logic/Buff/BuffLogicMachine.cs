
using System.Collections.Generic;
using Unity.Collections;
public class BuffLogicMachine
{
    private List<BuffLogicBase> buffList = new List<BuffLogicBase>();

    public void AddBuff(BuffLogicBase buffLogic)
    {
        buffLogic.Enter();
        buffList.Add(buffLogic);
    }

    public void ExitBuff(BuffLogicBase buffLogic)
    {
        buffLogic.Exit();
        buffList.RemoveSwapBack(buffLogic);
    }

    public void UpdateBuffMachine(float dt)
    {
        for (int i = 0; i < buffList.Count; i++)
        {
            buffList[i].Update(dt);
        }
    }

    public void Die()
    {
        for (int i = 0; i < buffList.Count; i++)
        {
            buffList[i].Exit();
        }
        buffList.Clear();
    }
}
