using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Robot : MonoBehaviour
{
    public enum RobotState
    {
        Idle,
        Deliver,
        Reorganize
    }

    public RobotState robotState = RobotState.Idle;

    
}
