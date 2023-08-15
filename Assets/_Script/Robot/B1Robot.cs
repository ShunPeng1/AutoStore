using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Script.Robot;
using Shun_Grid_System;
using UnityEngine;
using Random = UnityEngine.Random;

public class B1Robot : Robot
{
    [Header("Robot Detection")]
    [SerializeField] protected float BoxColliderSize = 0.9f;
    [SerializeField] protected float CastRadius = 1.5f;
    [SerializeField] protected LayerMask RobotLayerMask;

    private float _minBlockAheadAngle => Mathf.Atan((CastRadius + BoxColliderSize/2)/(0.5f + BoxColliderSize/2)) * Mathf.PI;
    private float _maxBlockAheadAngle = 45f;
    
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

}
