using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class gfdgConfig
{
    private static gfdgConfig ins;
    public static gfdgConfig Ins
    {
        get
        {
            if(ins == null)
            {
                ins = new gfdgConfig();
                ins.init();
            }
            return ins;
        }
    }

    private List<gfdg> dataList = new List<gfdg>();
    public List<gfdg> ConfigDataList => dataList;

    private void init()
    {
        string json = File.ReadAllText(Application.streamingAssetsPath + "gfdg.json");
        dataList = JsonConvert.DeserializeObject<List<gfdg>>(json);
    }

    public gfdg SearchById(int id)
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

public class gfdg
{
	public int id;
	public string wocao;
	public string[] sigaoyi;
	public float[] dsd;
	public float ac;
	
}