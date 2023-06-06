using System;
using System.Collections;
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
        Jamming,
        Redirecting
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
        public Vector3 RedirectCellPosition;
        protected LinkedList<GridXZCell> MovingPath;
        
        [Header("Movement")] 
        [SerializeField] protected float MaxMovementSpeed = 1f;
        [SerializeField] protected float PreemptiveDistance = 0.05f;
        [SerializeField] protected float JamWaitTime = 5f;
        
        
        [Header("Crate ")] 
        protected Queue<Func<IEnumerator>>  ArrivalDestinationFuncs = new ();

        protected List<int> test;
        protected Crate HoldingCrate;

        [Header("Component")] 
        protected Rigidbody Rigidbody;

        
        IEnumerator Start()
        {
            GoalCellPosition = LastCellPosition = NextCellPosition = transform.position;
            yield return null;
            CurrentGrid = MapManager.Instance.StorageGrid;
            (XIndex, ZIndex) = CurrentGrid.GetXZ(transform.position);

            Rigidbody = GetComponent<Rigidbody>();
        }
        
        
        #region AssignTask
        
        protected IEnumerator BecomeIdle()
        {
            RobotState = RobotStateEnum.Idle;
            yield break;
        }
        public abstract void RedirectOrthogonal(Robot requestedRobot);
        
        public abstract void ApproachCrate(Crate crate);
        
        protected abstract IEnumerator PickUpCrate();
        protected abstract IEnumerator DropDownCrate();
        
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
            
            // Move Toward
            //Vector3 newPosition = Vector3.MoveTowards(transform.position, NextCellPosition, MaxMovementSpeed * Time.fixedDeltaTime);
            //Rigidbody.MovePosition(newPosition);
            
            transform.position = Vector3.MoveTowards(transform.position, NextCellPosition, MaxMovementSpeed * Time.fixedDeltaTime);

            // Check Cell
            if (Vector3.Distance(transform.position, NextCellPosition) <= PreemptiveDistance)
            {
                StartCoroutine(nameof(ArriveDestination));
                ExtractNextCellInPath();
            }
        }
        
        public IEnumerator ArriveDestination()
        {
            if (CurrentGrid.GetXZ(transform.position) != CurrentGrid.GetXZ(GoalCellPosition) ||
                ArrivalDestinationFuncs.Count == 0) yield break;
            
            var arrivalDestinationFunc = ArrivalDestinationFuncs.Dequeue();
            StartCoroutine(arrivalDestinationFunc.Invoke());

        }

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

        protected void ExtractNextCellInPath()
        {
            if (MovingPath == null ||MovingPath.Count == 0) return;
            var nextDestination = MovingPath.First.Value;
            MovingPath.RemoveFirst(); // the next standing node
            
            XIndex = nextDestination.XIndex;
            ZIndex = nextDestination.ZIndex;
            LastCellPosition = NextCellPosition;
            NextCellPosition = CurrentGrid.GetWorldPosition(XIndex, ZIndex) + Vector3.up * transform.position.y;
            //Debug.Log(gameObject.name + " Get Next Cell " + NextCellPosition);
        }
        
        #endregion
        
        
        
        public GridXZCell GetCurrentGridCell()
        {
            return CurrentGrid.GetItem(XIndex, ZIndex);
        }
    }
}