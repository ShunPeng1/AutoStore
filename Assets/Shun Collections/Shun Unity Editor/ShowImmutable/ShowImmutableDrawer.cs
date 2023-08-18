using UnityEditor;
using UnityEngine;

namespace Shun_Unity_Editor
{
    #if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ShowImmutableAttribute))]
    public class ShowImmutableDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.PropertyField(position, property, label);
            EditorGUI.EndDisabledGroup();
        }
    }
    
    #endif
}