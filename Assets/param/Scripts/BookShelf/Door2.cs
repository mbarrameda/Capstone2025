using UnityEngine;
using System.Collections;

/// <summary>
/// Simple door controller that moves the door from closedPos to openPos when OpenDoor() is called.
/// Attach to the door GameObject.
/// </summary>
public class DoorController : MonoBehaviour
{
    [Header("Door positions")]
    [Tooltip("Local position where the door rests when closed. Leave as default if you want to use current local position.")]
    public Vector3 closedLocalPosition;

    [Tooltip("Local position where the door will move when open (e.g. move up).")]
    public Vector3 openLocalPosition = new Vector3(0, 3f, 0);

    [Header("Timing")]
    [Tooltip("Seconds it takes the door to open.")]
    public float openTime = 1.0f;

    private bool isOpen = false;

    private void Start()
    {
        // Initialize closed position to current local position if not set explicitly
        closedLocalPosition = transform.localPosition;
    }

    /// <summary>
    /// Public method to open the door. If already open, does nothing.
    /// </summary>
    public void OpenDoor()
    {
        if (isOpen) return;
        StartCoroutine(OpenDoorCoroutine());
    }

    private IEnumerator OpenDoorCoroutine()
    {
        isOpen = true;

        float elapsed = 0f;
        Vector3 start = transform.localPosition;
        Vector3 target = closedLocalPosition + openLocalPosition;

        while (elapsed < openTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / openTime);
            transform.localPosition = Vector3.Lerp(start, target, t);
            yield return null;
        }

        transform.localPosition = target;
    }
}
