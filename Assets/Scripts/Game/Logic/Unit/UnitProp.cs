
public class UnitProp
{
    private float radius;
    public float Radius => radius;

    private float maxSpeed;
    public float MaxSpeed => maxSpeed;

    public UnitProp(float radius, float maxSpeed)
    {
        this.radius = radius;
        this.maxSpeed = maxSpeed;
    }
}
