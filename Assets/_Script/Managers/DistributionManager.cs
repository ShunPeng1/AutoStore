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
        public int SourceDepth; // 0 is the top bin of the stack
        public Vector2Int DestinationGridIndex;
    }

    enum SpawnStyle
    {
        Random,
        Fixed
    }
    [SerializeField] private SpawnStyle _spawnStyle = SpawnStyle.Random;

    [Header("Random Spawn")] 
    [SerializeField, Range(0.001f, 100f)] private float _spawnRate = 5f;
    
    [FormerlySerializedAs("_crateSpawnInfos")]
    [Header("Fixed Spawn")]
    [SerializeField] private List<BinSpawnInfo> _binSpawnInfos;

    [Header("Initial Spawn")] 
    [SerializeField] private int _spawnBinLayerHeight = 5;
    

    private GridXZ<CellItem> _grid;

    private float _currentTime = 0f;
    private Robot[] _robots;

    private Dictionary<Bin, BinTransportTask> _deliveringBinsByBinTransportTasks = new ();
    private Queue<BinTransportTask> _pendingBinTransportTasks = new();

    
    void Start()
    {
        _grid = MapManager.Instance.WorldGrid;

        _robots = FindObjectsOfType<Robot>();
        _binSpawnInfos.Sort((x, y) =>
        {
            var ret = x.ArriveTime.CompareTo(y.ArriveTime);
            return ret;
        });

        InitializeSpawnBin();
    }

    void InitializeSpawnBin()
    {
        for (int x = 0; x < _grid.Width; x++)
        {
            for (int z = 0; z < _grid.Height; z++)
            {
                var cellItem = _grid.GetCell(x, z).Item;
                
                for (int y = 0; y < _spawnBinLayerHeight; y++)
                {
                    var bin = Instantiate(ResourceManager.Instance.GetRandomBin(), cellItem.GetTopStackWorldPosition(), Quaternion.identity);
                    
                    cellItem.AddToStack(bin);
                }
            }
        }
    }

    void Update()
    {
        _currentTime += Time.deltaTime;

        switch (_spawnStyle)
        {
            case SpawnStyle.Random:
                if (_currentTime >= _spawnRate) 
                {
                    AssignBinTransportTaskRandomly();
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
    /// Find robots to get bin
    /// </summary>
    private void AssignMission()
    {
        while (_pendingBinTransportTasks.Count > 0)
        {
            var binTransportTask = _pendingBinTransportTasks.Peek();
            Robot shortestReachRobot = null;
            int shortestReach = int.MaxValue;

            foreach (var robot in _robots)
            {
                if (robot.CurrentRobotState != RobotStateEnum.Idling) continue;
                
                int reach = RobotUtility.GetDistanceFromRobotToBinSource(_grid, robot, binTransportTask);
                
                if (reach >= shortestReach) continue;
                shortestReachRobot = robot;
                shortestReach = reach;
            }

            if (shortestReachRobot == null) break;

            _pendingBinTransportTasks.Dequeue();
            binTransportTask.SetMobilizedRobot(new []{shortestReachRobot});
                        
            shortestReachRobot.ApproachBin(binTransportTask);

        }
    }
    

    /// <summary>
    /// This function create the crate in the game world space
    /// </summary>
    private void AssignBinTransportTaskRandomly()
    {
        int sourceX = Random.Range(0, _grid.Width), sourceZ = Random.Range(0, _grid.Height);
        int destinationX = Random.Range(0, _grid.Width), destinationZ = Random.Range(0, _grid.Height);
        
        GridXZCell<CellItem> sourceCell = _grid.GetCell(sourceX,sourceZ);
        GridXZCell<CellItem> destinationCell = _grid.GetCell(destinationX,destinationZ);

        Bin transportBin = sourceCell.Item.GetRandomBinInStack();

        if (transportBin == null) return;
        
        BinTransportTask task = new BinTransportTask(transportBin, sourceCell, destinationCell);
        
        _pendingBinTransportTasks.Enqueue(task);
    }

    private void CreateBinFixed(BinSpawnInfo binSpawnInfo)
    {
        GridXZCell<CellItem> sourceCell = _grid.GetCell(binSpawnInfo.SourceGridIndex.x,binSpawnInfo.SourceGridIndex.y);
        GridXZCell<CellItem> destinationCell = _grid.GetCell(binSpawnInfo.DestinationGridIndex.x,binSpawnInfo.DestinationGridIndex.y);

        Bin transportBin = sourceCell.Item.GetBinFromTopStack(binSpawnInfo.SourceDepth);
        
        if (transportBin == null) return;
        
        BinTransportTask task = new BinTransportTask(transportBin, sourceCell, destinationCell);
        
        _pendingBinTransportTasks.Enqueue(task);
    }
    
    
    public void ArriveDestination(Robot robot, BinTransportTask binTransportTask)
    {
        DebugUIManager.Instance.AddFinish();

        if (FileRecorderManager.InstanceOptional != null ) 
            FileRecorderManager.Instance.ResultRecords.Add(
                new FileRecorderManager.ResultRecord(
                    Time.time - binTransportTask.PickUpTime, 
                    RobotUtility.GetIdealTimeDeliveryBin(robot, binTransportTask), 
                    binTransportTask.TargetBinSource.XIndex, 
                    binTransportTask.TargetBinSource.YIndex, 
                    binTransportTask.TargetBinDestination.XIndex, 
                    binTransportTask.TargetBinDestination.YIndex ));
    }

    
}