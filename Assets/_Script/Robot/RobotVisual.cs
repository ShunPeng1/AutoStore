using System;
using System.Collections;
using System.Collections.Generic;
using _Script.Robot;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Robot))]
public class RobotVisual : MonoBehaviour
{
    private Robot _robot;
    [SerializeField] private Renderer _robotBodyRenderer;
    [SerializeField] private LineRenderer _lineRenderer;

    private MaterialPropertyBlock _materialPropertyBlock;
    private static readonly int BaseMapProperty = Shader.PropertyToID("_BaseColor");

    private void Start()
    {
        _robot = GetComponent<Robot>();
     
        //SetBodyColorNoPattern();
        SetBodyColorUsingFlyWeightPattern();   
        
    }

    private void Update()
    {
        ShowPath();
    }

    void ShowPath()
    {
        if (_robot.CurrentRobotState == RobotStateEnum.Idle || _robot.MovingPath == null)
        {
            _lineRenderer.positionCount = 0;
            return;
        }
        
        _lineRenderer.positionCount = _robot.MovingPath.Count + 1;
        _lineRenderer.SetPosition(0, transform.position);

        int itr = 1;
        foreach (var cell in _robot.MovingPath)
        {
            _lineRenderer.SetPosition(itr, _robot.CurrentGrid.GetWorldPositionOfNearestCell(cell.XIndex,cell.ZIndex));
            itr++;
        }
    }

    /// <summary>
    /// Using FlyWeight Pattern to reduce the number of memory use in Ram
    /// </summary>
    private void SetBodyColorUsingFlyWeightPattern()
    {
        _materialPropertyBlock = new MaterialPropertyBlock();
        _robotBodyRenderer.GetPropertyBlock(_materialPropertyBlock); // Get the current material to _materialPropertyBlock
        _materialPropertyBlock.SetColor(BaseMapProperty, GetRandomBrightColor()); // Assign a random color
        _robotBodyRenderer.SetPropertyBlock(_materialPropertyBlock); // Apply the edit material to renderer
        
    }
    
    private void SetBodyColorNoPattern()
    {
        _robotBodyRenderer.material.color = GetRandomBrightColor(); // Assign a random color
    }

    private Color GetRandomBrightColor()
    {
        return Color.HSVToRGB(Random.Range(0f,1f), 1f,1f);
    }
    
}
