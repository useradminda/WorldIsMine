using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class cfg_buffConfig
{
    private static cfg_buffConfig ins;
    public static cfg_buffConfig Ins
    {
        get
        {
            if(ins == null)
            {
                ins = new cfg_buffConfig();
                ins.init();
            }
            return ins;
        }
    }

    private List<cfg_buff> dataList = new List<cfg_buff>();
    public List<cfg_buff> ConfigDataList => dataList;

    private void init()
    {
        string path = Path.Combine(
            Application.streamingAssetsPath,
            "JsonData",
            "cfg_buff.json");
        string json = File.ReadAllText(path);
        dataList = JsonConvert.DeserializeObject<List<cfg_buff>>(json);
    }

    public cfg_buff SearchById(int id)
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

public class cfg_buff
{
	public int id;
	public string name;
	public string platform;
	public float cost;
	public string prefab;
	
}
