
using System;
using System.Collections.Generic;
using _Script.Robot;
using Shun_Grid_System;
using UnityEngine;

[Serializable]
public class BinTransportTask
{
    public GridXZCell<CellItem> TargetBinDestination;
    public GridXZCell<CellItem> TargetBinSource;
    
    public List<Robot> MobilizedRobots = new();
    public Robot HoldingRobot;

    public List<Bin> MigratedBins = new ();
    public Bin TargetBin;

    public float PickUpTime;
    public float CreateTaskTime;
    public BinTransportTask(Bin targetBin, GridXZCell<CellItem> targetBinSource, GridXZCell<CellItem> targetBinDestination)
    {
        TargetBin = targetBin;
        TargetBinSource = targetBinSource;
        TargetBinDestination = targetBinDestination;

        CreateTaskTime = Time.time;
    }

    public void PickUpBin(Bin bin)
    {
        if (bin == TargetBin) PickUpTargetBin();
        else
        {
            MigratedBins.Add(bin);
        }
    }

    private void PickUpTargetBin()
    {
        PickUpTime = Time.time;
    }

    public void DropDownBin(Bin bin)
    {
        
    }
    
}