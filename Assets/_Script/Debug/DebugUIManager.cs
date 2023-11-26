using System.Collections;
using System.Collections.Generic;
using _Script.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityUtilities;

public class DebugUIManager : SingletonMonoBehaviour<DebugUIManager>
{
    [SerializeField] private bool isActive = true;
    [SerializeField] private TMP_Text fpsText;
    [SerializeField] private float deltaTime;
    
    [Header("Time Scale")]
    [SerializeField] private Slider timeScaleSlider;
    [SerializeField] private int minTimeScale = 0, maxTimeScale = 100;
    [SerializeField] private float timeScaleStep = 1.0f;
    
    [Header("Collision")]
    [SerializeField] private TMP_Text collisionText;
    private int _collosionCount;
    
    [Header("Crate finish")]
    [SerializeField] private TMP_Text finishText;
    private int _finishCount;
    
    void Start()
    {
        // Set the initial value of the slider to the current time scale
        timeScaleSlider.minValue = minTimeScale;
        timeScaleSlider.maxValue = maxTimeScale;

        timeScaleSlider.value = Time.timeScale;
    }
    
    void Update () {
        if(!isActive) return;

        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        fpsText.text ="FPS " + Mathf.Ceil (fps).ToString ();
        
        
        // Decrease time scale with "J" key
        if (Input.GetKeyDown(KeyCode.J))
        {
            AdjustTimeScale(-timeScaleStep);
        }

        // Increase time scale with "K" key
        if (Input.GetKeyDown(KeyCode.K))
        {
            AdjustTimeScale(timeScaleStep);
        }
    }

    public void AddCollision()
    {
        _collosionCount++;
        collisionText.text = "Collide: " + _collosionCount/2;
    }
    
    
    public void AddFinish()
    {
        _finishCount++;
        finishText.text = "finish: " + _finishCount;
    }
    
    private void AdjustTimeScale(float delta)
    {
        float newTimeScale = Mathf.Clamp(Time.timeScale + delta, minTimeScale, maxTimeScale);
        Time.timeScale = newTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale; // Adjust fixed delta time accordingly
        
        timeScaleSlider.value = newTimeScale;
    }
    
}
