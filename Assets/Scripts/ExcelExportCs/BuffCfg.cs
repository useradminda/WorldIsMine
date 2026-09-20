using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BuffCfgConfig
{
    private static BuffCfgConfig ins;
    public static BuffCfgConfig Ins
    {
        get
        {
            if(ins == null)
            {
                ins = new BuffCfgConfig();
                ins.init();
            }
            return ins;
        }
    }

    private List<BuffCfg> dataList = new List<BuffCfg>();
    public List<BuffCfg> ConfigDataList => dataList;

    private void init()
    {
        string path = Path.Combine(
            Application.streamingAssetsPath,
            "JsonData",
            "BuffCfg.json");
        string json = File.ReadAllText(path);
        dataList = JsonConvert.DeserializeObject<List<BuffCfg>>(json);
    }

    public BuffCfg SearchById(int id)
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

public class BuffCfg
{
	public int id;
	public string name;
	public string des;
	public int tyep;
	public float time;
	public int value;
	
}
