using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crate : MonoBehaviour
{
    private GridXZ<StackStorageGridItem> _storageGrid;

    public int currentX;
    public int currentZ;
    public int storingX;
    public int storingZ;
    private void Start()
    {
        
    }

    public void Init(GridXZ<StackStorageGridItem> storageGrid, int destinationX, int destinationZ)
    {
        _storageGrid = storageGrid;
        storingX = destinationX;
        storingZ = destinationZ;
    }
}
