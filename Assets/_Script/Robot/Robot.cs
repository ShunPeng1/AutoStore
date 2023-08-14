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

        protected BaseStateMachine<RobotStateEnum> RobotStateMachine = new ();

        public RobotStateEnum CurrentRobotState;
        protected IStateHistoryStrategy<RobotStateEnum> StateHistoryStrategy;
        
        [Header("Grid")]
        public Vector3 NextCellPosition;
        public Vector3 LastCellPosition;
        public bool IsMidwayMove = true;
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
            CurrentRobotState = RobotStateMachine.GetState();
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
            RobotState idleState = new(this, RobotStateEnum.Idle, null, null,
                (_, _) =>
                {
                    if (transform.position != LastCellPosition) 
                        RedirectToNearestCell();
                });
            
            RobotMovingState approachingState = new(this, RobotStateEnum.Approaching);
            
            RobotState handlingState = new(this,RobotStateEnum.Handling );
            
            RobotMovingState deliveringState = new(this,RobotStateEnum.Delivering);
            
            RobotState jammingState = new(this,RobotStateEnum.Jamming);
            
            RobotMovingState redirectingState = new(this,RobotStateEnum.Redirecting);
            
            RobotStateMachine.AddState(idleState);
            RobotStateMachine.AddState(approachingState);
            RobotStateMachine.AddState(handlingState);
            RobotStateMachine.AddState(deliveringState);
            RobotStateMachine.AddState(jammingState);
            RobotStateMachine.AddState(redirectingState);
            
            //RobotStateMachine.CurrentBaseState = idleState;
            RobotStateMachine.StateHistoryStrategy = StateHistoryStrategy;
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
                IsMidwayMove = false;
            }

            if (NextCellPosition == transform.position)
            {
                LastCellPosition = NextCellPosition;
                IsMidwayMove = false;
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
            
            RobotStateMachine.SetToState(RobotStateEnum.Idle, CurrentTask);
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