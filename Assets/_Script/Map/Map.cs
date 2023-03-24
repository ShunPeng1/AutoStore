using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour
{
    [SerializeField] private int width = 100, height = 100;
    
    void Start()
    {
        GridXZ<StackStorage> grid = new GridXZ<StackStorage>(width,height, 1,1, transform.position,
            );
    }

    
}
