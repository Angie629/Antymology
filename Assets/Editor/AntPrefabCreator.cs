#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Handles making and assigning the AntPrefab in Unity Editor
public static class AntPrefabCreator
{
    private const string PrefabPath = "Assets/Resources/AntPrefab.prefab";

    // Runs when Unity loads the editor, sets up prefab creation
    [InitializeOnLoadMethod]
    private static void EnsurePrefab()
    {
        EditorApplication.delayCall += CreatePrefabIfMissing;
    }

    // Makes the AntPrefab if it's not already in Resources
    private static void CreatePrefabIfMissing()
    {
        // If prefab exists, do nothing
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            return;
        }

        // Make sure Resources folder is there
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        // Make the root GameObject for the prefab
        GameObject root = new GameObject("AntPrefab");
        // Add a capsule for the ant's visual
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "AntVisual";
        visual.transform.SetParent(root.transform, false);
        // Remove collider from the capsule
        Object.DestroyImmediate(visual.GetComponent<Collider>());

        // Save the prefab and clean up
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        // Give the prefab to any WorldManager in the scene
        if (prefab != null)
        {
            AssignPrefabToWorldManagers(prefab);
        }
    }

    // Assigns the prefab to all WorldManagers that don't have one
    private static void AssignPrefabToWorldManagers(GameObject prefab)
    {
        Antymology.Terrain.WorldManager[] managers = Object.FindObjectsByType<Antymology.Terrain.WorldManager>(FindObjectsSortMode.None);
        foreach (Antymology.Terrain.WorldManager manager in managers)
        {
            if (manager.antPrefab == null)
            {
                manager.antPrefab = prefab;
                EditorUtility.SetDirty(manager);
            }
        }
    }
}
#endif
