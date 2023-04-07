using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DebugUI : MonoBehaviour
{
    [SerializeField] private bool isActive = true;
    [SerializeField] private TMP_Text fpsText;
    [SerializeField] private float deltaTime;
    
    void Update () {
        if(!isActive) return;

        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        fpsText.text ="FPS " + Mathf.Ceil (fps).ToString ();
    }
}
