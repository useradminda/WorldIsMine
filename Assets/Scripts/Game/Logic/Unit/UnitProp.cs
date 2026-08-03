
public class UnitProp
{
    private float radius;
    public float Radius => radius;

    private float maxSpeed;
    public float MaxSpeed => maxSpeed;

    private int hp;
    public int Hp => hp;


    public UnitProp(int hp, float radius, float maxSpeed)
    {
        this.hp = hp;
        this.radius = radius;
        this.maxSpeed = maxSpeed;
    }

    public void ChangeHp(int value)
    {
        hp += value;
    }
}
