using System;
using System.Collections;
using System.Collections.Generic;
using _Script.Robot;
using UnityEngine;
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
    }

    enum SpawnStyle
    {
        Random,
        Fixed
    }
    [SerializeField] private SpawnStyle _spawnStyle = SpawnStyle.Random;

    [Header("Random Spawn")] 
    [SerializeField, Range(1f, 100f)] private float _spawnRate = 5f;
    [SerializeField, Range(1, 100)] private int _maxPendingCrate = 100;

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
                    CreateCrateFixed(_crateSpawnInfos[0].SourceGridIndex, _crateSpawnInfos[0].DestinationGridIndex);
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
                if (robot.CurrentBaseState.MyStateEnum == RobotStateEnum.Idle)
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
            _storageGrid.GetCell(crate.currentX, crate.currentZ),
            robot.GetCurrentGridCell());
        return 10 * x + 10 * z;
    }

    /// <summary>
    /// This function create the crate in the game world space
    /// </summary>
    private void CreateCrateRandomly()
    {
        int currentX = Random.Range(0, _width), currentZ = Random.Range(0, _height);
        int storingX = Random.Range(0, _width), storingZ = Random.Range(0, _height);
        var freshCrate = Instantiate(ResourceManager.Instance.GetRandomCrate(),
            _storageGrid.GetWorldPositionOfNearestCell(currentX, currentZ), Quaternion.identity);

        freshCrate.Init(_storageGrid, currentX, currentZ, storingX, storingZ);
        _pendingCrates.Enqueue(freshCrate);
    }

    private void CreateCrateFixed(Vector2Int sourceIndex, Vector2Int destinationIndex)
    {
        var freshCrate = Instantiate(ResourceManager.Instance.GetRandomCrate(),
            _storageGrid.GetWorldPositionOfNearestCell(sourceIndex.x, sourceIndex.y), Quaternion.identity);

        freshCrate.Init(_storageGrid, sourceIndex.x, sourceIndex.y, destinationIndex.x, destinationIndex.y);
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
}