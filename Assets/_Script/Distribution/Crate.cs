using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crate : MonoBehaviour
{
    public int currentX
    {
        get => currentX;
        set => currentX = value;
    }

    public int currentZ
    {
        get => currentZ;
        set => currentZ = value;
    }
    
    public int storingX
    {
        get => storingX;
        set => storingX = value;
    }
    public int storingZ
    {
        get => storingZ;
        set => storingZ = value;
    }

    public void Init()
    {
        
    }
}
