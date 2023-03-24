using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StackStorageGrid : MonoBehaviour
{
    private GridXZ<StackStorageGridItem> _grid ;
    public void SetGrid(GridXZ<StackStorageGridItem> grid)
    {
        _grid = grid;
    }
    
}
