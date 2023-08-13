using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapManager))]
public class MapManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        MapManager mapManager = (MapManager)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Spawn Stack Storage"))
        {
            mapManager.SpawnStackStorage();
        }

        if (GUILayout.Button("Delete Stack Storage"))
        {
            mapManager.DeleteStackStorage();
        }
    }
}