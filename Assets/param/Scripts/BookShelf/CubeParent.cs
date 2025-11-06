using UnityEngine;

/// <summary>
/// Controls the cube puzzle logic.
/// Keeps track of cubes and opens the door when the right cube is interacted with.
/// </summary>
public class CubeGroupManager : MonoBehaviour
{
    [Tooltip("Array of all 6 child cubes.")]
    public CubeChildInteract[] cubes;

    [Tooltip("The door GameObject that will open when the right cube is picked.")]
    public GameObject door;

    [Tooltip("Door open rotation or movement speed.")]
    public float doorOpenSpeed = 2f;

    private bool puzzleSolved = false;
    private Quaternion doorTargetRotation;

    private void Start()
    {
        // Assign this manager to each cube
        foreach (var cube in cubes)
        {
            cube.cubeManager = this;
        }

        // Keep door closed at start
        if (door != null)
            doorTargetRotation = door.transform.rotation;
    }

    /// <summary>
    /// Called by CubeChildInteract when a cube is interacted with.
    /// </summary>
    public void OnCubeInteracted(CubeChildInteract cube)
    {
        if (puzzleSolved) return;

        if (cube.isCorrectCube)
        {
            Debug.Log("Correct cube chosen! Door opening...");
            puzzleSolved = true;
            OpenDoor();
        }
        else
        {
            Debug.Log("Wrong cube!");
        }
    }

    private void OpenDoor()
    {
        if (door != null)
        {
            // Example: rotate door 90 degrees on Y axis
            Quaternion targetRotation = Quaternion.Euler(door.transform.eulerAngles - new Vector3(0, 90, 0));
            StartCoroutine(RotateDoor(door, targetRotation));
        }
    }

    private System.Collections.IEnumerator RotateDoor(GameObject doorObj, Quaternion targetRot)
    {
        Quaternion startRot = doorObj.transform.rotation;
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * doorOpenSpeed;
            doorObj.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }
    }
}
