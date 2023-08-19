using System;
using System.Collections;
using System.Collections.Generic;
using _Script.Managers;
using _Script.Robot;
using Shun_Grid_System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityUtilities;
using Random = UnityEngine.Random;

public class DistributionManager : SingletonMonoBehaviour<DistributionManager>
{
    [Serializable]
    class BinSpawnInfo
    {
        public float ArriveTime;
        public Vector2Int SourceGridIndex;
        public Vector2Int DestinationGridIndex;
        public float PullUpTime, DropDownTime;
    }

    enum SpawnStyle
    {
        Random,
        Fixed
    }
    [SerializeField] private SpawnStyle _spawnStyle = SpawnStyle.Random;

    [Header("Random Spawn")] 
    [SerializeField, Range(0.001f, 100f)] private float _spawnRate = 5f;
    [SerializeField, Range(1, 100)] private int _maxPendingBin = 100;
    [SerializeField] private Vector2 _pullUpRandomRange = Vector2.up;
    [SerializeField] private Vector2 _dropDownRandomRange = Vector2.up;
    
    [FormerlySerializedAs("_crateSpawnInfos")]
    [Header("Fixed Spawn")]
    [SerializeField] private List<BinSpawnInfo> _binSpawnInfos;
    
    
    private GridXZ<CellItem> _storageGrid;

    private float _currentTime = 0f;
    private Queue<Bin> _pendingBins = new();
    private Robot[] _robots;
    
    void Start()
    {
        _storageGrid = MapManager.Instance.WorldGrid;

        _robots = FindObjectsOfType<Robot>();
        _binSpawnInfos.Sort((x, y) =>
        {
            var ret = x.ArriveTime.CompareTo(y.ArriveTime);
            return ret;
        });
    }

    void Update()
    {
        _currentTime += Time.deltaTime;

        switch (_spawnStyle)
        {
            case SpawnStyle.Random:
                if (_currentTime >= _spawnRate) 
                {
                    CreateBinRandomly();
                    _currentTime = 0;
                }
                break;
            case SpawnStyle.Fixed:
                if (_binSpawnInfos.Count > 0 && _currentTime >= _binSpawnInfos[0].ArriveTime)
                {
                    CreateBinFixed(_binSpawnInfos[0]);
                    _binSpawnInfos.RemoveAt(0);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        AssignMission();
    }

    /// <summary>
    /// 
    /// </summary>
    private void AssignMission()
    {
        while (_pendingBins.Count > 0)
        {
            var crate = _pendingBins.Peek();
            Robot shortestReachRobot = null;
            int shortestReach = int.MaxValue;

            foreach (var robot in _robots)
            {
                if (robot.CurrentRobotState == RobotStateEnum.Idling)
                {
                    int reach = CalculateDistance(robot, crate);
                    if (reach < shortestReach)
                    {
                        shortestReachRobot = robot;
                        shortestReach = reach;
                    }
                }
            }

            if (shortestReachRobot == null) break;

            _pendingBins.Dequeue();
            shortestReachRobot.ApproachBin(crate);
            
        }
    }
    
    /// <summary>
    /// the distance between robot and crate
    /// </summary>
    private int CalculateDistance(Robot robot, Bin bin)
    {
        Vector2Int index = _storageGrid.GetIndexDifferenceAbsolute(
            _storageGrid.GetCell(bin.PickUpIndexX, bin.PickUpIndexZ),
            _storageGrid.GetCell(robot.LastCellPosition));
        return 10 * index.x + 10 * index.y;
    }

    /// <summary>
    /// This function create the crate in the game world space
    /// </summary>
    private void CreateBinRandomly()
    {
        
        int spawnSourceX = Random.Range(0, _storageGrid.Width), spawnSourceZ = Random.Range(0, _storageGrid.Height);
        int storeDestinationX = Random.Range(0, _storageGrid.Width), storeDestinationZ = Random.Range(0, _storageGrid.Height);
        float pullUpTime = Random.Range(_pullUpRandomRange.x, _pullUpRandomRange.y);
        float dropDownTime = Random.Range(_dropDownRandomRange.x, _dropDownRandomRange.y);
        
        CellItem cellItem = _storageGrid.GetCell(spawnSourceX,spawnSourceZ).Item;
        
        var freshBin = Instantiate(ResourceManager.Instance.GetRandomBin(), cellItem.GetTopStackWorldPosition(), Quaternion.identity);

        freshBin.Init(
            _storageGrid, 
            spawnSourceX, 
            spawnSourceZ, 
            storeDestinationX,
            storeDestinationZ,
            pullUpTime, 
            dropDownTime);
        
        _pendingBins.Enqueue(freshBin);
        cellItem.AddToStack(freshBin);
    }

    private void CreateBinFixed(BinSpawnInfo binSpawnInfo)
    {
        CellItem cellItem = _storageGrid.GetCell(binSpawnInfo.SourceGridIndex.x, binSpawnInfo.SourceGridIndex.y).Item;
        
        var freshBin = Instantiate(ResourceManager.Instance.GetRandomBin(), cellItem.GetTopStackWorldPosition(), Quaternion.identity);
        
        freshBin.Init(
            _storageGrid, 
            binSpawnInfo.SourceGridIndex.x, 
            binSpawnInfo.SourceGridIndex.y, 
            binSpawnInfo.DestinationGridIndex.x, 
            binSpawnInfo.DestinationGridIndex.y, 
            binSpawnInfo.PullUpTime,
            binSpawnInfo.DropDownTime);
        
        _pendingBins.Enqueue(freshBin);
        cellItem.AddToStack(freshBin);
    }
    
    public void RequestMission(Robot robot)
    {
        if (_pendingBins.Count > 0)
        {
            var crate = _pendingBins.Dequeue();
            robot.ApproachBin(crate);
        }
    }

    public void ArriveDestination(Robot robot, Bin bin)
    {
        DebugUIManager.Instance.AddFinish();

        if (FileRecorderManager.InstanceOptional != null ) FileRecorderManager.Instance.ResultRecords.Add(new FileRecorderManager.ResultRecord(Time.time - bin.RequestedTime, GetTimeFinishAssumption(robot, bin), bin.PickUpIndexX, bin.PickUpIndexZ, bin.DropDownIndexX, bin.DropDownIndexZ ));
    }

    public float GetTimeFinishAssumption(Robot robot, Bin bin)
    {
        float time = (( Mathf.Abs(bin.DropDownIndexX - bin.PickUpIndexX) +  Mathf.Abs(bin.DropDownIndexZ - bin.PickUpIndexZ) ) * (robot.MaxMovementSpeed / Time.fixedDeltaTime ) / 1000f );
        Debug.Log(time + " - " + bin.RequestedTime);
        return time;
    }
    
}