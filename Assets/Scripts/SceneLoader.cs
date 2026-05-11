using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] string loadScene;

    void Awake()
    {
        if (string.IsNullOrWhiteSpace(loadScene))
        {
            Debug.LogWarning($"{nameof(SceneLoader)} on '{gameObject.name}' has no scene name assigned.");
        }
    }

    public void LoadScene()
    {
        if (string.IsNullOrWhiteSpace(loadScene)) return;

        // Treat loadScene as either a build index or a scene name.
        if (int.TryParse(loadScene, out int buildIndex))
        {
            if (buildIndex >= 0 && buildIndex < SceneManager.sceneCountInBuildSettings)
                SceneManager.LoadScene(buildIndex);
            else
                Debug.LogError($"{nameof(SceneLoader)}: Build index {buildIndex} is out of range.");
        }
        else
        {
            // If the scene is not in build settings, LoadScene will throw an error.
            if (Application.CanStreamedLevelBeLoaded(loadScene))
            {
                SceneManager.LoadScene(loadScene);
            }
            else
            {
                Debug.LogError($"{nameof(SceneLoader)}: Scene '{loadScene}' is not found in Build Settings.");
            }
        }
    }
}
