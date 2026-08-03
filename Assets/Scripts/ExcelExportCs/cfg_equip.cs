using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class cfg_equipConfig
{
    private static cfg_equipConfig ins;
    public static cfg_equipConfig Ins
    {
        get
        {
            if(ins == null)
            {
                ins = new cfg_equipConfig();
                ins.init();
            }
            return ins;
        }
    }

    private List<cfg_equip> dataList = new List<cfg_equip>();
    public List<cfg_equip> ConfigDataList => dataList;

    private void init()
    {
        string path = Path.Combine(
            Application.streamingAssetsPath,
            "JsonData",
            "cfg_equip.json");
        string json = File.ReadAllText(path);
        dataList = JsonConvert.DeserializeObject<List<cfg_equip>>(json);
    }

    public cfg_equip SearchById(int id)
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

public class cfg_equip
{
	public int id;
	public string name;
	public string platform;
	public float cost;
	public string prefab;
	
}
