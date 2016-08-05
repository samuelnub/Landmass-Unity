using UnityEngine;
using System.Collections;
using UnityEditor; // Needs this!

// Inherits off editor instead of monobehaviour
[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor {

    public override void OnInspectorGUI()
    {
        MapGenerator mapGen = (MapGenerator)target;

        if(DrawDefaultInspector())
        {
            if(mapGen.autoUpdate)
            {
                mapGen.DrawMapInEditor();
            }
        }

        if(GUILayout.Button("Generato!"))
        {
            mapGen.DrawMapInEditor();
        }
    }
}
