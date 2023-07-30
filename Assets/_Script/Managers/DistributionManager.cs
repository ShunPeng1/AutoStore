using System;
using System.Collections;
using System.Collections.Generic;
using _Script.Managers;
using _Script.Robot;
using UnityEngine;
using UnityEngine.Serialization;
using UnityUtilities;
using Random = UnityEngine.Random;

public class DistributionManager : SingletonMonoBehaviour<DistributionManager>
{
    [Serializable]
    class CrateSpawnInfo
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
    [SerializeField, Range(1, 100)] private int _maxPendingCrate = 100;
    [SerializeField] private Vector2 _pullUpRandomRange = Vector2.up;
    [SerializeField] private Vector2 _dropDownRandomRange = Vector2.up;
    
    [Header("Fixed Spawn")]
    [SerializeField] private List<CrateSpawnInfo> _crateSpawnInfos;
    
    
    private GridXZ<GridXZCell<StackStorage>> _storageGrid;
    private int _width, _height;

    private float _currentTime = 0f;
    private Queue<Crate> _pendingCrates = new();
    private Robot[] _robots;
    
    void Start()
    {
        _storageGrid = MapManager.Instance.WorldGrid;
        (_width, _height) = _storageGrid.GetWidthHeight();

        _robots = FindObjectsOfType<Robot>();
        _crateSpawnInfos.Sort((x, y) =>
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
                    CreateCrateRandomly();
                    _currentTime = 0;
                }
                break;
            case SpawnStyle.Fixed:
                if (_crateSpawnInfos.Count > 0 && _currentTime >= _crateSpawnInfos[0].ArriveTime)
                {
                    CreateCrateFixed(_crateSpawnInfos[0]);
                    _crateSpawnInfos.RemoveAt(0);
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
        while (_pendingCrates.Count > 0)
        {
            var crate = _pendingCrates.Peek();
            Robot shortestReachRobot = null;
            int shortestReach = int.MaxValue;

            foreach (var robot in _robots)
            {
                if (robot.CurrentRobotState == RobotStateEnum.Idle)
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

            _pendingCrates.Dequeue();
            shortestReachRobot.ApproachCrate(crate);
            
        }
    }
    
    /// <summary>
    /// the distance between robot and crate
    /// </summary>
    private int CalculateDistance(Robot robot, Crate crate)
    {
        (int x, int z) = GridXZCell<StackStorage>.GetIndexDifferenceAbsolute(
            _storageGrid.GetCell(crate.PickUpIndexX, crate.PickUpIndexZ),
            _storageGrid.GetCell(robot.LastCellPosition));
        return 10 * x + 10 * z;
    }

    /// <summary>
    /// This function create the crate in the game world space
    /// </summary>
    private void CreateCrateRandomly()
    {
        int currentX = Random.Range(0, _width), currentZ = Random.Range(0, _height);
        int storingX = Random.Range(0, _width), storingZ = Random.Range(0, _height);
        float pullUpTime = Random.Range(_pullUpRandomRange.x, _pullUpRandomRange.y);
        float dropDownTime = Random.Range(_dropDownRandomRange.x, _dropDownRandomRange.y);
        
        var freshCrate = Instantiate(ResourceManager.Instance.GetRandomCrate(),
            _storageGrid.GetWorldPositionOfNearestCell(currentX, currentZ), Quaternion.identity);

        freshCrate.Init(_storageGrid, currentX, currentZ, storingX, storingZ, pullUpTime, dropDownTime);
        _pendingCrates.Enqueue(freshCrate);
    }

    private void CreateCrateFixed(CrateSpawnInfo crateSpawnInfo)
    {
        var freshCrate = Instantiate(ResourceManager.Instance.GetRandomCrate(),
            _storageGrid.GetWorldPositionOfNearestCell(crateSpawnInfo.SourceGridIndex.x, crateSpawnInfo.SourceGridIndex.y), Quaternion.identity);

        freshCrate.Init(
            _storageGrid, 
            crateSpawnInfo.SourceGridIndex.x, 
            crateSpawnInfo.SourceGridIndex.y, 
            crateSpawnInfo.DestinationGridIndex.x, 
            crateSpawnInfo.DestinationGridIndex.y, 
            crateSpawnInfo.PullUpTime,
            crateSpawnInfo.DropDownTime);
        
        _pendingCrates.Enqueue(freshCrate);
    }
    
    public void RequestMission(Robot robot)
    {
        if (_pendingCrates.Count > 0)
        {
            var crate = _pendingCrates.Dequeue();
            robot.ApproachCrate(crate);
        }
    }

    public void ArriveDestination(Robot robot, Crate crate)
    {
        DebugUIManager.Instance.AddFinish();
        
        FileRecorderManager.Instance.ResultRecords.Add(new FileRecorderManager.ResultRecord(Time.time - crate.RequestedTime, GetTimeFinishAssumption(robot, crate), crate.PickUpIndexX, crate.PickUpIndexZ, crate.DropDownIndexX, crate.DropDownIndexZ ));
    }

    public float GetTimeFinishAssumption(Robot robot, Crate crate)
    {
        float time = (( Mathf.Abs(crate.DropDownIndexX - crate.PickUpIndexX) +  Mathf.Abs(crate.DropDownIndexZ - crate.PickUpIndexZ) ) * (robot.MaxMovementSpeed / Time.fixedDeltaTime ) / 1000f );
        Debug.Log(time + " - " + crate.RequestedTime);
        return time;
    }
    
}