using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityUtilities;

public class DistributionManager : SingletonMonoBehaviour<DistributionManager>
{
    [Header("Bundle Spawn")] 
    [SerializeField, Range(1f, 100f)] private float spawnRate = 5f; 
    
    
    
    private GridXZ<StackStorageGridItem> _storageGrid;

    
    
    IEnumerator Start()
    {
        yield return null;
        _storageGrid = MapManager.Instance.storageGrid;
    }

    private void Update()
    {
        if (_storageGrid == null) return;
    }
}
