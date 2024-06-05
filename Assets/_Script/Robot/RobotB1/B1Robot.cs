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
    protected override void ExtendCable(Action finishCallback)
    {
        var item = CurrentGrid.GetCell(transform.position).Item;
        
        Sequence sequence = DOTween.Sequence();
        
        Vector3 topStackPosition = (HoldingBin == null ? item.GetTopBinWorldPosition() : item.GetTopStackWorldPosition()) 
                                   + (HookTransform.position - BinHookPlaceTransform.position);
        
        var hookMoveDistance = Mathf.Abs(topStackPosition.y - TopHookCeilingTransform.position.y);
        var hookMoveDuration = hookMoveDistance / HookMoveSpeed;

        sequence.Join( HookTransform.DOMove(topStackPosition, hookMoveDuration).SetEase(HookMoveEase)) ;
        
        sequence.AppendCallback(finishCallback.Invoke);
    }

    protected override void ContractCable(Action finishCallback)
    {
        var item = CurrentGrid.GetCell(transform.position).Item;
        
        var hookMoveDistance = Mathf.Abs(HookTransform.position.y - TopHookCeilingTransform.position.y);
        var hookMoveDuration = hookMoveDistance / HookMoveSpeed;

        Sequence sequence = DOTween.Sequence();
        sequence.Join( HookTransform.DOMove(TopHookCeilingTransform.position, hookMoveDuration).SetEase(HookMoveEase)) ;
        
        sequence.AppendCallback(finishCallback.Invoke);
    }


    protected override void MoveCable()
    {
        float distance = Vector3.Distance(TopHookCeilingTransform.position, HookTransform.position);

        CableTransform.localScale = new Vector3(_cableInitialScale.x, _cableInitialScale.y, distance * _cableLengthMultiply);
        //CableTransform.position = (TopHookCeilingTransform.position + HookTransform.position )/2f;
    }
    
    #endregion
}
