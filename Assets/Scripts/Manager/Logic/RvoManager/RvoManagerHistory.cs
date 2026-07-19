
//using AillieoUtils;
using AillieoUtils;
using Nebukam.Common;
using Nebukam.ORCA;
using UnityEngine;
//#if UNITY_EDITOR
//using Nebukam.Common.Editor;
//#endif
//using System.Collections.Generic;
//using Unity.Mathematics;
//using UnityEngine;

//using static Unity.Mathematics.math;
//using Random = UnityEngine.Random;
////using System.Collections.Generic;
//using UVector2 = UnityEngine.Vector2;
//using AVector2 = AillieoUtils.Vector2;
//using System.Collections;
//using System.Reflection.Emit;
//using UnityEngine.UIElements;



//// 个体单位
//public class UnitInfo
//{
//    public Nebukam.ORCA.Agent Agenter;
//    public MonsterBaseComponent MonsterCompoent;
//    private float radius;
//    public float Radius => radius;

//    private EFriendType friendType = EFriendType.None;
//    public EFriendType FriendType => friendType;

//    public UnitInfo(Nebukam.ORCA.Agent _agent, MonsterBaseComponent _monsterComponent)
//    {
//        MonsterCompoent = _monsterComponent;
//        if (_agent != null)
//        {
//            radius = _agent.radius;          
//            Agenter = _agent;
//        }
//    }

//    public void SetFrindType(EFriendType _friendType)
//    {
//        friendType = _friendType;
//    }

//    // 判断是否死亡
//    public bool UnitBDead()
//    {
//        return MonsterCompoent.MonsterBDead();
//    }
//}

//namespace Nebukam.ORCA
//{
//    public class RvoKDState : IPositionProvider
//    {
//        public AVector2 pos;
//        public Color color;
//        public int id = 0;
//        public int lastUpdateFrame = -1;
//        public Agent agent;

//        public AVector2 position
//        {
//            get
//            {
//               return new AVector2(agent.transform.position.x, agent.transform.position.z); //=> pos;// new AVector2(agent.pos.x, agent.pos.z);
//            }
//        }
//        public float agentRadius => agent.radius;

//        // 会在获取缓存的时候更新（在initKdTree的时候) 好像不准确
//        //public MonsterBaseComponent MonsterCompoent;


//        public void SetSelected(bool selected)
//        {
//            this.color = selected ? Color.blue : Color.white;
//        }
//    }
public class RvoManagerHistory : MonoBehaviour
{
    private void Awake()
    {
        
    }
}

//    public class RvoManager : MonoBehaviour
//    {

//        public static RvoManager RvoManagerIns;

//        public AgentGroup<Agent> agents;
//        private ObstacleGroup obstacles;
//        //private ObstacleGroup dynObstacles;
//       // private RaycastGroup raycasts;
//        private ORCA simulation;


//        public AxisPair axis = AxisPair.XY;

//        [Header("Debug")]
//        Color staticObstacleColor = Color.red;

//        [System.NonSerialized]
//        public GameObject ObstacleManagerGob;

//        private List<ObstacleComponent> staticObstacleList = new List<ObstacleComponent>();

//        private KDTree<RvoKDState> tree;
//        private HashSet<RvoKDState> queryResult = new HashSet<RvoKDState>();

//        // 初始士兵数量
//        public Dictionary<EFriendType, int> OriginalSoldierDic = new Dictionary<EFriendType, int>();
//        // 战损数量
//        public Dictionary<EFriendType, int> HurtSoldierDic = new Dictionary<EFriendType, int>();


//        public Dictionary<Agent, UnitInfo> enemyDic = new Dictionary<Agent, UnitInfo>();
//        public Dictionary<Agent, UnitInfo> friendDic = new Dictionary<Agent, UnitInfo>();
//        [System.NonSerialized]
//        public List<MonsterBaseComponent> netualList = new List<MonsterBaseComponent>();
//        public UnitInfo FriendDoor;


//        public bool GameStop = true;

//        private Transform friendTargetPoint;
//        private Transform enemyTargetPoint;

//        protected virtual void Awake()
//        {

//            RvoManagerIns = this;
//            friendTargetPoint = transform.Find("FriendTargetPoint");
//            enemyTargetPoint = transform.Find("EnemyTargetPoint");
//            InitBattleInfo();
//        }

//        public Vector3 GetFriendTargetPoint()
//        {
//            if (friendTargetPoint == null)
//                return Vector3.zero;
//            return friendTargetPoint.position;
//        }

//        public Vector3 GetEnemyTargetPoint()
//        {
//            if (enemyTargetPoint == null)
//                return Vector3.zero;
//            return enemyTargetPoint.position;
//        }

//        protected virtual void Start()
//        {

//        }

//        // 初始化一些战斗需要的物件
//        public void InitBattleInfo()
//        {
//            CyclePool<GameObject>.Clear();
//            agents = new AgentGroup<Agent>();

//            obstacles = new ObstacleGroup();

//            simulation = new ORCA();
//            simulation.plane = axis;
//            simulation.agents = agents;
//            simulation.staticObstacles = obstacles;

//            setStaticObstacles();
//            createBorderObstacles();

//            createKDTree();
//        }

//        // 开始战斗
//        public void StartBattle()
//        {
//            GameStop = false;
//        }


//        // 设置静态障碍物
//        private void setStaticObstacles()
//        {
//            if(ObstacleManagerGob == null)
//            {
//                ObstacleManagerGob = GameObject.Find("ObstacleManager");
//            }

//            for (int i = 0; i < ObstacleManagerGob.transform.childCount; i++)
//            {
//                Transform _obstacleTrans = ObstacleManagerGob.transform.GetChild(i);
//                if (_obstacleTrans.name == "BorderObstacle")
//                {
//                    continue;
//                }
//                staticObstacleList.Add(_obstacleTrans.GetComponent<ObstacleComponent>());
//            }
//            List<float3> _vList = new List<float3>();
//            for (int i = 0; i < staticObstacleList.Count; i++)
//            {
//                int _count = staticObstacleList[i].V3List.Count;
//                _vList.Clear();
//                _vList.Capacity = _count;
//                for (int j = 0; j < _count; j++)
//                {
//                    _vList.Add(staticObstacleList[i].V3List[j]);
//                }
//                obstacles.Add(_vList, axis == AxisPair.XZ);
//            }
//        }

//        // 设置边界障碍
//        private void createBorderObstacles()
//        {
//            ObstacleComponent borderObstacle = ObstacleManagerGob.transform.Find("BorderObstacle").GetComponent<ObstacleComponent>();

//            float3[] squarePoints = new float3[] {
//                float3(borderObstacle.V3List[0].x, 0, borderObstacle.V3List[0].z),
//                float3(borderObstacle.V3List[1].x, 0, borderObstacle.V3List[1].z),
//                float3(borderObstacle.V3List[2].x, 0, borderObstacle.V3List[2].z),
//                float3(borderObstacle.V3List[3].x, 0, borderObstacle.V3List[3].z),
//            };

//            obstacles.Add(squarePoints, false, 10.0f);
//        }

//        private bool dynamicState;
//        public void AddDynamicObstacle()
//        {
//            dynamicState = true;
//            List<float3> _vList = new List<float3>();
//            _vList.Add(new Vector3(-40, 0, 80));
//            _vList.Add(new Vector3(40, 0, 80));
//            _vList.Add(new Vector3(40, 0, 90));
//            _vList.Add(new Vector3(-40, 0, 90));
//            obstacles.Add(_vList);
//        }

//        public Obstacle AddDynamicObstacle(List<Vector3> _dynamicObjectBorderInfo)
//        {
//            Obstacle _obstacle;
//            List<float3> _vList = new List<float3>();
//            for (int i = 0; i < _dynamicObjectBorderInfo.Count; i++)
//            {
//                _vList.Add(_dynamicObjectBorderInfo[i]);
//            }
//            _obstacle = obstacles.Add(_vList);
//            return _obstacle;
//        }

//        public void DelDynamicObstacle()
//        {
//            if (dynamicState == true)
//            {
//                obstacles.Remove(obstacles[obstacles.Count - 1]);
//                return;
//            }
//        }
//        public void DelDynamicObstacle(Obstacle _obstacle)
//        {           
//            obstacles.Remove(_obstacle);
//        }


//        // 创建友军
//        public virtual void CreateFriend(SoldierInfo _soldierInfo, GameObject _prefab, Vector3 _bornPoint, EFriendType _eFriendType, EUnitType _unitType, Vector3? _forward = null, Vector3? _targetPoint = null)
//        {
//            SoldierSettings _soldierSettings = SoldierSettingsManager.GameSettingsManagerIns.GetSoldierTable(_eFriendType);
//            if(_soldierSettings == null)
//            {
//                Debug.LogError(_eFriendType.ToString() + "配置为空");
//                return;
//            }
//            Prop _prop = new Prop(_soldierInfo, _eFriendType, _soldierSettings);
//            float _moveSpeed = _soldierSettings.MoveSpeed;
//            if (BattleDataProxy.Ins.BattleType == EBattleType.Normal || BattleDataProxy.Ins.BattleType == EBattleType.Boss)
//            {
//                if(_eFriendType == EFriendType.ArcherMan || _eFriendType == EFriendType.MagicMan || _eFriendType == EFriendType.TreatMan || _eFriendType == EFriendType.StoneMachine)
//                {
//                    _moveSpeed = 0;
//                }
//                if (BattleDataProxy.Ins.BattleType == EBattleType.Normal || BattleDataProxy.Ins.BattleType == EBattleType.Boss)
//                {
//                    if (_eFriendType == EFriendType.ArcherMan || _eFriendType == EFriendType.TreatMan)
//                    {
//                        _prop.SetAttackDisForce(150);
//                        _prop.SetSearchRange(150);
//                    }
//                }
//            }

//            UnitInfo _unitInfo = CreateUnitAtPoint(_bornPoint, _soldierSettings.BodyRadius, _moveSpeed, _prefab, _unitType, _prefab.name, _prop, _forward);
//            if (_eFriendType == EFriendType.FriendDoor)
//                FriendDoor = _unitInfo;
//            addOriginalSoldier(_eFriendType);
//            _unitInfo.SetFrindType(_eFriendType);
//        }

//        // 创建敌军
//        public virtual UnitInfo CreateEnemy(Vector3 _bornPoint, EEnemyType _eEnemyType, int _monsterBody, Vector3? _forward = null)
//        {
//            MonsterSettings _monsterSettings = MonsterSettingsManager.MonsterSettingsManagerIns.GetMonsterTable(_eEnemyType);
//            if (_monsterSettings == null)
//            {
//                Debug.LogError(_eEnemyType.ToString() + "配置为空");
//                return null;
//            }
//            GameObject _prefab = _monsterSettings.Prefab;
//            Prop _prop = new Prop(_monsterSettings, _monsterBody);
//            UnitInfo _unitInfo = CreateUnitAtPoint(_bornPoint, _monsterSettings.BodyRadius, _monsterSettings.MoveSpeed, _prefab, EUnitType.Enemy, _prefab.name, _prop, _forward);
//            return _unitInfo;
//        }

//        // 创建敌人Boss
//        public virtual void CreateEenemyBoss(Vector3 _bornPoint, BossInfo _bossInfo)
//        {
//            MonsterSettings _monsterSettings = MonsterSettingsManager.MonsterSettingsManagerIns.GetMonsterTable(_bossInfo.EnemyType);
//            if (_monsterSettings == null)
//            {
//                Debug.LogError(_bossInfo.EnemyType.ToString() + "配置为空");
//                return;
//            }
//            GameObject _prefab = _monsterSettings.Prefab;
//            Prop _prop = new Prop(_bossInfo);
//            UnitInfo _unitInfo = CreateUnitAtPoint(_bornPoint, _monsterSettings.BodyRadius, _monsterSettings.MoveSpeed, _prefab, EUnitType.Enemy, _prefab.name, _prop);
//        }

//        // 拷贝当前角色士兵属性到敌人身上
//        public void CopyToBossEnemyHumanArmy(Vector3 _bornPoint, SoldierInfo _soldierInfo, EEnemyType _eEnemyType)
//        {
//            MonsterSettings _monsterSettings = MonsterSettingsManager.MonsterSettingsManagerIns.GetMonsterTable(_eEnemyType);
//            if (_monsterSettings == null)
//            {
//                Debug.LogError(_eEnemyType.ToString() + "配置为空");
//                return;
//            }
//            GameObject _prefab = _monsterSettings.Prefab;
//            Prop _prop = new Prop(_soldierInfo, _monsterSettings);
//            UnitInfo _unitInfo = CreateUnitAtPoint(_bornPoint, _monsterSettings.BodyRadius, _monsterSettings.MoveSpeed, _prefab, EUnitType.Enemy, _prefab.name, _prop);
//        }

//        public UnitInfo CreateUnitAtPoint(Vector3 _createPoint, float _radius, float _moveSpeed, GameObject _prefab, EUnitType _eUnitType, string _name, Prop _prop = null, Vector3? _forward = null)
//        {
//            IAgent a;

//            a = agents.Add((float3)_createPoint + float3(0.01f, 0f, 0.01f)) as IAgent;

//            a.transform = _prefab.GetCatchEntityObject().transform; //GameObject.Instantiate(_prefab).transform;

//            a.transform.position = _createPoint;
//            a.transform.name = _name;

//            a.saveMaxSpeed = _moveSpeed;
//            a.radius = _radius;
//            a.radiusObst = a.radius;
//            if (_forward != null)
//            {
//                a.prefVelocity = (Vector3)_forward.Value;
//                a.velocity = (Vector3)_forward.Value;
//            }
//            else
//            {
//                a.prefVelocity = float3(0f);
//            }

//            UnitInfo _unitInfo = null;

//            if (_eUnitType == EUnitType.Friend || _eUnitType == EUnitType.ThirdParty)
//            {               
//                a.transform.forward = Vector3.forward;
//                if (_forward != null)
//                {
//                    a.transform.forward = (Vector3)_forward;
//                }
//                _unitInfo = createUnitInfo((Agent)a, a.transform, _eUnitType, _prefab);
//                friendDic.Add((Agent)a, _unitInfo);
//                _unitInfo.MonsterCompoent.SetProp(_prop);
//            }
//            else
//            {
//                a.transform.forward = -Vector3.forward;
//                if(_forward != null)
//                {
//                    a.transform.forward = (float3)_forward;
//                }
//                _unitInfo = createUnitInfo((Agent)a, a.transform, _eUnitType, _prefab);
//                enemyDic.Add((Agent)a, _unitInfo);
//                _unitInfo.MonsterCompoent.SetProp(_prop);
//            }
//            return _unitInfo;
//        }

//        private UnitInfo createUnitInfo(Agent _agent, Transform _transform, EUnitType _unitType, GameObject _originalPrefab)
//        {
//            MonsterBaseComponent mbComp = _transform.GetComponent<MonsterBaseComponent>();
//            if (mbComp == null)
//                Debug.LogError("严重错误 缺少monsterBaseComponent");

//            UnitInfo _unitInfo = new UnitInfo(_agent, mbComp);
//            Vector3 _targetPoint = _unitType == EUnitType.Enemy ? enemyTargetPoint.position : friendTargetPoint.position;
//            mbComp.SetMonterInfo(_unitInfo, _unitType, _targetPoint, _originalPrefab);

//            return _unitInfo;
//        }

//        // 删除某个友方类型的所有战斗角色信息
//        public void DeleteFriendDataByFriendType(EFriendType _friendType)
//        {
//            List<Agent> _agents = new List<Agent>();
//            foreach(var info in friendDic)
//            {
//                UnitInfo _unitInfo = info.Value;
//                if(_unitInfo.FriendType == _friendType)
//                {
//                    _unitInfo.MonsterCompoent.gameObject.SetCycleComponentCurrently();
//                    _agents.Add(_unitInfo.Agenter);
//                    agents.Remove(_unitInfo.Agenter);
//                }
//            }
//            for(int i = 0; i < _agents.Count; i++)
//            {
//                friendDic.Remove(_agents[i]);
//            }
//        }

//        public void RemoveAgentByMonsterBaseComp(MonsterBaseComponent _mbc)
//        {
//            List<Agent> _agents = new List<Agent>();
//            foreach (var info in friendDic)
//            {
//                UnitInfo _unitInfo = info.Value;
//                if (_unitInfo.MonsterCompoent == _mbc)
//                {
//                    _agents.Add(_unitInfo.Agenter);
//                    agents.Remove(_unitInfo.Agenter);
//                }
//            }
//            for (int i = 0; i < _agents.Count; i++)
//            {
//                friendDic.Remove(_agents[i]);
//            }
//        }     


//        #region 创建战场可击破物件
//        public void CreateTreasure(Vector3 _createPoint, GameObject _prefab)
//        {
//            Prop _prop = new Prop();
//            Transform _coinTrans = _prefab.GetCatchEntityObject().transform; //GameObject.Instantiate(_prefab).transform;
//            _coinTrans.gameObject.layer = 28;
//            _coinTrans.position = _createPoint;
//            MonsterBaseComponent mbComp = _coinTrans.GetComponent<MonsterBaseComponent>();
//            Agent _agent = agents.Add(_createPoint) as Agent;
//            _agent.navigationEnabled = false;
//            _agent.radius = 0;
//            _agent.radiusObst = _agent.radius;
//            _agent.transform = _coinTrans;
//            _agent.transform.name = "Treasure";

//            UnitInfo _unitInfo = new UnitInfo(_agent, mbComp);
//            Vector3 _targetPoint = Vector3.zero;
//            mbComp.SetMonterInfo(_unitInfo, EUnitType.ForthParty, _targetPoint, _prefab);
//            mbComp.SetProp(_prop);
//            netualList.Add(mbComp);
//            initKDTree();
//        }

//        // 创建建造废墟
//        public void CreateRuins(string _ruinsName, Vector3 _bornPoint, Vector3 _forward)
//        {
//            RuinsSettings _ruinsSettings = RuinsSettingsManager.Instance.SearchRuinsSettingsByName(_ruinsName);
//            if (_ruinsSettings == null)
//            {
//                Debug.LogError(_ruinsName + "配置为空");
//                return;
//            }
//            GameObject _prefab = _ruinsSettings.RuinsPrefab;
//            RuinsProp _prop = new RuinsProp(_ruinsSettings);
//            GameObject _ruinsGameObject = _prefab.GetCatchEntityObject();
//            _ruinsGameObject.layer = 25;
//            _ruinsGameObject.GetCompoentByGob<MonsterBaseComponent>().SetMonterInfo(null, EUnitType.Friend, Vector3.zero, _prefab);
//            _ruinsGameObject.GetCompoentByGob<RuinsComponent>().SetProp(_prop);
//            _ruinsGameObject.transform.position = _bornPoint;
//            _ruinsGameObject.transform.forward = _forward;
//        }

//        #endregion

//        #region 创建陷阱
//        public UnitInfo CreateTrap(Vector3 _bornPoint, string _trapName, Vector3 _forward)
//        {
//            BuildingSettings _buildingSettings = BuildingSettingsManager.Ins.SearchBuildingSettingsByName(_trapName);
//            if (_buildingSettings == null)
//            {
//                Debug.LogError(_trapName + "配置为空");
//                return null;
//            }
//            GameObject _prefab = _buildingSettings.BuildingPrefabFriend;
//            TrapProp _prop = new TrapProp(_buildingSettings);
//            GameObject _trapGameObject = _prefab.GetCatchEntityObject();
//            _trapGameObject.transform.position = _bornPoint;
//            _trapGameObject.transform.forward = _forward;

//            MonsterBaseComponent mbComp = _trapGameObject.GetComponent<MonsterBaseComponent>();
//            UnitInfo _unitInfo = new UnitInfo(null, mbComp);
//            mbComp.SetMonterInfo(_unitInfo, EUnitType.Friend, Vector3.zero, _prefab);
//            mbComp.SetProp(_prop);
//            netualList.Add(mbComp);
//            return _unitInfo;
//        }

//        #endregion

//        #region
//        // 创建我方建筑
//        public virtual UnitInfo CreateFriendBuild(Vector3 _bornPoint, string _buildingName, Vector3 _forward)
//        {
//            BuildingSettings _buildingSettings = BuildingSettingsManager.Ins.SearchBuildingSettingsByName(_buildingName);
//            if (_buildingSettings == null)
//            {
//                Debug.LogError(_buildingName + "配置为空");
//                return null;
//            }

//            GameObject _prefab = _buildingSettings.BuildingPrefabFriend;
//            float _buildPropRatio = 1;
//            if (BattleDataProxy.Ins.BattleType == EBattleType.AttackCity || BattleDataProxy.Ins.BattleType == EBattleType.FarmLandCity)
//            {
//                _buildPropRatio = AttackCitySettingsManager.Ins.GetAttackCityBuildPropRatio(AttackCitySettingsManager.Ins.GetAttackCitySetting(BattleDataProxy.Ins.AttackCityName), BattleDataProxy.Ins.AttackCityLvl);
//            }
//            Prop _prop = new BuildingProp(_buildingSettings, _buildPropRatio);
//            UnitInfo _unitInfo = CreateUnitAtPoint(_bornPoint, _buildingSettings.BuildingRadius, 0, _prefab, EUnitType.Friend, _prefab.name, _prop, _forward);
//            _unitInfo.MonsterCompoent.gameObject.layer = 25;
//            return _unitInfo;
//        }
//        #endregion


//        //// 重新计算士兵属性
//        //public void ReCalculateSoldierPro()
//        //{
//        //    foreach(var so in friendDic)
//        //    {
//        //        so.Value.MonsterCompoent.MonsterProp.ReCalculateProp();
//        //    }
//        //}

//        private void addOriginalSoldier(EFriendType _friendType)
//        {
//            if (_friendType == EFriendType.None)
//                return;
//            if (_friendType != EFriendType.BoomBucket && _friendType != EFriendType.ArrowTower && _friendType != EFriendType.FriendDoor && _friendType != EFriendType.ProtectWoodDoor)
//            {
//                if (!OriginalSoldierDic.ContainsKey(_friendType))
//                {
//                    OriginalSoldierDic.Add(_friendType, 1);
//                }
//                else
//                {
//                    ++OriginalSoldierDic[_friendType];
//                }
//            }
//        }

//        private void hurtSoldier(EFriendType _friendType)
//        {
//            if (_friendType == EFriendType.None)
//                return;
//            if (_friendType != EFriendType.BoomBucket && _friendType != EFriendType.ArrowTower && _friendType != EFriendType.FriendDoor && _friendType != EFriendType.ProtectWoodDoor)
//            {
//                if (!HurtSoldierDic.ContainsKey(_friendType))
//                {
//                    HurtSoldierDic.Add(_friendType, 1);
//                }
//                else
//                {
//                    ++HurtSoldierDic[_friendType];
//                }
//                if(FarmLandCityRvoManager.FarmLandCityRvoManagerIns != null)
//                {
//                    FarmLandCityRvoManager.FarmLandCityRvoManagerIns.MinusCurrentSoldierNumber(_friendType);
//                }
//            }
//        }

//        #region AiSearch (Ai搜索)
//        // 获取敌对阵营最近的单位(无视搜索范围)
//        public UnitInfo GetNearestOppositeUnit(Vector3 _currentPoint, EUnitType _eUnitType)
//        {
//            float _minSqrDis = 0;
//            UnitInfo _unitInfoer = null;
//            Dictionary<Agent, UnitInfo> _searchDic = _eUnitType == EUnitType.Friend ? enemyDic : friendDic;
//            foreach (var item in _searchDic)
//            {
//                if(item.Value.UnitBDead() == false)
//                {
//                    if (_minSqrDis == 0)
//                    {
//                        _minSqrDis = (_currentPoint - item.Value.MonsterCompoent.transform.position).sqrMagnitude;
//                        _unitInfoer = item.Value;
//                    }
//                    else
//                    {
//                        float _sqr = (_currentPoint - item.Value.MonsterCompoent.transform.position).sqrMagnitude;
//                        if (_sqr < _minSqrDis)
//                        {
//                            _unitInfoer = item.Value;
//                            _minSqrDis = _sqr;
//                        }
//                    }
//                }
//            }
//            return _unitInfoer;
//        }

//        // 获取血量最少得友军
//        public UnitInfo GetLowestHpOurSide(EUnitType _eUnitType)
//        {
//            long _minHp = 0;
//            UnitInfo _unitInfoer = null;
//            Dictionary<Agent, UnitInfo> _searchDic = _eUnitType == EUnitType.Friend ? friendDic : enemyDic;
//            foreach (var item in _searchDic)
//            {
//                if(item.Value.MonsterCompoent.MonsterBDead() == false && item.Value.MonsterCompoent.BeFullHp() == false 
//                    && BFriendBuilds(item.Value.FriendType) == false && item.Value.MonsterCompoent.IsBuildingMonster() == false)
//                {
//                    if(_minHp == 0)
//                    {
//                        _unitInfoer = item.Value;
//                        _minHp = item.Value.MonsterCompoent.MonsterProp.GetCurrentHp();
//                    }
//                    else
//                    {
//                        if(item.Value.MonsterCompoent.MonsterProp.GetCurrentHp() < _minHp)
//                        {
//                            _unitInfoer = item.Value;
//                            _minHp = item.Value.MonsterCompoent.MonsterProp.GetCurrentHp();
//                        }
//                    }
//                }
//            }
//            return _unitInfoer;
//        }

//        private List<UnitInfo> notFullHpSoldierList = new List<UnitInfo>();
//        // 获取非满血得友军
//        public UnitInfo GetNotFullHpOurSide(EUnitType _eUnitType)
//        {
//            notFullHpSoldierList.Clear();
//            UnitInfo _unitInfoer = null;
//            Dictionary<Agent, UnitInfo> _searchDic = _eUnitType == EUnitType.Friend ? friendDic : enemyDic;
//            foreach (var item in _searchDic)
//            {
//                if (item.Value.MonsterCompoent.MonsterBDead() == false && item.Value.MonsterCompoent.BeFullHp() == false 
//                    && BFriendBuilds(item.Value.FriendType) == false && item.Value.MonsterCompoent.IsBuildingMonster() == false)                 
//                {
//                    notFullHpSoldierList.Add(item.Value);                   
//                }
//            }
//            if(notFullHpSoldierList.Count > 0)
//            {
//                int _index = Random.Range(0, notFullHpSoldierList.Count);
//                _unitInfoer = notFullHpSoldierList[_index];
//            }
//            return _unitInfoer;
//        }

//        // 查找在搜索范围内的最近敌人
//        private HashSet<RvoKDState> serchQueryResult = new HashSet<RvoKDState>();
//        public UnitInfo GetNearestOppositeUnitBySearchDis(Vector3 _currentPoint, EUnitType _eUnitType, float _searchRadius)
//        {
//            float _minSqrDis = 0;
//            UnitInfo _unitInfoer = null;
//            UnitInfo _buildUnitInfoer = null;
//            serchQueryResult.Clear();
//            Dictionary<Agent, UnitInfo> _searchDic = _eUnitType == EUnitType.Friend ? enemyDic : friendDic;
//            tree.QueryInRange(Samples.VectorUtils.ToAVec(new UVector2(_currentPoint.x, _currentPoint.z)), _searchRadius, serchQueryResult);
//            foreach (var result in serchQueryResult)
//            {
//                if (result.agent.NeedDestory == false)
//                {
//                    if (result.agent.transform == null)
//                        continue;

//                    MonsterBaseComponent _mbComponent = null; // result.MonsterCompoent;
//                    if (_searchDic.ContainsKey(result.agent))
//                    {
//                        _mbComponent = _searchDic[result.agent].MonsterCompoent;
//                    }
//                    if (_mbComponent == null || _mbComponent.MonsterBDead() == true)
//                        continue;
//                    if (_eUnitType == EUnitType.Friend && _mbComponent.EUnitType == EUnitType.ThirdParty)
//                        continue;
//                    EUnitType _oppositeUnitType = getOppositeUnitType(_eUnitType);
//                    if(_mbComponent.EUnitType == _oppositeUnitType)
//                    {
//                        //if(_buildUnitInfoer == null)
//                        //{
//                        //    if(_mbComponent.IsBuildingMonster())
//                        //    {
//                        //        _buildUnitInfoer = _mbComponent.UnitInfoer;
//                        //        continue;
//                        //    }
//                        //}
//                        //if (_mbComponent.IsBuildingMonster() == false)
//                        {
//                            if (_minSqrDis == 0)
//                            {
//                                _minSqrDis = (_currentPoint - _mbComponent.transform.position).sqrMagnitude;
//                                _unitInfoer = _mbComponent.UnitInfoer;
//                            }
//                            else
//                            {
//                                float _sqr = (_currentPoint - _mbComponent.transform.position).sqrMagnitude;
//                                if (_sqr < _minSqrDis)
//                                {
//                                    _unitInfoer = _mbComponent.UnitInfoer;
//                                    _minSqrDis = _sqr;
//                                }
//                            }
//                        }
//                    }                 
//                }
//            }
//            //if(_unitInfoer == null)
//            //{
//            //    if(_buildUnitInfoer != null)
//            //    {
//            //        return _buildUnitInfoer;
//            //    }
//            //}
//            return _unitInfoer;
//        }

//        // 根据搜索距离 查找在搜索范围内的敌人(非最近)
//        private HashSet<RvoKDState> serchQueryResult2 = new HashSet<RvoKDState>();
//        private List<UnitInfo> randomList = new List<UnitInfo>();
//        public UnitInfo GetOppositeUnitBySearchDis(Vector3 _currentPoint, EUnitType _eUnitType, float _searchRadius)
//        {
//            randomList.Clear();
//            UnitInfo _buildUnitInfoer = null;
//            serchQueryResult2.Clear();
//            tree.QueryInRange(Samples.VectorUtils.ToAVec(new UVector2(_currentPoint.x, _currentPoint.z)), _searchRadius, serchQueryResult2);
//            Dictionary<Agent, UnitInfo> _searchDic = _eUnitType == EUnitType.Friend ? enemyDic : friendDic;

//            foreach (var result in serchQueryResult2)
//            {
//                if (result.agent.NeedDestory == false)
//                {
//                    if (result.agent.transform == null)
//                        continue;

//                    //MonsterBaseComponent _mbComponent = result.MonsterCompoent;
//                    MonsterBaseComponent _mbComponent = null; // result.MonsterCompoent;
//                    if (_searchDic.ContainsKey(result.agent))
//                    {
//                        _mbComponent = _searchDic[result.agent].MonsterCompoent;
//                    }
//                    if (_mbComponent == null || _mbComponent.MonsterBDead() == true)
//                        continue;
//                    if (_eUnitType == EUnitType.Friend && _mbComponent.EUnitType == EUnitType.ThirdParty)
//                        continue;
//                    EUnitType _oppositeUnitType = getOppositeUnitType(_eUnitType);
//                    if (_mbComponent.EUnitType == _oppositeUnitType)
//                    {
//                        randomList.Add(_mbComponent.UnitInfoer);
//                        //return _mbComponent.UnitInfoer;
//                        //if(_mbComponent.IsBuildingMonster() == false)
//                        //{
//                        //    return _mbComponent.UnitInfoer;
//                        //}
//                        //if (_buildUnitInfoer == null)
//                        //{
//                        //    if (_mbComponent.IsBuildingMonster())
//                        //    {
//                        //        _buildUnitInfoer = _mbComponent.UnitInfoer;
//                        //        continue;
//                        //    }
//                        //}                           
//                    }
//                }
//            }
//            if(randomList.Count > 0)
//            {
//                return randomList[Random.Range(0, randomList.Count)];
//            }
//            //if (_buildUnitInfoer != null)
//            //{
//            //    return _buildUnitInfoer;
//            //}

//            return null;
//        }

//        // 随机获取一个敌对阵营单位
//        public UnitInfo GetRandomOppositeUnit(EUnitType _eUnitType)
//        {
//            UnitInfo _unitInfoer = null;
//            Dictionary<Agent, UnitInfo> _searchDic = _eUnitType == EUnitType.Friend ? enemyDic : friendDic;
//            if (_searchDic.Count <= 0)
//                return _unitInfoer;
//            if (RvoManager.RvoManagerIns.agents.Count > 0)
//            {
//                for (int i = 0; i < RvoManager.RvoManagerIns.agents.Count; i++)
//                {
//                    int _randomIndex = Random.Range(0, RvoManager.RvoManagerIns.agents.Count);
//                    Agent _agent = RvoManager.RvoManagerIns.agents[_randomIndex];
//                    if (_agent.NeedDestory == false && _searchDic.ContainsKey(_agent))
//                    {
//                        _unitInfoer = _searchDic[_agent];
//                        if (_unitInfoer.MonsterCompoent.EUnitType == _eUnitType)
//                        {
//                            continue;
//                        }
//                        return _unitInfoer;
//                    }
//                }
//            }
//            return null;
//        }
//        #endregion


//        protected virtual void Update()
//        {
//            if (GameStop == true)
//                return;
//            if (simulation == null)
//                return;

//            //Schedule the simulation job. 
//            simulation.Schedule(Time.deltaTime);

//            //Store "target" position
//            //float3 tr = target.position;

//            #region update & draw agents

//            //Draw agents debug
//            IAgent agent;
//            float3 agentPos;
//            for (int i = 0, count = agents.Count; i < count; i++)
//            {
//                agent = agents[i] as IAgent;
//                agentPos = agent.pos;
//                float3 tarPos = ((Agent)agent).transform.position;  //target.position;
//                if (enemyDic.ContainsKey((Agent)agent))
//                {
//                    tarPos = enemyDic[(Agent)agent].MonsterCompoent.GetTargetPosition();
//                }
//                else if(friendDic.ContainsKey((Agent)agent))
//                {
//                    tarPos = friendDic[(Agent)agent].MonsterCompoent.GetTargetPosition();
//                }
//#if UNITY_EDITOR
//                //Agent body
//                if (axis == AxisPair.XY)
//                {
//                    Draw.Circle2D(agentPos, agent.radius, Color.green, 12);
//                    Draw.Circle2D(agentPos, agent.radiusObst, Color.cyan.A(0.15f), 12);
//                }
//                else
//                {
//                    Draw.Circle(agentPos, agent.radius, Color.green, 12);
//                    Draw.Circle(agentPos, agent.radiusObst, Color.cyan.A(0.15f), 12);

//                }
//                //Agent simulated velocity (ORCA compliant)
//                Draw.Line(agentPos, agentPos + (normalize(agent.velocity) * agent.radius), Color.green);
//                //Agent goal vector
//                Draw.Line(agentPos, agentPos + (normalize(agent.prefVelocity) * agent.radius), Color.grey);
//#endif
//                //Update agent preferred velocity so it always tries to reach the "target" object
//                //float mspd = max(minSpeed + (i + 1) * 0.5f, maxSpeed);
//                //float s = min(1f, distance(agent.pos, tr) / mspd);
//                //float agentSpeed = mspd * s;
//                //float _maxSpeed = agentSpeed * s;
//                //float _speed = _maxSpeed;
//                //if (_speed > agent.saveMaxSpeed)
//                //    _speed = agent.saveMaxSpeed;
//                //agent.maxSpeed = _speed;

//                float agentSpeed = agent.saveMaxSpeed;    ////
//                agent.maxSpeed = agent.saveMaxSpeed;      ////

//                agent.prefVelocity = normalize(tarPos - agent.pos) * agentSpeed;

//                ////if (agent.transform != null)
//                ////{
//                ////    agent.transform.rotation = Quaternion.Lerp(agent.transform.rotation, Quaternion.LookRotation(agent.velocity), Time.smoothDeltaTime * 3f);
//                ////    agent.transform.position = Vector3.Lerp(agent.transform.position, agent.pos, Time.smoothDeltaTime * 3f);
//                ////}

//                agent.maxNeighbors = 20;   
//                //agent.neighborDist = 0.1f; // 避开其他智能体的距离
//                agent.timeHorizon = 0.1f;   // 距离其他代理检查
//                agent.timeHorizonObst = 4f; // 速度越小，这个值越大，才不会穿透不可行走区域，距离障碍

//                agent.radiusObst = agent.radius;

//                //ObjectList[i].transform.position = agent.pos;
//                //float3 velocity = normalize(tr - agent.pos);
//                //float3 forward = new float3(velocity.x, 0 ,velocity.z);
//                //ObjectList[i].transform.rotation = Quaternion.Lerp(ObjectList[i].transform.rotation, Quaternion.LookRotation(agent.velocity), Time.deltaTime * 3f);
//                //var kd2 = tree.managed[i] as RvoKDState;
//                //kd2.pos.x = agent.pos.x;
//                //kd2.pos.y = agent.pos.z;
//                //ObjectList[i].transform.rotation = Quaternion.LookRotation(agent.velocity);
//            }

//            #endregion

//#if UNITY_EDITOR

//            #region draw obstacles

//            //Draw static obstacles
//            Obstacle o;
//            int oCount = obstacles.Count, subCount;
//            for (int i = 0; i < oCount; i++)
//            {
//                o = obstacles[i];
//                subCount = o.Count;

//                //Draw each segment
//                for (int j = 1, count = o.Count; j < count; j++)
//                {
//                    Draw.Line(o[j - 1].pos, o[j].pos, staticObstacleColor);
//                    Draw.Circle(o[j - 1].pos, 0.2f, Color.magenta, 6);
//                }
//                //Draw closing segment (simulation consider 2+ segments to be closed.)
//                if (!o.edge)
//                    Draw.Line(o[subCount - 1].pos, o[0].pos, staticObstacleColor);
//            }

//            float delta = Time.deltaTime * 50f;


//            #endregion

//#endif

//            updateKDTree();

//        }
//        public void createKDTree()
//        {
//            tree = new KDTree<RvoKDState>();
//            tree.initPool();
//        }

//        // 存一个删除时刻执行的逻辑，比如自爆兵，爆炸油桶
//        List<MonsterBaseComponent> needDestoryMonsterList = new List<MonsterBaseComponent>();

//        private int humanCount = 0;
//        public int HumanCount => humanCount;
//        public void initKDTree()
//        {
//            needDestoryMonsterList.Clear();
//            tree.recycleAgent();
//            int count = agents.Count;
//            tree.Clear();
//            humanCount = 0;
//            for (int i = count - 1; i >= 0; i--)
//            {
//                Agent _agent;
//                float3 agentPos;
//                _agent = agents[i] as Agent;
//                if(_agent.NeedDestory == true)
//                {
//                    //GameObject.Destroy(_agent.transform.gameObject);                                  
//                    agents.Remove(_agent);
//                    if (enemyDic.ContainsKey(_agent))
//                    {
//                        BattleRewardDataSystem.Ins.BeKillMonsterComp(enemyDic[_agent].MonsterCompoent);
//                        if (enemyDic[_agent].MonsterCompoent.BAfterDestory() == true)
//                            needDestoryMonsterList.Add(enemyDic[_agent].MonsterCompoent);
//                        enemyDic.Remove(_agent);

//                    }
//                    else if (friendDic.ContainsKey(_agent))
//                    {
//                        if (FarmLandBattleManager.FarmLandBattleManagerIns != null)
//                        {
//                            if (friendDic[_agent].MonsterCompoent.IsBuildingMonster() == false)
//                                FarmLandBattleManager.FarmLandBattleManagerIns.ReleaseUsePoint(friendDic[_agent].FriendType, friendDic[_agent].MonsterCompoent.TargetPoint);
//                        }
//                        hurtSoldier(friendDic[_agent].FriendType);
//                        if (friendDic[_agent].MonsterCompoent.BAfterDestory() == true)
//                            needDestoryMonsterList.Add(friendDic[_agent].MonsterCompoent);
//                        friendDic.Remove(_agent);

//                    }
//                    continue;
//                }
//                else
//                {
//                    if(friendDic.ContainsKey(_agent))
//                    {
//                        if (friendDic[_agent].MonsterCompoent.IsBuildingMonster() == false)
//                        {
//                            ++humanCount;
//                        }
//                    }
//                }
//                //_agent.transform.name = "chongzi_" + i;
//                agentPos = _agent.pos;
//                AVector2 v2;
//                v2.x = agentPos.x;
//                v2.y = agentPos.y;
//                var pws = tree.applyAgent();//new RvoKDState();
//                pws.id = i;
//                pws.pos = v2;
//                pws.agent = (Agent)_agent;                
//                tree.Add(pws);
//            }
//            for(int i = 0; i < needDestoryMonsterList.Count; i++)
//            {               
//                needDestoryMonsterList[i].DoAfterDestory();             
//            }
//            for(int i = netualList.Count - 1; i >= 0; i--)
//            {
//                if(netualList[i].MonsterBDead())
//                {
//                    netualList[i].DoAfterDestory();
//                    netualList.RemoveAt(i);
//                }
//            }
//            needDestoryMonsterList.Clear();
//            if(UI_Battle_Panel.Ins != null)
//            {
//                if (BattleDataProxy.Ins.BattleType == EBattleType.AttackCity)
//                    UI_Battle_Panel.Ins.UI_AttackCity_Panel.SetPeople(humanCount);
//                if(BattleDataProxy.Ins.BattleType == EBattleType.FarmLandCity)
//                    UI_Battle_Panel.Ins.UI_Buildings_Panel.SetMaxPeople(humanCount);
//            }
//        }
//        public void updateKDTree()
//        {
//            if (tree != null)
//                tree.Rebuild();
//        }


//        // 同一个帧内删除掉kdtree的值，但是没有updateKDTree ，update是在下一针跑的，所以会报错误，后续可能把当前帧打死的所有怪物搜集一下，在下一针去删除agents列表然后在updatekdtree
//        public void TouchSomeThing(UVector2 _touchV2, float _radius, BulletType _bulletType, Transform _hitTransform, EUnitType _attackUnitType, EBuffType _buffType, int _attackDamage, int _buffDamage = 0, bool _protectPlayer = false)
//        {
//            if (GameStop == true)
//                return;
//            if (tree == null)
//                return;           

//            bool _rebuildTree = false;
//            // 射线击中的目标处理
//            MonsterBaseComponent _hitMbComp = null;
//            if (_hitTransform != null && _hitTransform.gameObject.layer != 29)
//            {
//                _hitMbComp = _hitTransform.GetComponent<MonsterBaseComponent>();               
//                if (_hitMbComp != null &&_hitMbComp.UnitInfoer.Agenter.NeedDestory == false)
//                {
//                    if (canDoDamage(_hitMbComp.UnitInfoer.MonsterCompoent.EUnitType, _attackUnitType, _protectPlayer, _buffType))
//                    {
//                        _rebuildTree |= DoSingleDamage(_hitMbComp, _attackDamage, _bulletType, _touchV2, _buffType, _buffDamage, _protectPlayer);
//                    }
//                }
//            }

//            // 射线击中后范围处理
//            _rebuildTree = _rebuildTree | doRangeDamage(_touchV2, _radius, _bulletType, _hitMbComp, _attackUnitType, _buffType, _attackDamage, _buffDamage, _protectPlayer);
//            if(_rebuildTree)
//            {
//                initKDTree();
//            }

//            if (OnJudgeWinGame())
//                return;

//            if (OnJudgeLoseGame())
//                return;


//            //// 胜利
//            //if (enemyDic.Count <= 0)
//            //{
//            //    WinGame();
//            //    return;
//            //}

//            //// 失败
//            //if (FriendDoor != null && FriendDoor.UnitBDead() ==  true)
//            //{
//            //    LoseGame();
//            //    return;
//            //}

//            //// 攻城战失败
//            //if(BattleDataProxy.Ins.BattleType == EBattleType.AttackCity)
//            //{               
//            //    if(friendDic.Count <= 0)
//            //    {
//            //        LoseGame();
//            //    }
//            //    return;
//            //}

//            //initKDTree  还好
//            //initKDTree();
//            //updateKDTree很耗时
//            //updateKDTree();

//        }

//        protected virtual bool OnJudgeWinGame()
//        {
//            if (enemyDic.Count <= 0)
//            {
//                WinGame();
//                return true;
//            }
//            return false;
//        }

//        protected virtual bool OnJudgeLoseGame()
//        {
//            // 失败
//            if (FriendDoor != null && FriendDoor.UnitBDead() == true)
//            {
//                LoseGame();
//                return true;
//            }
//            return false;
//        }

//        protected void WinGame()
//        {
//            if (GameStop == false)
//            {
//                GameStop = true;
//                BattleManager.Ins.SetResult(EGameResult.Win);
//                StartCoroutine("playVictoryAction");
//            }
//            refuseBattleData();
//        }

//        protected void LoseGame()
//        {
//            if (GameStop == false)
//            {
//                GameStop = true;
//                StartCoroutine("playVictoryAction");
//                BattleManager.Ins.SetResult(EGameResult.Lose);
//            }
//            refuseBattleData();
//        }


//        IEnumerator playVictoryAction()
//        {
//            yield return new WaitForSeconds(0.5f);
//            foreach (var _info in friendDic)
//            { 
//                _info.Value.MonsterCompoent.StateMachine.SetCurrentState(StateType.Run);
//                _info.Value.MonsterCompoent.PlayAnim(EActionType.Victory);
//            }
//            foreach (var _info in enemyDic)
//            {
//                _info.Value.MonsterCompoent.StateMachine.SetCurrentState(StateType.Run);
//                _info.Value.MonsterCompoent.PlayAnim(EActionType.Victory);
//            }
//            friendDic.Clear();
//            enemyDic.Clear();
//        }

//        public void ExitBattle()
//        {
//            //if (GameStop == false)
//            {
//                GameStop = true;
//                StartCoroutine("playVictoryAction");
//                BattleManager.Ins.SetResult(EGameResult.Lose);              
//                refuseBattleData();
//            }
//        }

//        //private void allPlayVictor()
//        //{
//        //    foreach(var v in friendDic)
//        //    {
//        //        v.Value.MonsterCompoent.PlayAnim(EActionType.Victory);
//        //    }
//        //    foreach (var v in enemyDic)
//        //    {
//        //        v.Value.MonsterCompoent.PlayAnim(EActionType.Victory);
//        //    }
//        //}

//        protected void refuseBattleData()
//        {
//            agents.Clear();
//            agents.Release();
//            obstacles.Clear();
//            simulation.staticObstacles = null;
//            simulation.agents = null;

//            FriendDoor = null;

//            netualList.Clear();
//            tree.Clear();
//        }

//        private bool doRangeDamage(UVector2 _touchV2, float _radius, BulletType _bulletType, MonsterBaseComponent _hitMbComp, EUnitType _attackUnitType, EBuffType _buffType, int _damage, int _buffDamage = 0, bool _protectPlayer = false)
//        {
//            if (_radius <= 0)
//                return false;
//            bool _dead = false;
//            tree.QueryInRange(Samples.VectorUtils.ToAVec(_touchV2), _radius, queryResult);
//            foreach (var result in queryResult)
//            {
//                if (result.agent.NeedDestory == false)
//                {
//                    if (result.agent.transform == null)
//                        continue;
//                    MonsterBaseComponent _mbc = null;
//                    if(friendDic.ContainsKey(result.agent))
//                    {
//                        _mbc = friendDic[result.agent].MonsterCompoent;
//                    }
//                    else if(enemyDic.ContainsKey(result.agent))
//                    {
//                        _mbc = enemyDic[result.agent].MonsterCompoent;
//                    }
//                    if (_mbc == null)
//                        continue;
//                    if ( _mbc == _hitMbComp || canDoDamage(_mbc.EUnitType, _attackUnitType, _protectPlayer, _buffType) == false)
//                        continue;
//                    _dead |= DoSingleDamage(_mbc, _damage, _bulletType, _touchV2, _buffType, _buffDamage, _protectPlayer);

//                    //result.MonsterCompoent = result.agent.transform.GetComponent<MonsterBaseComponent>();
//                    //if (result.MonsterCompoent == _hitMbComp || canDoDamage(result.MonsterCompoent.EUnitType, _attackUnitType, _protectPlayer, _buffType) == false)
//                    //    continue;
//                    //_dead |= DoSingleDamage(result.MonsterCompoent, _damage, _bulletType, _touchV2, _buffType, _buffDamage, _protectPlayer);                      
//                }
//            }
//            return _dead;
//        }

//        // 单个伤害
//        public bool DoSingleDamage(MonsterBaseComponent _mbComp, int _attackDamageValue, BulletType _bulletType, UVector2 _touchV2, EBuffType _buffType, int _buffDamage = 0, bool _protectPlayer = false)
//        {           
//            // 治疗
//            if(_buffType == EBuffType.Health)
//            {
//                _mbComp.DoMonsterDamage(_attackDamageValue);
//                return false;
//            }
//            // 伤害
//            _mbComp.DoMonsterDamage(-_attackDamageValue);
//            _mbComp.DamageSlash();
//            if (_protectPlayer == true && _mbComp.EUnitType == EUnitType.Enemy)
//                BattleScoreDataSystem.Ins.ProtecterHitMonster();
//            if (_mbComp.MonsterBDead())
//            {
//                // 击杀记录
//                if(_mbComp.EUnitType == EUnitType.Enemy)
//                    BattleScoreDataSystem.Ins.SetProtectKillMonster(_mbComp.transform.position, _protectPlayer);                
//                _mbComp.DestorySelf(_bulletType, _touchV2);
//                _mbComp.UnitInfoer.Agenter.NeedDestory = true;
//                return true;
//            }
//            else
//            {                         
//                // 添加buff
//                if (_buffType != EBuffType.None)
//                {
//                    float _buffBeiLv = 0.3f;
//                    if(_bulletType == BulletType.IceLazer)
//                    {
//                        _buffBeiLv = 0.6f;
//                    }
//                    else if(_bulletType == BulletType.StormIce)
//                    {
//                        _buffBeiLv = 0.4f;
//                    }
//                    if(_attackDamageValue != 0)
//                        _mbComp.SetBuff(_buffType, _attackDamageValue, _buffBeiLv);
//                    else
//                        _mbComp.SetBuff(_buffType, _buffDamage, _buffBeiLv);
//                }
//            }
//            return false;
//        }

//        // 范围加血
//        private HashSet<RvoKDState> addBloodQueryResult = new HashSet<RvoKDState>();
//        public void DoRangeAddBlood(UVector2 _touchV2, float _radius, int _addBloodValue, GameObject _objectBoomPrefab)
//        {
//            if (_radius <= 0)
//                return;
//            tree.QueryInRange(Samples.VectorUtils.ToAVec(_touchV2), _radius, addBloodQueryResult);
//            foreach (var result in addBloodQueryResult)
//            {
//                if (result.agent.NeedDestory == false)
//                {
//                    if (result.agent.transform == null)
//                        continue;
//                    MonsterBaseComponent _mbc = null;

//                    if (friendDic.ContainsKey(result.agent))
//                    {
//                        _mbc = friendDic[result.agent].MonsterCompoent;
//                    }
//                    else if (enemyDic.ContainsKey(result.agent))
//                    {
//                        _mbc = enemyDic[result.agent].MonsterCompoent;
//                    }
//                    if (_mbc == null)
//                        continue;
//                    if (_mbc.EUnitType == EUnitType.Friend && _mbc.EUnitType != EUnitType.ForthParty)
//                    {
//                        GameObject _boom = _objectBoomPrefab.GetCatchEntityObject(2f);
//                        _boom.transform.position = _mbc.transform.position;
//                        _mbc.DoMonsterDamage(_addBloodValue);
//                    }

//                    //result.MonsterCompoent = result.agent.transform.GetComponent<MonsterBaseComponent>();
//                    //if(result.MonsterCompoent.EUnitType == EUnitType.Friend && result.MonsterCompoent.EUnitType != EUnitType.ForthParty)
//                    //{
//                    //    GameObject _boom = _objectBoomPrefab.GetCatchEntityObject(2f);
//                    //    _boom.transform.position = result.MonsterCompoent.transform.position;
//                    //    result.MonsterCompoent.DoMonsterDamage(_addBloodValue);
//                    //}
//                }
//            }
//        }

//        // 根据阵营判断是否可以攻击敌人
//        private bool canDoDamage(EUnitType _beHurtUnitType, EUnitType _attackUnitType, bool _protecter, EBuffType _buffType)
//        {   

//            // 治疗类型，攻击和受击是同一个阵营
//            if(_buffType == EBuffType.Health && _beHurtUnitType == _attackUnitType)
//            {
//                return true;
//            }
//            if(_protecter == true)
//            {
//                // 主角的攻击
//                if(_beHurtUnitType == EUnitType.ForthParty)
//                {
//                    return true;
//                }
//            }
//            else
//            {
//                if(_attackUnitType == EUnitType.Friend && _beHurtUnitType == EUnitType.ForthParty)
//                {
//                    return false;
//                }
//            }
//            // 第三方单位只作用于敌人
//            if (_attackUnitType == EUnitType.ThirdParty && _beHurtUnitType == EUnitType.Enemy)
//            {
//                return true;
//            }
//            else
//            {
//                if(_attackUnitType == EUnitType.ThirdParty && _beHurtUnitType == EUnitType.Friend)
//                {
//                    return false;
//                }
//                if (_beHurtUnitType != _attackUnitType)
//                    return true;
//            }
//            return false;
//        }

//        // 获取对面阵营
//        private EUnitType getOppositeUnitType(EUnitType _unitType)
//        {
//            if (_unitType == EUnitType.Friend)
//                return EUnitType.Enemy;
//            if (_unitType == EUnitType.Enemy)
//                return EUnitType.Friend;         
//            return EUnitType.Enemy;
//        }

//        private void LateUpdate()
//        {
//            //if (gameEndState == true)
//            //    return;
//            //Attempt to complete and apply the simulation results, only if the job is done.
//            //TryComplete will not force job completion.
//            if (simulation != null && simulation.TryComplete())
//            {
//                if (GameStop == true)
//                {                   
//                    //simulation.Dispose();
//                    simulation.DisposeAll();
//                    simulation = null;
//                }
//                //Move dynamic obstacles randomly
//                //int oCount = dynObstacles.Count;
//                //float delta = Time.deltaTime * 50f;

//                //for (int i = 0; i < oCount; i++)
//                //dynObstacles[i].Offset(float3(Random.Range(-delta, delta), Random.Range(-delta, delta), 0f));

//            }
//        }

//        private void OnApplicationQuit()
//        {
//            //Make sure to clean-up the jobs
//            simulation.DisposeAll();
//        }

//        public static bool BFriendBuilds(EFriendType _friendType)
//        {
//            if (_friendType == EFriendType.BoomBucket || _friendType == EFriendType.ArrowTower || _friendType == EFriendType.FriendDoor || _friendType == EFriendType.ProtectWoodDoor)
//                return true;
//            return false;
//        }
//    }
//}