using UnityEngine;
using TMPro;
using System.Collections;

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

    [Header("Feedback")]
    [Tooltip("TextMeshProUGUI element for pop-up messages.")]
    public TextMeshProUGUI hintText;

    [Tooltip("Time in seconds the pop-up stays visible.")]
    public float hintDuration = 2f;

    [Tooltip("Sound played when the correct cube is selected.")]
    public AudioClip successSound;

    [Tooltip("AudioSource used for feedback sounds.")]
    public AudioSource audioSource;

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

        // Hide hint text initially
        if (hintText != null)
            hintText.enabled = false;
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

            // Show pop-up hint
            if (hintText != null)
                StartCoroutine(ShowHint("You heard a door open!"));

            // Play success sound
            if (audioSource != null && successSound != null)
                audioSource.PlayOneShot(successSound);

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

    private IEnumerator RotateDoor(GameObject doorObj, Quaternion targetRot)
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

    /// <summary>
    /// Shows a hint message temporarily on screen.
    /// </summary>
    private IEnumerator ShowHint(string message)
    {
        hintText.text = message;
        hintText.enabled = true;

        yield return new WaitForSeconds(hintDuration);

        hintText.enabled = false;
    }
}
