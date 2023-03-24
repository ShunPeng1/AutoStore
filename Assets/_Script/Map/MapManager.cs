using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityUtilities;

public class MapManager : SingletonMonoBehaviour<MapManager>
{
    [SerializeField] private int width = 20, height = 20;
    [SerializeField] private float cellWidthSize = 1f, cellHeightSize = 1f;
    public GridXZ<StackStorageGridItem> storageGrid;

    void Start()
    {
        storageGrid = new GridXZ<StackStorageGridItem>(width, height, cellWidthSize, cellHeightSize, transform.position,
            (grid, x, z) =>
            {
                StackStorage stackStorage = Instantiate(ResourceManager.Instance.stackStorage, grid.GetWorldPosition(x,z),Quaternion.identity ,transform);
                StackStorageGridItem storageGridItem = new StackStorageGridItem(grid, x, z, stackStorage);
                stackStorage.Init(grid, x, z, storageGridItem);
                return storageGridItem;
            });

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                //StackStorage stackStorage = Instantiate(ResourceManager.Instance.stackStorage, transform);
                //stackStorage.Init(storageGrid, x, z);
            }
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            //Vector3 hitPosition = Physics.Raycast(mousePosition, )
            storageGrid.GetItem(mousePosition).AddWeight(1f);
        }
    }
}