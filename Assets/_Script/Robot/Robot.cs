using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Script.Robot
{
    public enum RobotStateEnum
    {
        Redirecting,
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
        public Vector3 LastCellPosition;
        public Vector3 GoalCellPosition;
        protected LinkedList<GridXZCell> MovingPath;
        
        [Header("Movement")] 
        [SerializeField] protected float MovementSpeed = 1f;
        [SerializeField] protected float PreemptiveDistance = 0.05f;
        [SerializeField] protected float JamWaitTime = 5f;
        
        [Header("Crate ")]
        protected Crate HoldingCrate;

        #region AssignTask
        public abstract void IdleRedirect(Robot requestedRobot);
        
        public abstract void ApproachCrate(Crate crate);
        
        protected IEnumerator Jamming()
        {
            RobotStateEnum lastRobotStateEnum = RobotState;
            RobotState = RobotStateEnum.Jamming;
            yield return new WaitForSeconds(JamWaitTime);
            RobotState = lastRobotStateEnum;
        }

        #endregion

        #region Detection
        protected abstract void DetectNearByRobot();

        #endregion

        #region Movement
        protected void MoveAlongGrid()
        {
            if (RobotState is RobotStateEnum.Jamming or RobotStateEnum.Idle) return;
            transform.position = Vector3.MoveTowards(transform.position, NextCellPosition, MovementSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, NextCellPosition) <= PreemptiveDistance)
            {
                //PathFinding();
                GetNextCellInPath();
                PickUpCrate();
                DropDownCrate();
            }
        }
        
        public abstract void PickUpCrate();
        public abstract void DropDownCrate();
        
        protected void PlayerControl()
        {
            float horizontal = Input.GetAxisRaw("Horizontal"), vertical = Input.GetAxisRaw("Vertical");
            if (Mathf.Abs(horizontal) == 1f)
            {
                var item = CurrentGrid.GetItem(XIndex + (int)horizontal, ZIndex);
                // If walkable
                if (item != default(GridXZCell))
                {
                    XIndex += (int)horizontal;
                    NextCellPosition = CurrentGrid.GetWorldPosition(XIndex, ZIndex) +
                                       Vector3.up * transform.position.y;
                }
            }
            else if (Mathf.Abs(vertical) == 1f)
            {
                var item = CurrentGrid.GetItem(XIndex, ZIndex + (int)vertical);
                // If walkable
                if (item != default(GridXZCell))
                {
                    ZIndex += (int)vertical;
                    NextCellPosition = CurrentGrid.GetWorldPosition(XIndex, ZIndex) +
                                       Vector3.up * transform.position.y;
                }
            }
        }

        #endregion
        
        protected void GetNextCellInPath()
        {
            if (MovingPath == null ||MovingPath.Count == 0) return;
            var nextDestination = MovingPath.First.Value;
            MovingPath.RemoveFirst(); // the next standing node
        
            XIndex = nextDestination.XIndex;
            ZIndex = nextDestination.ZIndex;
            NextCellPosition = CurrentGrid.GetWorldPosition(XIndex, ZIndex) + Vector3.up * transform.position.y;

        }
        
        public GridXZCell GetCurrentGridCell()
        {
            return CurrentGrid.GetItem(XIndex, ZIndex);
        }
    }
}