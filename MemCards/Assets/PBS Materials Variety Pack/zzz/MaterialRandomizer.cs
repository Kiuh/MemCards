# if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MaterialRandomizerScript))]
public class MaterialRandomizer : Editor
{
    public override void OnInspectorGUI()
    {
        _ = DrawDefaultInspector();

        MaterialRandomizerScript myScript = (MaterialRandomizerScript)target;
        if (GUILayout.Button("Find Materials"))
        {
            myScript.findMaterials();
        }
        if (GUILayout.Button("Find Spheres"))
        {
            myScript.findMaterialSpheres();
        }
        if (GUILayout.Button("Randomize Materials"))
        {
            myScript.randomizeMaterials();
        }
    }
}
#endif
