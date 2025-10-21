using UnityEngine;
using System.Collections;

/// <summary>
/// Attach to the parent object that contains the six child cubes.
/// Responsibilities:
/// - Track which index is the correct one to open the door
/// - Receive child interactions and handle results
/// - Forward UI hint show/hide requests
/// - Reference the DoorController to open the door when the correct cube is selected
/// </summary>
public class CubeGroupManager : MonoBehaviour
{
    [Header("Game logic")]
    [Tooltip("Index (0..childCount-1) of the correct child that opens the door.")]
    public int correctChildIndex = 0;

    [Tooltip("If true, only the correct cube opens the door and other cubes just vanish with no effect.")]
    public bool onlyCorrectOpensDoor = true;

    [Header("References")]
    [Tooltip("Reference to the DoorController component.")]
    public DoorController doorController;

    [Tooltip("Optional UI hint text controller. If assigned, ShowInteractHint will call it.")]
    public SimpleUIHintTMP uiHint;

    [Header("Visual/Gameplay")]
    [Tooltip("If true, plays a small vanish animation (scale down) when a cube is removed.")]
    public bool playVanishAnimation = true;

    [Tooltip("Duration for vanish animation in seconds.")]
    public float vanishDuration = 0.25f;

    private void Start()
    {
        // Sanity check
        if (doorController == null)
            Debug.LogWarning($"{name}: doorController not assigned. Door won't open until you assign one.");

        if (uiHint == null)
            Debug.Log($"{name}: uiHint not assigned. Interaction hints will not be visible unless assigned.");

        // Ensure correctChildIndex is within valid range
        int childCount = transform.childCount;
        if (childCount == 0)
            Debug.LogWarning($"{name}: No child cubes found beneath CubeGroup.");

        if (correctChildIndex < 0 || correctChildIndex >= childCount)
        {
            Debug.LogWarning($"{name}: correctChildIndex {correctChildIndex} out of range (0..{Mathf.Max(0, childCount - 1)}). Clamping to 0.");
            correctChildIndex = Mathf.Clamp(correctChildIndex, 0, Mathf.Max(0, childCount - 1));
        }
    }

    /// <summary>
    /// Called by a child when a player interacts with it.
    /// childIndex: index assigned to the child (or -1 if unknown)
    /// childObject: the child's GameObject reference
    /// </summary>
    public void ChildInteracted(int childIndex, GameObject childObject)
    {
        bool isCorrect = (childIndex == correctChildIndex);

        if (isCorrect)
        {
            // remove the child and open the door
            StartCoroutine(VanishAndOpen(childObject));
        }
        else
        {
            // remove the child (optionally) but do not open the door
            StartCoroutine(VanishOnly(childObject));

            // If you want to give feedback for wrong picks, do it here (sound, UI, etc.)
            Debug.Log($"{name}: Player selected wrong cube (index {childIndex}).");
        }
    }

    /// <summary>
    /// Simple vanish coroutine that scales the object down then destroys it, then opens the door
    /// </summary>
    private IEnumerator VanishAndOpen(GameObject child)
    {
        yield return VanishCoroutine(child);
        // open the door (if assigned)
        if (doorController != null)
            doorController.OpenDoor();
        else
            Debug.LogWarning("CubeGroupManager: doorController not assigned when trying to open the door.");
    }

    /// <summary>
    /// Vanish but do not open door
    /// </summary>
    private IEnumerator VanishOnly(GameObject child)
    {
        yield return VanishCoroutine(child);
    }

    /// <summary>
    /// Generic vanish routine (scale down then destroy)
    /// </summary>
    private IEnumerator VanishCoroutine(GameObject child)
    {
        if (!playVanishAnimation || child == null)
        {
            if (child != null) Destroy(child);
            yield break;
        }

        float elapsed = 0f;
        Vector3 startScale = child.transform.localScale;
        while (elapsed < vanishDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / vanishDuration);
            child.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }

        Destroy(child);
    }

    /// <summary>
    /// Called by a child to show/hide the interact hint.
    /// This is a convenience so children don't need direct reference to UI.
    /// </summary>
    public void ShowInteractHint(bool show)
    {
        if (uiHint != null)
        {
            if (show) uiHint.ShowHint("Press E to interact");
            else uiHint.HideHint();
        }
    }
}
