using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DieBaseComponent : MonoBehaviour
{
    protected UnitView mUnitView;

    public void SetUnitView(UnitView unitView)
    {
        mUnitView = unitView;
        EnterDieState();
    }

    protected virtual void EnterDieState()
    {

    }

    // Update is called once per frame
    void Update()
    {
        UpdateDie(Time.deltaTime);   
    }

    protected virtual void UpdateDie(float dt)
    {

    }

    protected void CycleUnitView()
    {
        UnitViewFactory.RemoveUnitView(mUnitView);
        DieBaseComponent dieComp = GetComponent<DieBaseComponent>();
        GameObject.Destroy(dieComp);
    }
}
