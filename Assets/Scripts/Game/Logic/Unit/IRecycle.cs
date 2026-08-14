public abstract class IRecycle
{
    protected bool isFree = true;
    public bool IsFree => isFree;

    public abstract void Recycle();
}
