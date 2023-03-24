using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class R5Robot : MonoBehaviour
{
    [Header("Grid Movement")]
    [SerializeField] private float movementSpeed = 1f;
    [SerializeField] private float preemptiveDistance = 0.05f;
    [SerializeField] private Map map;
    
    private Vector3 _moveToPosition;
    private GridXZ<StackStorageGridItem> _currentGrid;
    private int _xIndex, _zIndex;
    void Start()
    {
        var position = transform.position;
        _moveToPosition = position;
        _currentGrid = map._storageGrid;
        (_xIndex, _zIndex) = _currentGrid.GetXZ(position);
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, _moveToPosition, movementSpeed * Time.deltaTime);
        if(Vector3.Distance(transform.position, _moveToPosition) <= preemptiveDistance){
            float horizontal = Input.GetAxisRaw("Horizontal"), vertical = Input.GetAxisRaw("Vertical");
            if (Mathf.Abs(horizontal) == 1f)
            {
                var item = _currentGrid.GetItem(_xIndex+(int)horizontal, _zIndex);
                // If walkable
                _moveToPosition = item.stackStorage.transform.position;
            }
            else if (Mathf.Abs(vertical) == 1f)
            {
                var item = _currentGrid.GetItem(_xIndex, _zIndex + (int) vertical);
                // If walkable
                _moveToPosition = item.stackStorage.transform.position;
            }
        }
        else
        {
            ;
        }        
        
    }
}
