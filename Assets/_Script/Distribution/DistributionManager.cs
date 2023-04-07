using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityUtilities;
using Random = UnityEngine.Random;

public class DistributionManager : SingletonMonoBehaviour<DistributionManager>
{
    [Header("Bundle Spawn")] 
    [SerializeField, Range(1f, 100f)] private float spawnRate = 5f;
    [SerializeField, Range(1, 100)] private int maxPendingCrate = 100; 

        
    private GridXZ<StackStorageGridItem> _storageGrid;
    private int width, height;
    
    private float _currentTime = 0f;
    private List<Crate> _pendingCrates = new();
    private Robot[] _robots;

    IEnumerator Start()
    {
        yield return null;
        _storageGrid = MapManager.Instance.storageGrid;
        (width,height) = _storageGrid.GetWidthHeight();

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

        foreach (var bundle in _pendingCrates)
        {
            bool isAllBusy = true;
            foreach (var robot in _robots)
            {
                if (robot.robotState == Robot.RobotState.Idle)
                {
                    
                    isAllBusy = false;
                    break;
                }
            }
            
            if(isAllBusy) break;
            
        }
    }

    void CreateBundle()
    {
        int currentX = Random.Range(0, width), currentZ = Random.Range(0, height);
        int storingX = Random.Range(0, width), storingZ = Random.Range(0, height);
        var freshCrate = Instantiate(ResourceManager.Instance.GetRandomCrate(), 
            _storageGrid.GetWorldPosition(currentX,currentZ), Quaternion.identity);

        freshCrate.Init(_storageGrid, storingX, storingZ);
        _pendingCrates.Add(freshCrate);
    }
}
