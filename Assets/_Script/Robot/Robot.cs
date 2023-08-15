using System;
using System.Collections;
using System.Collections.Generic;
using Shun_Grid_System;
using Shun_State_Machine;
using UnityEngine;

namespace _Script.Robot
{

    public abstract partial class Robot : MonoBehaviour
    {
        [Header("Stat")] 
        private static int _idCount = 0;
        public int Id;
        
        [Header("Robot State Machine")]
        public RobotStateEnum CurrentRobotState;
        protected readonly BaseStateMachine<RobotStateEnum> RobotStateMachine = new ();
        private IStateHistoryStrategy<RobotStateEnum> _stateHistoryStrategy;
        
        [Header("Grid")]
        protected internal GridXZ<CellItem> CurrentGrid;

        
        public GridXZCell<CellItem> NextCell;
        public GridXZCell<CellItem> LastCell;
        public Vector3 NextCellPosition => CurrentGrid.GetWorldPositionOfNearestCell(NextCell);
        public Vector3 LastCellPosition => CurrentGrid.GetWorldPositionOfNearestCell(LastCell);
        public bool IsMidwayMove = true;
        protected internal LinkedList<GridXZCell<CellItem>> MovingPath;
        
        [Header("Movement")] 
        public float MaxMovementSpeed = 1f;
        [SerializeField] protected float JamWaitTime = 5f;
        
        [Header("Pathfinding and obstacle")] 
        private IPathfindingAlgorithm<GridXZ<CellItem>,GridXZCell<CellItem>, CellItem> _pathfindingAlgorithm;
        protected List<Robot> NearbyRobots = new();
        
        [Header("Task ")]
        [SerializeField] protected Crate HoldingCrate;
        
        
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
            CurrentRobotState = RobotStateMachine.GetState();
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

        private void InitializeComponents()
        {
            Rigidbody = GetComponent<Rigidbody>();
            BoxCollider = GetComponent<BoxCollider>();
            
            BoxCollider.size = BoxColliderSize * Vector3.one;
        }
        
        private void InitializeState()
        {
            RobotIdlingState idlingState = new(this, RobotStateEnum.Idling);
            
            RobotMovingState approachingState = new(this, RobotStateEnum.Approaching);
            
            RobotState handlingState = new(this,RobotStateEnum.Handling );
            
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
            RobotStateMachine.StateHistoryStrategy = _stateHistoryStrategy;
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
            RobotStateMachine.SetToState(RobotStateEnum.Jamming);
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
        
            RobotStateMachine.SetToState(RobotStateEnum.Delivering, null, robotTask);
            
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
            
            RobotStateMachine.SetToState(RobotStateEnum.Idling);
        }

        #endregion

        #region MOVEMENT
        protected virtual void MoveAlongGrid()
        {
            // Move
            transform.position = Vector3.MoveTowards(transform.position, NextCellPosition, MaxMovementSpeed * Time.fixedDeltaTime);
            Rigidbody.velocity = Vector3.zero;

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