using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour
{
    [SerializeField] private int width = 100, height = 100;
    [SerializeField] private StackStorageGrid _storageGrid; 
    void Start()
    {
        GridXZ<StackStorageGridItem> gridXZ = new GridXZ<StackStorageGridItem>(width, height, 1, 1, transform.position,
            (grid, x, z) => new StackStorageGridItem(grid, x, z));
        StackStorageGrid storageGrid = new ();
        storageGrid.SetGrid(gridXZ);

    }
    
    
}
