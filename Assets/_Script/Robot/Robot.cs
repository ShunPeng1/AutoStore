using System.Collections.Generic;
using UnityEngine;

namespace _Script.Robot
{
    public enum RobotStateEnum
    {
        Idle,
        Delivering,
        Retrieving,
        Approaching,
        Jamming
    }
    
    public abstract class Robot : MonoBehaviour
    {
        [Header("Stat")] 
        public int Id;
        public RobotStateEnum RobotState = RobotStateEnum.Idle;
    
        [Header("Grid")]
        protected GridXZ<GridXZCell> CurrentGrid;
        protected int XIndex, ZIndex;
        public Vector3 NextCellPosition;
        public Vector3 GoalCellPosition;
        protected LinkedList<GridXZCell> MovingPath;
        
        [Header("Movement")] 
        [SerializeField] protected float MovementSpeed = 1f;
        [SerializeField] protected float PreemptiveDistance = 0.05f;


        [Header("PathFinding")] [SerializeField]
        protected LineRenderer DebugLineRenderer;

        
        [Header("Crate ")]
        protected Crate HoldingCrate;
        
        
        public abstract void ApproachCrate(Crate crate);
        public abstract void PickUpCrate();
        public abstract void DropDownCrate();

        public GridXZCell GetCurrentGridCell()
        {
            return CurrentGrid.GetItem(XIndex, ZIndex);
        }
    }
}