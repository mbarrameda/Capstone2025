using UnityEngine;

public class ArtifactItem : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pickupSFX;

    public void Pickup(PlayerInputHandler player)
    {
        // Only works for Explorer tag
        if (!player.CompareTag("Explorer")) return;

        if (player.isCarryingArtifact)
        {
            // Show the "I need to keep down..." message using your existing hint system
            if (player.uiHint != null)
                player.uiHint.ShowHint("I need to keep down the one I have on the altar", 2f);
            return;
        }

        // Play SFX
        if (audioSource && pickupSFX)
            audioSource.PlayOneShot(pickupSFX);

        player.isCarryingArtifact = true;

        // Hide the artifact from the world
        gameObject.SetActive(false);
    }
}