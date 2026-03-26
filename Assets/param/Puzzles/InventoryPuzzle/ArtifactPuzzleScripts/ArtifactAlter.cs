using UnityEngine;
using System.Collections.Generic;

public class ArtifactAltar : MonoBehaviour
{
    [Header("Altar Settings")]
    public int artifactsNeeded = 3;
    private int currentArtifacts = 0;

    [Header("Visuals")]
    public List<Transform> artifactSlots = new List<Transform>();

    [Header("Audio")]
    public AudioSource altarAudioSource;
    public AudioClip placeSound;         // Regular placement sound
    public AudioClip completionSound;    // Big sound when 3/3 are placed

    [Header("Success Events")]
    public GameObject doorToOpen;
    public string escapeText = "The path is clear! ESCAPE!";

    public void PlaceArtifact(PlayerInputHandler player)
    {
        if (player.isCarryingArtifact)
        {
            // 1. Show the artifact model
            if (currentArtifacts < artifactSlots.Count)
            {
                artifactSlots[currentArtifacts].gameObject.SetActive(true);
            }

            currentArtifacts++;
            player.isCarryingArtifact = false;

            // 2. Play the correct Audio Clip
            HandlePlacementAudio();

            // 3. UI Feedback
            if (player.uiHint != null)
                player.uiHint.ShowHint($"{currentArtifacts}/{artifactsNeeded} placed...", 2f);

            // 4. Check for Puzzle Completion
            if (currentArtifacts >= artifactsNeeded)
            {
                OpenPath(player);
            }
        }
        else
        {
            if (player.uiHint != null)
                player.uiHint.ShowHint("I need an artifact first.");
        }
    }

    private void HandlePlacementAudio()
    {
        if (altarAudioSource == null) return;

        // If this was the final piece:
        if (currentArtifacts >= artifactsNeeded)
        {
            if (completionSound != null)
            {
                // Play the big completion sound
                altarAudioSource.PlayOneShot(completionSound);
            }
        }
        else // Otherwise play the regular "thud"
        {
            if (placeSound != null)
            {
                altarAudioSource.PlayOneShot(placeSound);
            }
        }
    }

    private void OpenPath(PlayerInputHandler player)
    {
        if (doorToOpen != null) doorToOpen.SetActive(false);
        if (player.uiHint != null) player.uiHint.ShowHint(escapeText, 5f);
    }
}