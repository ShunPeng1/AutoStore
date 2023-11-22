using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Shun_Grid_System;
using Shun_State_Machine;
using Shun_Unity_Editor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Script.Robot
{

    public abstract partial class Robot : MonoBehaviour
    {
        [Header("Stat")] 
        private static int _idCount = 0;
        public int Id;

        [Header("Robot State Machine")] 
        [ShowImmutable, SerializeField] protected BaseStateMachine<RobotStateEnum> RobotStateMachine = new ();
        public RobotStateEnum CurrentRobotState => RobotStateMachine.GetState();
        private IStateHistoryStrategy<RobotStateEnum> _stateHistoryStrategy;
        
        [Header("Grid")]
        protected internal GridXZ<CellItem> CurrentGrid;
        public GridXZCell<CellItem> NextCell;
        public GridXZCell<CellItem> LastCell;
        
        public Vector3 NextCellPosition => CurrentGrid.GetWorldPositionOfNearestCell(NextCell);
        public Vector3 LastCellPosition => CurrentGrid.GetWorldPositionOfNearestCell(LastCell);
        public Vector3 GoalCellPosition => CurrentGrid.GetWorldPositionOfNearestCell(MovingPath.Last.Value);
        
        public bool IsMidwayMove = true;
        public LinkedList<GridXZCell<CellItem>> MovingPath;
        
        [Header("Movement")] 
        public float MaxMovementSpeed = 1f;
        
        private IPathfindingAlgorithm<GridXZ<CellItem>,GridXZCell<CellItem>, CellItem> _pathfindingAlgorithm;
         
        protected List<Robot> NearbyRobots = new();

        [Header("Task ")] 
        [ShowImmutable, SerializeField] protected BinTransportTask CurrentBinTransportTask; 
        [ShowImmutable, SerializeField] protected Bin HoldingBin;
        
        [Header("Cable and Hook Transform")]
        [SerializeField] protected Transform CableTransform;
        [SerializeField] protected Transform HookTransform;    
        [SerializeField] protected Transform TopHookCeilingTransform;
        [SerializeField] protected Transform BinHookPlaceTransform;


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
            
            LastCell = NextCell = CurrentGrid.GetCell(transform.position);
        }

        private void InitializeStrategy()
        {
            _pathfindingAlgorithm = MapManager.Instance.GetPathFindingAlgorithm();
            _stateHistoryStrategy = new RobotStateHistoryStrategy();
        }

        protected abstract void InitializeComponents();
        
        private void InitializeState()
        {
            RobotIdlingState idlingState = new(this, RobotStateEnum.Idling);
            
            RobotMovingState approachingState = new(this, RobotStateEnum.Approaching);
            
            RobotHandlingState handlingState = new(this,RobotStateEnum.Handling);
            
            RobotMovingState deliveringState = new(this,RobotStateEnum.Delivering);
            
            RobotJammingState jammingState = new(this,RobotStateEnum.Jamming);
            
            RobotMovingState redirectingState = new(this,RobotStateEnum.Redirecting);
            
            RobotStateMachine.AddState(idlingState);
            RobotStateMachine.AddState(approachingState);
            RobotStateMachine.AddState(handlingState);
            RobotStateMachine.AddState(deliveringState);
            RobotStateMachine.AddState(jammingState);
            RobotStateMachine.AddState(redirectingState);
            
            //RobotStateMachine.CurrentBaseState = idleState;
            RobotStateMachine.SetHistoryStrategy(_stateHistoryStrategy);
        }

        #endregion

        /// <summary>
        /// Using the Template Method Pattern to store the function that the different type of robot can override for its own implementation
        /// </summary>
        /// <param name="requestedRobot"></param>

        #region REDIRECT_FUNCTIONS
        protected void RedirectToNearestCell()
        {
            Vector3 nearestCellPosition = CurrentGrid.GetWorldPositionOfNearestCell(transform.position);
        
            //Debug.Log( gameObject.name+ " Redirect To Nearest Cell " + nearestCellPosition);
            
            RobotMovingTask robotMovingTask = new RobotMovingTask(RobotMovingTask.StartPosition.NearestCell, nearestCellPosition, SetToJam);
            RobotStateMachine.SetToState(RobotStateEnum.Redirecting, null , robotMovingTask );
        }

        public bool RedirectRequest(Robot requestedRobot, Vector3 requestedRobotNextCellPosition, Vector3 requestedRobotGoalCellPosition)
        {
            
            //bool result = RedirectToOrthogonalCell(requestedRobot, requestedRobotNextCellPosition, requestedRobotGoalCellPosition);

            // TODO : test this function
            bool result = RedirectToLowestWeightCell(requestedRobot, requestedRobotNextCellPosition, requestedRobotGoalCellPosition);
            
            if (!result)
            {
                RedirectToNearestCell(); // Redirect to fit the cell and wait 
            }

            return result;

        }
        
        
        /// <summary>
        /// This function will be requested when the robot is Idle, or standing on others goal. To move to a other direction
        /// The direction is right, left, backward, prefer mostly the direction which is not blocking
        /// </summary>
        private bool RedirectToOrthogonalCell(Robot requestedRobot, Vector3 requestedRobotNextCellPosition, Vector3 requestedRobotGoalCellPosition)
        {
            if (CurrentRobotState == RobotStateEnum.Redirecting)
            {
                return true;  // Cannot redirect twice, but this is already redirecting
            }
            
            // Calculate the direction
            Vector3 requestedRobotDistance = (CurrentGrid.GetWorldPositionOfNearestCell(requestedRobot.transform.position) - CurrentGrid.GetWorldPositionOfNearestCell(transform.position)).normalized;
            
            Vector3 roundDirection = new Vector3(
                Mathf.FloorToInt(Mathf.Abs(requestedRobotDistance.x)), // -1 or 1, or 0 when -1<x<1 
                0,
                Mathf.Sign(requestedRobotDistance.z) * Mathf.CeilToInt(Mathf.Abs(requestedRobotDistance.z)) // 0 or -1 when -1<=z<0 or -1 when 0<z<=1
            );
            Vector3 orthogonalDirection = Vector3.Cross(Vector3.up, roundDirection).normalized; // find the orthogonal vector
            
            
            // Check validity and detect obstacles for redirecting right, left, and backward
            bool goRightValid = IsValidRedirectPosition(orthogonalDirection * -1, requestedRobotDistance, requestedRobotNextCellPosition, out Vector3 redirectRightGoalCellPosition, out bool isBlockRight);
            bool goLeftValid = IsValidRedirectPosition(orthogonalDirection, requestedRobotDistance, requestedRobotNextCellPosition, out Vector3 redirectLeftGoalCellPosition, out bool isBlockLeft);
            bool goBackwardValid = IsValidRedirectPosition(roundDirection * -1, requestedRobotDistance, requestedRobotNextCellPosition, out Vector3 redirectBackwardGoalCellPosition, out bool isBlockBackward); 
            bool goForwardValid = IsValidRedirectPosition(roundDirection, requestedRobotDistance, requestedRobotNextCellPosition, out Vector3 redirectForwardGoalCellPosition, out bool isBlockForward);
            
            // Determine the final redirect goal position based on validity and obstacles
            Vector3 redirectGoalCellPosition;
            List<Vector3> potentialRedirectGoalCells = new();
            
            // Select randomly no blocking redirect goal
            if (goRightValid && ! isBlockRight)
                potentialRedirectGoalCells.Add(redirectRightGoalCellPosition); 
            if (goLeftValid && ! isBlockLeft)
                potentialRedirectGoalCells.Add(redirectLeftGoalCellPosition);
            if (goBackwardValid && ! isBlockBackward)
                potentialRedirectGoalCells.Add(redirectBackwardGoalCellPosition);
            if (goForwardValid && ! isBlockForward)
                potentialRedirectGoalCells.Add(redirectForwardGoalCellPosition);
            
            if (potentialRedirectGoalCells.Count != 0) // There is a non-blocking redirect goal
            {
                redirectGoalCellPosition = potentialRedirectGoalCells[Random.Range(0, potentialRedirectGoalCells.Count)];
            }
            else // All redirect goal are block, randomly choose a path that is valid and may redirect other if needed
            {
                if (goRightValid) potentialRedirectGoalCells.Add(redirectRightGoalCellPosition);
                if (goLeftValid) potentialRedirectGoalCells.Add( redirectLeftGoalCellPosition);
                if (goBackwardValid) potentialRedirectGoalCells.Add( redirectBackwardGoalCellPosition);
                if (goForwardValid) potentialRedirectGoalCells.Add(redirectForwardGoalCellPosition);
                
                if (potentialRedirectGoalCells.Count == 0) // No valid path was found either (usually at corner)
                {
                    RedirectToNearestCell(); // Redirect to fit the cell and wait 
                    return false;
                }

                redirectGoalCellPosition = potentialRedirectGoalCells[Random.Range(0, potentialRedirectGoalCells.Count)];
            }

            //Debug.Log(requestedRobot.gameObject.name + " requested to move " + gameObject.name + " from " + CurrentGrid.GetIndex(transform.position) + " to " + CurrentGrid.GetIndex(redirectGoalCellPosition));
            RobotMovingTask robotMovingTask = new RobotMovingTask(RobotMovingTask.StartPosition.NearestCell, redirectGoalCellPosition, SetToJam);
            RobotStateMachine.SetToState(RobotStateEnum.Redirecting, null, robotMovingTask );
            return true;
        }
        
        /// <summary>
        /// This function will be requested when the robot is Idle, or standing on others goal. To move to a other direction
        /// The direction is right, left, backward, prefer mostly the direction which is not blocking
        /// </summary>
        public bool RedirectToLowestWeightCell(Robot requestedRobot, Vector3 requestedRobotNextCellPosition, Vector3 requestedRobotGoalCellPosition)
        {
            if (CurrentRobotState == RobotStateEnum.Redirecting)
            {
                return true;  // Cannot redirect twice, but this is already redirecting
            }

            Dictionary<GridXZCell<CellItem>, double> weightCellToCosts = new();
            double weight = 999;
            
            foreach (GridXZCell<CellItem> cell in requestedRobot.MovingPath)
            {
                weightCellToCosts[cell] = weight;
            }
            
            weightCellToCosts.Add(requestedRobot.NextCell, weight);
            weightCellToCosts.Add(requestedRobot.LastCell, weight);
            
            GridXZCell<CellItem> redirectCell = _pathfindingAlgorithm.LowestCostCellWithWeightMap(CurrentGrid.GetCell(transform.position), weightCellToCosts);
            
            Debug.Log(requestedRobot.gameObject.name + " requested to move " + gameObject.name + " from " + CurrentGrid.GetIndex(transform.position) + " to " + CurrentGrid.GetIndex(CurrentGrid.GetWorldPositionOfNearestCell(redirectCell)));
            RobotMovingTask robotMovingTask = new RobotMovingTask(RobotMovingTask.StartPosition.NearestCell,CurrentGrid.GetWorldPositionOfNearestCell(redirectCell) , SetToJam);
            RobotStateMachine.SetToState(RobotStateEnum.Redirecting, null, robotMovingTask );
            return true;
        }
        
        private bool IsValidRedirectPosition(Vector3 direction, Vector3 exceptDirection, Vector3 detectedRobotGoalPosition, out Vector3 redirectGoalCellPosition, out bool isBlockAhead)
        {
            var redirectIndex = CurrentGrid.GetIndex(transform.position + direction * 1);
            redirectGoalCellPosition = CurrentGrid.GetWorldPositionOfNearestCell(redirectIndex.x, redirectIndex.y);
            isBlockAhead = false;
            
            foreach (var nearbyRobot in NearbyRobots)
            {
                bool isBlockingGoal = RobotUtility.CheckRobotBlockAHead(nearbyRobot, redirectGoalCellPosition);
                if (isBlockingGoal)
                {
                    switch (nearbyRobot.CurrentRobotState)
                    {
                        case RobotStateEnum.Idling:
                            isBlockAhead = false;
                            break;
                        case RobotStateEnum.Handling:
                            isBlockAhead = true;
                            return false;
                        case RobotStateEnum.Delivering:
                        case RobotStateEnum.Approaching:
                        case RobotStateEnum.Jamming:
                        case RobotStateEnum.Redirecting:
                        default:
                            isBlockAhead = true;
                            break;
                    }
                }
            }
            
            return CurrentGrid.CheckValidCell(redirectIndex.x, redirectIndex.y) && exceptDirection != direction && detectedRobotGoalPosition != redirectGoalCellPosition;
        }

        #endregion

        #region HANDLE_FUNCTIONS

        protected abstract void ExtendCable(Action finishCallback);

        protected abstract void ContractCable(Action finishCallback);

        protected abstract void MoveCable();
        

        #endregion
        
        #region TASK_FUNCTIONS

        
        public void ApproachBin(BinTransportTask binTransportTask)
        {
            CurrentBinTransportTask = binTransportTask;

            Vector3 goalCellPosition = CurrentGrid.GetWorldPositionOfNearestCell( CurrentBinTransportTask.TargetBinSource );
            RobotMovingTask robotMovingTask = new RobotMovingTask(RobotMovingTask.StartPosition.NextCell, goalCellPosition, ArriveBinSource, 0);
        
            RobotStateMachine.SetToState(RobotStateEnum.Approaching, null, robotMovingTask);
        }


        protected void SetToJam()
        {
            RobotStateMachine.SetToState(RobotStateEnum.Jamming);
        }
        
        
        protected void ArriveBinSource()
        {
            RobotStateMachine.SetToState(RobotStateEnum.Handling);
        }
        
        
        protected void ArriveBinDestination()
        {
            DistributionManager.Instance.ArriveDestination(this, CurrentBinTransportTask);
            
            RobotStateMachine.SetToState(RobotStateEnum.Handling);
        }
        

        #endregion

        #region MOVEMENT
        protected virtual void MoveAlongGrid()
        {
            // Move
            transform.position = Vector3.MoveTowards(transform.position, NextCellPosition, MaxMovementSpeed * Time.fixedDeltaTime);
            
            IsMidwayMove = true;
        }
        
        
        #endregion
        

        #region DETECTION_AND_COLLIDERS
        protected abstract void DetectNearByRobot();

        protected abstract bool CheckRobotSafeDistance(Robot checkRobot);
        
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