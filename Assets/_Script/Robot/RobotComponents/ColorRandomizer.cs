using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorRandomizer : MonoBehaviour
{
    [SerializeField] private Renderer[] _renderers;
    
    private MaterialPropertyBlock _materialPropertyBlock;
    private static readonly int BaseMapProperty = Shader.PropertyToID("_BaseColor");
    
    
    void Start()
    {
        SetColorFlyWeight();
    }
    
    
    private void SetColorFlyWeight()
    {
        _materialPropertyBlock = new MaterialPropertyBlock();
        Color color = GetRandomColor();
        
        foreach (var rendererToSet in _renderers)
        {
            rendererToSet.GetPropertyBlock(_materialPropertyBlock); // Get the current material to _materialPropertyBlock
            _materialPropertyBlock.SetColor(BaseMapProperty, color); // Assign a random color
            rendererToSet.SetPropertyBlock(_materialPropertyBlock); // Apply the edit material to renderer
        }
        
    }

    private Color GetRandomColor()
    {
        return Color.HSVToRGB(Random.Range(0f,1f), Random.Range(0f,1f),Mathf.Pow( Random.Range(0.875f,1f), 3));
    }
    
}
