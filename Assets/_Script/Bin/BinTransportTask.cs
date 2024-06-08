
using System;
using System.Collections.Generic;
using _Script.Robot;
using Shun_Grid_System;
using UnityEngine;

[Serializable]
public class BinTransportTask
{
    public GridXZCell<CellItem>[] RobotStartCells;
    public GridXZCell<CellItem> TargetBinDestination;
    public GridXZCell<CellItem> TargetBinSource;
    
    public Robot [] MobilizedRobots;
    public Robot HoldingRobot;

    public List<Bin> MigratedBins = new ();
    public Bin TargetBin;


    public float PickUpTime;
    public float CreateTaskTime;
    public float WaitingForGoalTime;
    public float JammingTime;
    
    public int MainStateChangeCount;
    public int RedirectStateChangeCount;
    public int JamStateChangeCount;
    
    public int PathTurnCount;
    public int PathChangeCount;
    public int PathUpdateCount;
    
    public Vector3 TotalDistance = Vector3.zero;
    
    public BinTransportTask(Bin targetBin, GridXZCell<CellItem> targetBinSource, GridXZCell<CellItem> targetBinDestination)
    {
        TargetBin = targetBin;
        TargetBinSource = targetBinSource;
        TargetBinDestination = targetBinDestination;

        CreateTaskTime = Time.time;
    }

    public void PickUpBin(Robot robot, Bin bin)
    {
        
        PickUpTime = Time.fixedTime;
        if (bin == TargetBin) PickUpTargetBin(robot);
        else
        {
            MigratedBins.Add(bin);
        }
    }

    private void PickUpTargetBin(Robot robot)
    {
        HoldingRobot = robot;
        
        //PickUpTime = Time.fixedTime;
        
    }

    public void DropDownBin(Bin bin)
    {
        
    }
    
    
    public void SetMobilizedRobot(Robot robot)
    {
        MobilizedRobots = new Robot[1];
        MobilizedRobots[0] = robot;
        
        RobotStartCells = new GridXZCell<CellItem>[1];
        RobotStartCells[0] = robot.NextCell;
    }
    

    public void SetMobilizedRobot(Robot [] robots)
    {
        MobilizedRobots = robots;
        
        RobotStartCells = new GridXZCell<CellItem>[robots.Length];

        foreach (var robot in robots)
        {
            RobotStartCells[Array.IndexOf(robots, robot)] = robot.LastCell;
        }
        
    }


}