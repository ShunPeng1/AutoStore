using System;
using System.Collections;
using System.Collections.Generic;
using _Script.Robot;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityUtilities;
using Random = UnityEngine.Random;

public class DistributionManager : SingletonMonoBehaviour<DistributionManager>
{
    [Header("Bundle Spawn")] [SerializeField, Range(1f, 100f)]
    private float spawnRate = 5f;

    [SerializeField, Range(1, 100)] private int maxPendingCrate = 100;


    private GridXZ<StackStorageGridCell> _storageGrid;
    private int width, height;

    private float _currentTime = 0f;
    private Queue<Crate> _pendingCrates = new();
    private Robot[] _robots;

    IEnumerator Start()
    {
        yield return null;
        _storageGrid = MapManager.Instance.storageGrid;
        (width, height) = _storageGrid.GetWidthHeight();

        _robots = FindObjectsOfType<Robot>();
    }

    void Update()
    {
        _currentTime += Time.deltaTime;
        if (_currentTime >= spawnRate)
        {
            CreateBundle();
            _currentTime = 0;
        }

        while (_pendingCrates.Count > 0)
        {
            var crate = _pendingCrates.Peek();
            Robot shortestReachRobot = null;
            int shortestReach = int.MaxValue;

            foreach (var robot in _robots)
            {
                if (robot.robotState == Robot.RobotState.Idle)
                {
                    int reach = CalculateReach(robot, crate);
                    if (reach < shortestReach)
                    {
                        shortestReachRobot = robot;
                        shortestReach = reach;
                    }
                }
            }

            if (shortestReachRobot == null) break;

            shortestReachRobot.TransportCrate(crate);
            _pendingCrates.Dequeue();
        }
    }

    private int CalculateReach(Robot robot, Crate crate)
    {
        (int x, int z) = StackStorageGridCell.GetIndexDifferenceAbsolute(
            _storageGrid.GetItem(crate.currentX, crate.currentZ),
            robot.getCurrentGridCell());
        return 10 * x + 10 * z;
    }

    private void CreateBundle()
    {
        int currentX = Random.Range(0, width), currentZ = Random.Range(0, height);
        int storingX = Random.Range(0, width), storingZ = Random.Range(0, height);
        var freshCrate = Instantiate(ResourceManager.Instance.GetRandomCrate(),
            _storageGrid.GetWorldPosition(currentX, currentZ), Quaternion.identity);

        freshCrate.Init(_storageGrid, currentX, currentZ, storingX, storingZ);
        _pendingCrates.Enqueue(freshCrate);
    }

    public void RequestMission(Robot robot)
    {
        if (_pendingCrates.Count > 0)
        {
            var crate = _pendingCrates.Dequeue();
            robot.TransportCrate(crate);
        }
    }
}