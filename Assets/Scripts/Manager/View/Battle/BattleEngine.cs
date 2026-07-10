using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 战斗引擎
public class BattleEngine : MonoBehaviour
{
    // 场地碰撞配置
    public ObstacleConfig ObstacleConfigComp;

    private void Awake()
    {
        
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private bool battleStart()
    {
        if(ObstacleConfigComp == null)
        {
            Debug.LogError("没有边界障碍信息");
            return false;
        }
        RvoManager.Instance.ManagerInit();
        RvoManager.Instance.SetBorderInfo(ObstacleConfigComp.GetComponent<ObstacleConfig>().BorderList);

        return true;
    }
}
