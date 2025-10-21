using UnityEngine;
using System.Collections;

/// <summary>
/// Attach to each child cube (the interactable cubes).
/// Handles detecting the player (tag "Explorer") entering/exiting trigger,
/// showing the UI hint, and performing interaction on key press.
/// Interaction is delegated to the CubeGroupManager.
/// </summary>
public class CubeChildInteract : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Index of this child in the group (0..5). Assign in inspector or leave -1 to let manager assign automatically.")]
    public int childIndex = -1;

    [Tooltip("Reference to the parent CubeGroup manager (drag the GameObject with CubeGroupManager).")]
    public CubeGroupManager groupManager;

    [Tooltip("Key used for interaction.")]
    public KeyCode interactKey = KeyCode.E;

    // whether the player is inside this cube's trigger
    private bool playerInRange = false;

    // cache the player's GameObject (optional)
    private GameObject player;

    private void Reset()
    {
        // Ensure there's a trigger collider present (not enforced, but helpful)
        Collider c = GetComponent<Collider>();
        if (c == null)
        {
            gameObject.AddComponent<BoxCollider>().isTrigger = true;
        }
        else
        {
            c.isTrigger = true;
        }
    }

    private void Start()
    {
        // If group manager not set, try to find it in the parent chain.
        if (groupManager == null)
        {
            groupManager = GetComponentInParent<CubeGroupManager>();
            if (groupManager == null)
            {
                Debug.LogWarning($"{name}: CubeGroupManager not assigned and not found in parents.");
            }
        }

        // If index is left -1, attempt to assign index by parent's child order
        if (childIndex < 0 && transform.parent != null)
        {
            for (int i = 0; i < transform.parent.childCount; i++)
            {
                if (transform.parent.GetChild(i) == transform)
                {
                    childIndex = i;
                    break;
                }
            }
        }
    }

    private void Update()
    {
        // If player is in range and press key, interact
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            OnInteract();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only allow player tagged "Explorer"
        if (other.gameObject.CompareTag("Explorer"))
        {
            playerInRange = true;
            player = other.gameObject;

            // Ask manager to show hint (manager will forward to UI)
            if (groupManager != null)
                groupManager.ShowInteractHint(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Explorer"))
        {
            playerInRange = false;
            player = null;

            if (groupManager != null)
                groupManager.ShowInteractHint(false);
        }
    }

    /// <summary>
    /// Called when the player interacts with this cube (presses the interact key while in range).
    /// Delegates handling to the group manager which knows which index is correct.
    /// </summary>
    public void OnInteract()
    {
        if (groupManager != null)
        {
            groupManager.ChildInteracted(childIndex, gameObject);
        }
        else
        {
            Debug.LogWarning($"{name}: No groupManager assigned — cannot process interaction.");
        }
    }
}
