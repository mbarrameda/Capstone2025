using UnityEngine;

/// <summary>
/// Attached to each child cube in the puzzle.
/// Handles interaction (called from PlayerInputHandler.OnInteract).
/// </summary>
public class CubeChildInteract : MonoBehaviour
{
    [Tooltip("Reference to the parent puzzle manager.")]
    public CubeGroupManager cubeManager;

    [Tooltip("Is this the correct cube to open the door?")]
    public bool isCorrectCube = false;

    private bool hasBeenInteracted = false;

    /// <summary>
    /// Called when the player interacts via controller.
    /// </summary>
    public void OnInteract()
    {
        if (hasBeenInteracted) return;
        hasBeenInteracted = true;

        // Hide this cube
        gameObject.SetActive(false);

        // Notify the puzzle manager
        if (cubeManager != null)
            cubeManager.OnCubeInteracted(this);
    }
}
