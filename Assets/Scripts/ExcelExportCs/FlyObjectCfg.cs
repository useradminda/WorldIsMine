using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FlyObjectCfgConfig
{
    private static FlyObjectCfgConfig ins;
    public static FlyObjectCfgConfig Ins
    {
        get
        {
            if(ins == null)
            {
                ins = new FlyObjectCfgConfig();
                ins.init();
            }
            return ins;
        }
    }

    private List<FlyObjectCfg> dataList = new List<FlyObjectCfg>();
    public List<FlyObjectCfg> ConfigDataList => dataList;

    private void init()
    {
        string json = File.ReadAllText(Application.streamingAssetsPath + "FlyObjectCfg.json");
        dataList = JsonConvert.DeserializeObject<List<FlyObjectCfg>>(json);
    }

    public FlyObjectCfg SearchById(int id)
    {
        for(int i = 0; i < ConfigDataList.Count; i++)
        {
            if (ConfigDataList[i].id == id)
            {
                return ConfigDataList[i];
            }
        }
        return null;
    }
}

public class FlyObjectCfg
{
	public int id;
	public string name;
	public float speed;
	public int flyType;
	
}