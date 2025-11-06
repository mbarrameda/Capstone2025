using UnityEngine;

/// <summary>
/// Attached to each cube in the sound puzzle.
/// - The pattern cube triggers pattern playback.
/// - Input cubes send their ID to the puzzle manager.
/// </summary>
public class SoundCubeInteract : MonoBehaviour
{
    [Tooltip("Reference to the SoundPatternPuzzle manager.")]
    public SoundPatternPuzzle puzzleManager;

    [Tooltip("Is this the single cube that plays the pattern?")]
    public bool isPatternCube = false;

    [Tooltip("Unique cube ID (0–5) for input cubes only.")]
    public int cubeID = 0;

    [Tooltip("Hint text displayed when player looks at this cube.")]
    public string hintMessage = "Press □ / X to Interact";

    public void OnInteract()
    {
        if (puzzleManager == null) return;

        if (isPatternCube)
            puzzleManager.PlayRandomPattern();
        else
            puzzleManager.RegisterInput(cubeID);
    }
}
