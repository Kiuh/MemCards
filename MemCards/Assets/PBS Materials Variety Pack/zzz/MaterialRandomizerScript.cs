using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MaterialRandomizerScript : MonoBehaviour
{
    public List<Material> materials;
    public List<GameObject> gameObjects;

    public void randomizeMaterials()
    {
        List<Material> materialsCopy = new(materials);
        foreach (GameObject go in gameObjects)
        {
            if (materialsCopy.Count == 0)
            {
                // time to quit
                return;
            }
            int chosen = Random.Range(0, materialsCopy.Count - 1);
            go.GetComponent<MeshRenderer>().material = materialsCopy[chosen];
            materialsCopy.RemoveAt(chosen);
        }
    }

    public void findMaterials()
    {
#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets(
            "t:Material",
            new[] { "Assets/PBS Materials Variety Pack/" }
        );
        materials.Clear();
        foreach (string id in guids)
        {
            //Debug.Log(AssetDatabase.GUIDToAssetPath(id));
            Material mat =
                AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(id)) as Material;
            if (!AssetDatabase.GUIDToAssetPath(id).Contains("coming_soon"))
            {
                materials.Add(mat);
            }
        }
#endif
    }

    public void findMaterialSpheres()
    {
        gameObjects.Clear();
        foreach (GameObject gameObj in FindObjectsOfType(typeof(GameObject)) as GameObject[])
        {
            if (gameObj.name == "Material Sphere")
            {
                //Debug.Log(gameObj.name);
                gameObjects.Add(gameObj);
            }
        }
    }
}
