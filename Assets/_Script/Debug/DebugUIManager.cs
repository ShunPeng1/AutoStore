using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityUtilities;

public class DebugUIManager : SingletonMonoBehaviour<DebugUIManager>
{
    [SerializeField] private bool isActive = true;
    [SerializeField] private TMP_Text fpsText;
    [SerializeField] private float deltaTime;
    
    [Header("Collision")]
    [SerializeField] private TMP_Text collisionText;
    private int _collosionCount;
    
    void Update () {
        if(!isActive) return;

        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        fpsText.text ="FPS " + Mathf.Ceil (fps).ToString ();
    }

    public void AddCollision()
    {
        _collosionCount++;
        collisionText.text = "Collide: " + _collosionCount.ToString();
    }
    
}
