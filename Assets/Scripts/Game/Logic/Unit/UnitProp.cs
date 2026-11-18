
using Unity.Mathematics;

public class UnitProp
{
    private float radius;
    public float Radius => radius;

    private float maxSpeed;
    public float MaxSpeed => maxSpeed;

    private int hp;
    public int Hp => hp;
    private int maxHp;
    public int MaxHp => maxHp;

    
    public UnitProp(int hp, float radius, float maxSpeed)
    {
        this.hp = hp;
        this.maxHp = hp;
        this.radius = radius;
        this.maxSpeed = maxSpeed;
    }

    /// <summary>重新设置最大血量，可选是否同步恢复当前血量。</summary>
    public void SetMaxHp(int value, bool restoreCurrentHp)
    {
        maxHp = math.max(0, value);
        if (restoreCurrentHp)
        {
            hp = maxHp;
        }
        else
        {
            hp = math.min(hp, maxHp);
        }
    }

    public void ChangeHp(int value)
    {
        hp += value;
        hp = math.max(0, hp);
    }

    public void RestoreMaxHpPercent(float percent)
    {
        int restoreHp = math.max(1, (int)math.ceil(maxHp * math.max(0f, percent)));
        hp = math.min(maxHp, math.max(hp, restoreHp));
    }
}
