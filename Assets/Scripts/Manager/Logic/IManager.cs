using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IManager
{
    void ManagerInit();
    void ManagerUpdate();
    void ManagerLateUpdate();
    void ManagerRefuse();
    void ManagerDestroy();
}
