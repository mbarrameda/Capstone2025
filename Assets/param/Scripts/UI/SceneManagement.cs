using UnityEngine;
using UnityEngine.SceneManagement;

public class TripWire : MonoBehaviour
{
    [Header("Scene to Load")]
    public string sceneName; // Name of the scene to load when the player enters

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the trigger has the tag "Explorer"
        if (other.CompareTag("Explorer"))
        {
            // Load the target scene
            SceneManager.LoadScene(sceneName);
        }
    }
}
