using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Controls the Sound Pattern Puzzle logic:
/// - Generates a random 6-sound pattern
/// - Plays it when the pattern cube is interacted with
/// - Checks player's reproduction sequence
/// - Opens the door on success
/// </summary>
public class SoundPatternPuzzle : MonoBehaviour
{
    [Header("Audio Settings")]
    [Tooltip("Assign 6 unique sound clips for the puzzle.")]
    public AudioClip[] soundClips; // 6 total sounds

    [Tooltip("AudioSource used for playing all sounds.")]
    public AudioSource audioSource;

    [Header("Door Settings")]
    [Tooltip("The door GameObject to move/open after solving the puzzle.")]
    public GameObject doorObject;
    [Tooltip("Speed of door opening animation.")]
    public float doorOpenSpeed = 2f;

    private List<int> patternSequence = new List<int>();
    private List<int> playerInputSequence = new List<int>();

    private bool patternGenerated = false;
    private bool patternPlaying = false;
    private bool puzzleCompleted = false;

    private void Start()
    {
        if (soundClips.Length != 6)
            Debug.LogWarning("?? You should assign exactly 6 sound clips for this puzzle!");
    }

    /// <summary>
    /// Called by the pattern cube — generates and plays a random 6-sound sequence.
    /// </summary>
    public void PlayRandomPattern()
    {
        if (puzzleCompleted || patternPlaying)
            return;

        GeneratePattern();
        StartCoroutine(PlayPattern());
    }

    private void GeneratePattern()
    {
        patternSequence.Clear();
        for (int i = 0; i < 6; i++)
        {
            patternSequence.Add(Random.Range(0, soundClips.Length));
        }
        patternGenerated = true;
        Debug.Log("?? New random sound pattern generated.");
    }

    private IEnumerator PlayPattern()
    {
        patternPlaying = true;
        foreach (int index in patternSequence)
        {
            audioSource.clip = soundClips[index];
            audioSource.Play();
            yield return new WaitForSeconds(audioSource.clip.length + 0.3f);
        }

        patternPlaying = false;
        Debug.Log("?? Pattern playback finished. Player can now reproduce it.");
    }

    /// <summary>
    /// Called by the 6 input cubes when interacted with.
    /// </summary>
    public void RegisterInput(int cubeID)
    {
        if (!patternGenerated || patternPlaying || puzzleCompleted)
            return;

        // Immediate sound feedback
        audioSource.clip = soundClips[cubeID];
        audioSource.Play();

        playerInputSequence.Add(cubeID);

        if (playerInputSequence.Count == patternSequence.Count)
            CheckPattern();
    }

    private void CheckPattern()
    {
        for (int i = 0; i < patternSequence.Count; i++)
        {
            if (playerInputSequence[i] != patternSequence[i])
            {
                Debug.Log("? Wrong sequence. Try again.");
                playerInputSequence.Clear();
                return;
            }
        }

        Debug.Log("? Correct sequence! Puzzle solved!");
        puzzleCompleted = true;
        StartCoroutine(OpenDoor());
    }

    private IEnumerator OpenDoor()
    {
        if (doorObject == null) yield break;

        Vector3 startPos = doorObject.transform.position;
        Vector3 endPos = startPos + Vector3.up * 3f;

        float t = 0f;
        while (t < 1f)
        {
            doorObject.transform.position = Vector3.Lerp(startPos, endPos, t);
            t += Time.deltaTime * doorOpenSpeed;
            yield return null;
        }

        doorObject.transform.position = endPos;
    }
}
