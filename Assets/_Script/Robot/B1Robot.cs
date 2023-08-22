using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Script.Robot;
using DG.Tweening;
using Shun_Grid_System;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class B1Robot : Robot
{
    [Header("Components")]
    protected Rigidbody Rigidbody;
    protected Collider BoxCollider;
    
    [Header("Robot Detection")]
    [SerializeField] protected float BoxColliderSize = 0.9f;
    [SerializeField] protected float CastRadius = 1.5f;
    [SerializeField] protected LayerMask RobotLayerMask;

    private float _minBlockAheadAngle => Mathf.Atan((CastRadius + BoxColliderSize/2)/(0.5f + BoxColliderSize/2)) * Mathf.PI;
    private float _maxBlockAheadAngle = 45f;

    [Header("Robot Hook and Cable")] 
    [SerializeField] protected Transform CableTransform;
    [SerializeField] protected Transform HookTransform;    
    [SerializeField] protected Transform TopHookCeilingTransform;
    [SerializeField] protected Transform BinHookPlaceTransform;
    [SerializeField] protected float HookMoveSpeed = 2f;
    [SerializeField] protected Ease HookMoveEase = Ease.InOutCubic;
    [SerializeField] private float _cableLengthMultiply = 3.8f; 
    private Vector3 _cableInitialScale;
    
    protected override void InitializeComponents()
    {
        Rigidbody = GetComponent<Rigidbody>();
        BoxCollider = GetComponent<Collider>();

        _cableInitialScale = CableTransform.localScale;
    }


    #region DETECTION
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, CastRadius);
    }


    protected override void DetectNearByRobot()
    {
        NearbyRobots = new List<Robot>();

        var colliders = Physics.OverlapSphere(transform.position, CastRadius, RobotLayerMask);
        foreach (var colliderHit in colliders)
        {
            Robot detectedRobot = colliderHit.gameObject.GetComponent<Robot>();
            if (detectedRobot == null || detectedRobot == this) continue;
            NearbyRobots.Add(detectedRobot);
        }
    }

    protected override bool CheckRobotSafeDistance(Robot checkRobot)
    {
        return Vector3.Distance(transform.position, checkRobot.transform.position) <= CastRadius ;
    }

    
    #endregion

    
    
    #region HANDLE_FUNCTIONS
    protected override void ExtendCable()
    {
        var item = CurrentGrid.GetCell(transform.position).Item;
        
        Sequence sequence = DOTween.Sequence();
        
        if (HoldingBin == null)
        {
            var topStackPosition = item.GetTopBinWorldPosition() + (HookTransform.position - BinHookPlaceTransform.position);
            var hookMoveDuration = Mathf.Abs(topStackPosition.magnitude - TopHookCeilingTransform.position.magnitude) / HookMoveSpeed;

            sequence.Join( HookTransform.DOMove(topStackPosition, hookMoveDuration).SetEase(HookMoveEase)) ;
            sequence.Join(DOTween.To(SetCablePosition, 0,1,hookMoveDuration));
            
            sequence.AppendCallback(HookBin);
        }
        else
        {
            var topStackPosition = item.GetTopStackWorldPosition() + (HookTransform.position - BinHookPlaceTransform.position);
            var hookMoveDuration = Mathf.Abs(topStackPosition.magnitude - TopHookCeilingTransform.position.magnitude) / HookMoveSpeed;

            sequence.Join( HookTransform.DOMove(topStackPosition, hookMoveDuration).SetEase(HookMoveEase)) ;
            sequence.Join(DOTween.To(SetCablePosition, 0,1,hookMoveDuration));
            
            sequence.AppendCallback(UnhookBin);
        }

        sequence.AppendCallback(ContractCable);
    }

    protected override void ContractCable()
    {
        var item = CurrentGrid.GetCell(transform.position).Item;
        var hookMoveDuration =  Mathf.Abs(HookTransform.position.magnitude - TopHookCeilingTransform.position.magnitude) / HookMoveSpeed;
        
        Sequence sequence = DOTween.Sequence();
        sequence.Join( HookTransform.DOMove(TopHookCeilingTransform.position, hookMoveDuration).SetEase(HookMoveEase)) ;
        sequence.Join(DOTween.To(SetCablePosition, 0,1,hookMoveDuration));
        
        
        if (HoldingBin == null)
        {
            sequence.AppendCallback(SetHandlingToIdlingState);
        }
        else
        {
            sequence.AppendCallback(SetHandlingToDeliveringState);
        }

        
    }

    private void HookBin()
    {
        var item = CurrentGrid.GetCell(transform.position).Item;
        HoldingBin = item.RemoveTopBinFromStack();
        
        HoldingBin.transform.SetParent(BinHookPlaceTransform);
        HoldingBin.transform.localPosition = Vector3.zero;
    }


    private void UnhookBin()
    {
        var item = CurrentGrid.GetCell(transform.position).Item;
        //item.AddToStack(HoldingBin);
        HoldingBin.transform.parent = null;
        HoldingBin = null;
    }

    private void SetHandlingToIdlingState()
    {
        CurrentBinTransportTask = null;
        RobotStateMachine.SetToState(RobotStateEnum.Idling);
    }

    private void SetHandlingToDeliveringState()
    {
        CurrentBinTransportTask.PickUpBin(HoldingBin);

        var goalCellPosition = CurrentGrid.GetWorldPositionOfNearestCell(CurrentBinTransportTask.TargetBinDestination);
        
        RobotMovingTask robotMovingTask = new RobotMovingTask(RobotMovingTask.StartPosition.NextCell, goalCellPosition, ArriveBinDestination, 0);
        
        RobotStateMachine.SetToState(RobotStateEnum.Delivering, null, robotMovingTask);

    }

    public override void SetCablePosition(float unused)
    {
        float distance = Vector3.Distance(TopHookCeilingTransform.position, HookTransform.position);

        CableTransform.localScale = new Vector3(_cableInitialScale.x, _cableInitialScale.y, distance * _cableLengthMultiply);
        //CableTransform.position = (TopHookCeilingTransform.position + HookTransform.position )/2f;
    }
    
    #endregion
}
