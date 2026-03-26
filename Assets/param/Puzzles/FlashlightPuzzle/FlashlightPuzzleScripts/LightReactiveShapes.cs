using UnityEngine;

public class LightReactiveObject : MonoBehaviour
{
    [Header("Settings")]
    public bool isRequiredShape = true; // Check this in Inspector if it's part of the puzzle
    public Material activatedMaterial;

    [Header("Feedback")]
    public AudioClip activationSound;
    private AudioSource audioSource;

    [HideInInspector] public bool isActivated = false;
    [HideInInspector] public LightPuzzleManager puzzleManager;

    private Renderer rend;

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        audioSource = GetComponent<AudioSource>();

        // Add AudioSource automatically if missing
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void Activate()
    {
        if (isActivated) return;

        if (isRequiredShape)
        {
            isActivated = true;

            // Swap Material
            if (rend != null && activatedMaterial != null)
                rend.material = activatedMaterial;

            // Play Sound
            if (activationSound != null)
                audioSource.PlayOneShot(activationSound);

            // Tell Manager to update count and show "Remaining" message
            puzzleManager?.RegisterGlow(this);
        }
        else
        {
            // Tell Manager to show "Wrong Shape" message
            puzzleManager?.NotifyWrongShape();
        }
    }
}