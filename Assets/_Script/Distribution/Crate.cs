
using Shun_Grid_System;
using UnityEngine;
using UnityEngine.Serialization;

public class Crate : MonoBehaviour
{
    private GridXZ<CellItem> _storageGrid;

    public int PickUpIndexX;
    public int PickUpIndexZ;
    public int DropDownIndexX;
    public int DropDownIndexZ;
    public float PickUpTime, DropDownTime;

    public float RequestedTime;
    private void Start()
    {
        
    }

    public void Init(GridXZ<CellItem> storageGrid,int pickUpIndexX, int pickUpIndexZ , int dropDownIndexX, int dropDownIndexZ, float pickUpTime, float dropDownTime)
    {
        _storageGrid = storageGrid;
        PickUpIndexX = pickUpIndexX;
        PickUpIndexZ = pickUpIndexZ;
        DropDownIndexX = dropDownIndexX;
        DropDownIndexZ = dropDownIndexZ;
        PickUpTime = pickUpTime;
        DropDownTime = dropDownTime;

    }

    public void PickUp()
    {
        RequestedTime = Time.time;
    }
}
