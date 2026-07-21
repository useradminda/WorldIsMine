using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class IView : MonoBehaviour
{
    public abstract void ViewInit();

    public abstract void ViewUpdate();

    public abstract void ViewDestroy();

    public abstract void ViewRefuse();
}
