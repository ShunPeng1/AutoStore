using System;
using System.Collections;
using System.Collections.Generic;
using _Script.Robot;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Robot))]
public class RobotVisual : RobotComponentDependence
{
    [SerializeField] private Renderer _robotBodyRenderer;
    [SerializeField] private LineRenderer _lineRenderer;

    private MaterialPropertyBlock _materialPropertyBlock;
    private static readonly int BaseMapProperty = Shader.PropertyToID("_BaseColor");

    private void Start()
    {
        SetBodyColorUsingFlyWeightPattern();
    }

    private void Update()
    {
        ShowPath();
    }

    void ShowPath()
    {
        if (Robot.CurrentRobotState is Robot.RobotIdlingState || Robot.MovingPath == null)
        {
            _lineRenderer.positionCount = 0;
            return;
        }
        
        _lineRenderer.positionCount = Robot.MovingPath.Count + 2;
        _lineRenderer.SetPosition(0, Robot.transform.position);
        _lineRenderer.SetPosition(1, Robot.NextCellPosition);
        int itr = 2;
        foreach (var cell in Robot.MovingPath)
        {
            _lineRenderer.SetPosition(itr, Robot.CurrentGrid.GetWorldPositionOfNearestCell(cell.XIndex,cell.YIndex));
            itr++;
        }
    }

    /// <summary>
    /// Using FlyWeight Pattern to reduce the number of memory use in Ram
    /// </summary>
    private void SetBodyColorUsingFlyWeightPattern()
    {
        _materialPropertyBlock = new MaterialPropertyBlock();
        Color color = GetRandomColor();
        
        _robotBodyRenderer.GetPropertyBlock(_materialPropertyBlock); // Get the current material to _materialPropertyBlock
        _materialPropertyBlock.SetColor(BaseMapProperty, color); // Assign a random color
        _robotBodyRenderer.SetPropertyBlock(_materialPropertyBlock); // Apply the edit material to renderer
        
        _lineRenderer.GetPropertyBlock(_materialPropertyBlock);
        _materialPropertyBlock.SetColor(BaseMapProperty, color); // Assign a random color
        _lineRenderer.SetPropertyBlock(_materialPropertyBlock);
    }

    private Color GetRandomColor()
    {
        float isSaturation = Random.Range(0, 2);
        return Color.HSVToRGB(Random.Range(0f,1f), Random.Range(0f,1f),Mathf.Pow( Random.Range(0.875f,1f), 3));
    }
    
}
