using System;
using System.Collections;
using System.Collections.Generic;
using Shun_Grid_System;
using Shun_State_Machine;
using UnityEngine;

namespace _Script.Robot
{
    public enum RobotStateEnum
    {
        Idle,
        Delivering,
        Handling,
        Approaching,
        Jamming,
        Redirecting
    }
    
    public abstract class Robot : MonoBehaviour
    {
        [Header("Stat")] 
        private static int _idCount = 0;
        public int Id;
        
        [Header("Robot State Machine")]

        protected BaseStateMachine<RobotStateEnum> RobotStateMachine = new ();
        public RobotStateEnum CurrentRobotState => RobotStateMachine.GetState();
        protected IStateHistoryStrategy<RobotStateEnum> StateHistoryStrategy;
        
        [Header("Grid")]
        public Vector3 NextCellPosition;
        public Vector3 LastCellPosition;
        public bool IsBetween2Cells = true;
        protected internal GridXZ<CellItem> CurrentGrid;
        
        protected internal LinkedList<GridXZCell<CellItem>> MovingPath;
        
        [Header("Movement")] 
        public float MaxMovementSpeed = 1f;
        [SerializeField] protected float PreemptiveDistance = 0.05f;
        [SerializeField] protected float JamWaitTime = 5f;
        protected Coroutine JamCoroutine;
        
        [Header("Pathfinding and obstacle")]
        protected IPathfindingAlgorithm<GridXZ<CellItem>,GridXZCell<CellItem>, CellItem> PathfindingAlgorithm;
        protected List<Robot> NearbyRobots = new();
        
        [Header("Task ")]
        [SerializeField] protected Crate HoldingCrate;
        [SerializeField] public RobotTask CurrentTask;
        
        [Header("Components")] 
        protected Rigidbody Rigidbody;
        protected BoxCollider BoxCollider;
        
        
        [Header("Robot Detection")]
        [SerializeField] protected float BoxColliderSize = 0.9f;
        [SerializeField] protected float CastRadius = 1.5f;
        [SerializeField] protected LayerMask RobotLayerMask;
        
        #region INITIALIZE

        protected void Awake()
        {
            Id = _idCount;
            _idCount++;
        }
        
        private void Start()
        {
            InitializeGrid();
            InitializeStrategy();
            InitializeComponents();
            InitializeState();
        }

        private void FixedUpdate()
        {
            RobotStateMachine.ExecuteState();
        }

        private void InitializeGrid()
        {
            CurrentGrid = MapManager.Instance.WorldGrid;
            
            LastCellPosition = NextCellPosition = CurrentGrid.GetWorldPositionOfNearestCell(transform.position);
        }

        private void InitializeStrategy()
        {
            PathfindingAlgorithm = MapManager.Instance.GetPathFindingAlgorithm();
            StateHistoryStrategy = new RobotStateHistoryStrategy();
        }

        private void InitializeComponents()
        {
            Rigidbody = GetComponent<Rigidbody>();
            BoxCollider = GetComponent<BoxCollider>();
            
            BoxCollider.size = BoxColliderSize * Vector3.one;
        }
        
        private void InitializeState()
        {
            BaseState<RobotStateEnum> idleState = new(RobotStateEnum.Idle, null, null,
                (_, _) =>
                {
                    if (transform.position != LastCellPosition) 
                        RedirectToNearestCell();
                });
            
            BaseState<RobotStateEnum> approachingState = new(RobotStateEnum.Approaching,
                MovingStateExecute, null, AssignTask);
            
            BaseState<RobotStateEnum> retrievingState = new(RobotStateEnum.Handling, null, null, AssignTask);
            
            BaseState<RobotStateEnum> deliveringState = new(RobotStateEnum.Delivering,
                MovingStateExecute, null, AssignTask);
            
            BaseState<RobotStateEnum> jammingState = new(RobotStateEnum.Jamming, null, null, AssignTask);
            
            BaseState<RobotStateEnum> redirectingState = new(RobotStateEnum.Redirecting,
                MovingStateExecute, null, AssignTask);
            
            RobotStateMachine.AddState(idleState);
            RobotStateMachine.AddState(approachingState);
            RobotStateMachine.AddState(retrievingState);
            RobotStateMachine.AddState(deliveringState);
            RobotStateMachine.AddState(jammingState);
            RobotStateMachine.AddState(redirectingState);
            
            //RobotStateMachine.CurrentBaseState = idleState;
            RobotStateMachine.StateHistoryStrategy = StateHistoryStrategy;
        }

        #endregion

        #region StateEvents

        private void MovingStateExecute(RobotStateEnum currentState, IStateParameter enterParameters)
        {
            if (!CheckArriveCell()) return; // change state during executing this function
            
            DetectNearByRobot();
            if (!DecideFromRobotDetection()) return; // change state during executing this function

            MoveAlongGrid();
        }

        private void AssignTask(RobotStateEnum lastRobotState, IStateParameter enterParameters)
        {
            if (enterParameters == null) return;
            
            CurrentTask = enterParameters as RobotTask;
            if (CurrentTask == null) return;
            
            switch (CurrentTask.StartCellPosition)
            {
                case RobotTask.StartPosition.LastCell:
                    CreateInitialPath(LastCellPosition, CurrentTask.GoalCellPosition);
                    ExtractNextCellInPath();
                    break;
                
                case RobotTask.StartPosition.NextCell:
                    CreateInitialPath(NextCellPosition, CurrentTask.GoalCellPosition);
                    MovingPath.RemoveFirst();
                    break;
                
                case RobotTask.StartPosition.NearestCell:
                    Vector3 nearestCellPosition = CurrentGrid.GetWorldPositionOfNearestCell(transform.position);

                    if (nearestCellPosition == LastCellPosition)
                    {
                        CreateInitialPath(nearestCellPosition, CurrentTask.GoalCellPosition);
                        ExtractNextCellInPath();
                    }
                    else if (nearestCellPosition == NextCellPosition)
                    {
                        if(CreateInitialPath(NextCellPosition, CurrentTask.GoalCellPosition))
                            MovingPath.RemoveFirst();
                    }
                    else
                    {
                        Debug.LogError( gameObject.name+" THE NEAREST CELL IS NOT LAST OR NEXT CELL "+ nearestCellPosition);
                    }
                    
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            CheckArriveCell();
        }
        

        #endregion
        
        /// <summary>
        /// Using the Template Method Pattern to store the function that the different type of robot can override for its own implementation
        /// </summary>
        /// <param name="requestedRobot"></param>

        #region TASK_FUNCTIONS
        
        protected abstract void RedirectToNearestCell();        
        public abstract bool RedirectToOrthogonalCell(Robot requestedRobot, Vector3 exceptionPosition);
        public abstract void ApproachCrate(Crate crate);

        protected void SetToJam()
        {
            if (JamCoroutine != null) StopCoroutine(JamCoroutine);

            if (LastCellPosition == transform.position)
            {
                NextCellPosition = LastCellPosition;
                IsBetween2Cells = false;
            }

            if (NextCellPosition == transform.position)
            {
                LastCellPosition = NextCellPosition;
                IsBetween2Cells = false;
            }
            
            JamCoroutine = StartCoroutine(nameof(Jamming));
        }
        
        protected IEnumerator Jamming()
        {
            RobotStateMachine.SetToState(RobotStateEnum.Jamming);

            yield return new WaitForSeconds(JamWaitTime);
                
            RobotStateMachine.RestoreState();
            
        }
        
        protected void ArriveCrateSource()
        {
            StartCoroutine(nameof(PullingUp));
        }
        
        protected IEnumerator PullingUp()
        {
            RobotStateMachine.SetToState(RobotStateEnum.Handling);

            yield return new WaitForSeconds(HoldingCrate.PickUpTime);
            
            
            HoldingCrate.transform.SetParent(transform);
            HoldingCrate.PickUp();
            
            
            var goalCellPosition = CurrentGrid.GetWorldPositionOfNearestCell(HoldingCrate.DropDownIndexX, HoldingCrate.DropDownIndexZ);
        
            RobotTask robotTask = new RobotTask(RobotTask.StartPosition.NextCell, goalCellPosition, ArriveCrateDestination, 0);
        
            RobotStateMachine.SetToState(RobotStateEnum.Delivering, CurrentTask, robotTask);
            
        }
        
        protected void ArriveCrateDestination()
        {
            DistributionManager.Instance.ArriveDestination(this, HoldingCrate);
            
            StartCoroutine(nameof(DroppingDown));
        }

        protected IEnumerator DroppingDown()
        {
            RobotStateMachine.SetToState(RobotStateEnum.Handling);

            yield return new WaitForSeconds(HoldingCrate.DropDownTime);
            
            
            Destroy(HoldingCrate.gameObject);
            HoldingCrate = null;
            
            RobotStateMachine.SetToState(RobotStateEnum.Idle, CurrentTask);
        }

        #endregion

        #region MOVEMENT
        
        protected abstract bool DecideFromRobotDetection();
        
        protected void MoveAlongGrid()
        {
            // Move
            transform.position = Vector3.MoveTowards(transform.position, NextCellPosition, MaxMovementSpeed * Time.fixedDeltaTime);
            Rigidbody.velocity = Vector3.zero;

            IsBetween2Cells = true;
        }

        bool CheckArriveCell()
        {
            if (Vector3.Distance(transform.position, NextCellPosition) != 0) return true;
            
            IsBetween2Cells = false;
            if (CurrentTask != null &&
                CurrentGrid.GetIndex(transform.position) == CurrentGrid.GetIndex(CurrentTask.GoalCellPosition))
            {
                CurrentTask.GoalArrivalAction?.Invoke();
                ExtractNextCellInPath();
                return false;
            }
                
            ExtractNextCellInPath();
            return true;
        }

        #endregion
        
        #region PATHS_AND_CELLS
        
        /// <summary>
        /// Using Iteration Pattern for getting the cell that the robot travelling when getting form the PathFinding Algorithm
        /// NextCellPosition : the next cell the robot going to travel to
        /// LastCellPosition : the cell that the robot is leaving
        /// This guarantee the robot is between the LastCellPosition and NextCellPosition, which is next to each other
        /// </summary>
        /// <returns></returns>
        protected void ExtractNextCellInPath()
        {
            if (MovingPath == null || MovingPath.Count == 0)
            {
                LastCellPosition = NextCellPosition;
                return;
            }
            var nextNextCell = MovingPath.First.Value;
            MovingPath.RemoveFirst(); // the next standing node

            Vector3 nextNextCellPosition = CurrentGrid.GetWorldPositionOfNearestCell(nextNextCell.XIndex, nextNextCell.YIndex);

            LastCellPosition = NextCellPosition;
            NextCellPosition = nextNextCellPosition;
            //Debug.Log(gameObject.name + " Get Next Cell " + NextCellPosition);
        }

        protected abstract bool UpdateInitialPath(List<GridXZCell<CellItem>> dynamicObstacle);
        protected abstract bool CreateInitialPath(Vector3 startPosition, Vector3 endPosition);

        
        #endregion

        #region COLLIDERS
        protected abstract void DetectNearByRobot();
        
        private void OnCollisionEnter(Collision other)
        {
            DebugUIManager.Instance.AddCollision();
            //Debug.Log($"{gameObject.name} Collide with {other.gameObject.name}");
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPaused = true;
            #endif
        }
        
        #endregion

    }
}