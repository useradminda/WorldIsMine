using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SkillCfgConfig
{
    private static SkillCfgConfig ins;
    public static SkillCfgConfig Ins
    {
        get
        {
            if(ins == null)
            {
                ins = new SkillCfgConfig();
                ins.init();
            }
            return ins;
        }
    }

    private List<SkillCfg> dataList = new List<SkillCfg>();
    public List<SkillCfg> ConfigDataList => dataList;

    private void init()
    {
        string path = Path.Combine(
            Application.streamingAssetsPath,
            "JsonData",
            "SkillCfg.json");
        string json = File.ReadAllText(path);
        dataList = JsonConvert.DeserializeObject<List<SkillCfg>>(json);
    }

    public SkillCfg SearchById(int id)
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

public class SkillCfg
{
	public int id;
	public string name;
	public float searchRange;
	public float atkRange;
	public float skillArea;
	public int damage;
	public float cd;
	public int normal;
	public int skillType;
	public int flyObjectId;
	
}
