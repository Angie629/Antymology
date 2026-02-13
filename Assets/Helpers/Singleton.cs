/*
 * Code pulled from https://wiki.unity3d.com/index.php/Singleton 
 */
using UnityEngine;

/// <summary>
/// Inherit from this base class to create a singleton.
/// Usage: public class MyClassName : Singleton<MyClassName> {}
/// Ensures only one instance exists and persists across scenes.
/// </summary>
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    // Check to see if we're about to be destroyed.
    /// <summary>
    /// True if the application is quitting or the singleton is being destroyed.
    /// </summary>
    private static bool m_ShuttingDown = false;
    /// <summary>
    /// Lock object for thread safety.
    /// </summary>
    private static object m_Lock = new object();
    /// <summary>
    /// The singleton instance.
    /// </summary>
    private static T m_Instance;

    /// <summary>
    /// Access singleton instance through this propriety.
    /// </summary>
    /// <summary>
    /// Returns the singleton instance, creating it if necessary.
    /// </summary>
    public static T Instance
    {
        get
        {
            if (m_ShuttingDown)
            {
                Debug.LogWarning("[Singleton] Instance '" + typeof(T) +
                    "' already destroyed. Returning null.");
                return null;
            }

            lock (m_Lock)
            {
                if (m_Instance == null)
                {
                    // Search for existing instance.
                    m_Instance = Object.FindFirstObjectByType<T>();

                    // Create new instance if one doesn't already exist.
                    if (m_Instance == null)
                    {
                        // Need to create a new GameObject to attach the singleton to.
                        var singletonObject = new GameObject();
                        m_Instance = singletonObject.AddComponent<T>();
                        singletonObject.name = typeof(T).ToString() + " (Singleton)";

                        // Make instance persistent.
                        DontDestroyOnLoad(singletonObject);
                    }
                }

                return m_Instance;
            }
        }
    }


    /// <summary>
    /// Called when the application quits to prevent creating new instances.
    /// </summary>
    private void OnApplicationQuit()
    {
        m_ShuttingDown = true;
    }


    /// <summary>
    /// Called when the singleton is destroyed to prevent recreation.
    /// </summary>
    private void OnDestroy()
    {
        m_ShuttingDown = true;
    }
}