using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Crate : MonoBehaviour
{
    private GridXZ<GridXZCell<StackStorage>> _storageGrid;

    public int CurrentX;
    public int CurrentZ;
    public int StoringX;
    public int StoringZ;
    public float PullUpTime, DropDownTime;
    
    
    private void Start()
    {
        
    }

    public void Init(GridXZ<GridXZCell<StackStorage>> storageGrid,int spawnX, int spawnZ , int destinationX, int destinationZ, float pullUpTime, float dropDownTime)
    {
        _storageGrid = storageGrid;
        CurrentX = spawnX;
        CurrentZ = spawnZ;
        StoringX = destinationX;
        StoringZ = destinationZ;
        PullUpTime = pullUpTime;
        DropDownTime = dropDownTime;
    }
}
