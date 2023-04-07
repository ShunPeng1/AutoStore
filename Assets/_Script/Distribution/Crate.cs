using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crate : MonoBehaviour
{
    private GridXZ<StackStorageGridCell> _storageGrid;

    public int currentX;
    public int currentZ;
    public int storingX;
    public int storingZ;
    private void Start()
    {
        
    }

    public void Init(GridXZ<StackStorageGridCell> storageGrid,int spawnX, int spawnZ , int destinationX, int destinationZ)
    {
        _storageGrid = storageGrid;
        currentX = spawnX;
        currentZ = spawnZ;
        storingX = destinationX;
        storingZ = destinationZ;
    }
}
