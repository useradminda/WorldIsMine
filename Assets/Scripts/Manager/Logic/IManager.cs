using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IManager
{
    void ManagerInit();
    void ManagerUpdate(float dt);
    void ManagerLateUpdate(float dt);
    void ManagerRefuse();
    void ManagerDestroy();
}
