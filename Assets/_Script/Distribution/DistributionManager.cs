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
 

        
    private GridXZ<StackStorageGridItem> _storageGrid;
    private int width, height;
    
    private float _currentTime = 0f;
    private List<Crate> _pendingCrates = new();
    private List<Robot> _robots;

    IEnumerator Start()
    {
        yield return null;
        _storageGrid = MapManager.Instance.storageGrid;
        (width,height) = _storageGrid.GetWidthHeight();
        
    }

    void Update()
    {
        _currentTime += Time.deltaTime;
        if (_currentTime >= spawnRate)
        {
            CreateBundle();
            _currentTime -= spawnRate;
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
        var freshCrate = Instantiate(ResourceManager.Instance.crate, transform);
        freshCrate.currentX = Random.Range(0, width);
        freshCrate.currentZ = Random.Range(0, height);
        freshCrate.storingX = Random.Range(0, width);
        freshCrate.storingZ = Random.Range(0, height);
        
        _pendingCrates.Add(freshCrate);
    }
}
