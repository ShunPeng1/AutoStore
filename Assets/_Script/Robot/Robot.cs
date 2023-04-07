using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Robot : MonoBehaviour
{
    public enum RobotState
    {
        Idle,
        Delivering,
        Retrieving
    }

    public RobotState robotState = RobotState.Idle;
    protected Crate holdingCrate;
    
    public abstract void TransportCrate(Crate crate);
    public abstract void PickUpCrate();
    public abstract void DropDownCrate();
}