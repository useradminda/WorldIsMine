using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SoliderCfgConfig
{
    private static SoliderCfgConfig ins;
    public static SoliderCfgConfig Ins
    {
        get
        {
            if(ins == null)
            {
                ins = new SoliderCfgConfig();
                ins.init();
            }
            return ins;
        }
    }

    private List<SoliderCfg> dataList = new List<SoliderCfg>();
    public List<SoliderCfg> ConfigDataList => dataList;

    private void init()
    {
        string json = File.ReadAllText(Application.streamingAssetsPath + "SoliderCfg.json");
        dataList = JsonConvert.DeserializeObject<List<SoliderCfg>>(json);
    }

    public SoliderCfg SearchById(int id)
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

public class SoliderCfg
{
	public int id;
	public string name;
	public int[] skill;
	public int hp;
	public int atk;
	public float moveSpeed;
	public float radius;
	public string prefab;
	
}