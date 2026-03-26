using UnityEngine;
using System.Collections.Generic;
using System.Collections; // Required for Coroutines

public class LightPuzzleManager : MonoBehaviour
{
    [Header("Puzzle Configuration")]
    public LightReactiveObject[] requiredShapes;
    public GameObject doorObject;
    public float doorOpenSpeed = 2f;

    [Header("UI Settings")]
    public SimpleUIHintTMP uiHint;
    public float messageDuration = 3f; // Time before text vanishes

    [Header("Audio")]
    public AudioClip successSound;
    public AudioSource globalAudioSource;

    private HashSet<LightReactiveObject> activatedObjects = new HashSet<LightReactiveObject>();
    private bool puzzleCompleted = false;
    private Coroutine hideRoutine; // Tracks the current timer

    private void Start()
    {
        foreach (var obj in requiredShapes)
        {
            if (obj != null)
            {
                obj.puzzleManager = this;
                obj.isRequiredShape = true;
            }
        }

        LightReactiveObject[] allInScene = FindObjectsOfType<LightReactiveObject>();
        foreach (var obj in allInScene)
        {
            if (obj.puzzleManager == null) obj.puzzleManager = this;
        }
    }

    public void RegisterGlow(LightReactiveObject obj)
    {
        if (puzzleCompleted) return;

        if (!activatedObjects.Contains(obj))
        {
            activatedObjects.Add(obj);
            int remaining = requiredShapes.Length - activatedObjects.Count;

            if (remaining > 0)
            {
                ShowMessage($"Shape activated! {remaining} more to find.");
            }
            else
            {
                PuzzleSolved();
            }
        }
    }

    public void NotifyWrongShape()
    {
        if (puzzleCompleted) return;
        ShowMessage("I need to look for other shapes...");
    }

    private void PuzzleSolved()
    {
        puzzleCompleted = true;

        if (successSound && globalAudioSource)
            globalAudioSource.PlayOneShot(successSound);

        ShowMessage("A door opened up!");
        StartCoroutine(OpenDoorSequence());
    }

    // New helper method to handle showing and auto-hiding
    private void ShowMessage(string message)
    {
        if (uiHint == null) return;

        // If a timer is already running, stop it so it doesn't hide the NEW message early
        if (hideRoutine != null) StopCoroutine(hideRoutine);

        uiHint.ShowHint(message);
        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(messageDuration);
        uiHint.HideHint();
        hideRoutine = null;
    }

    private IEnumerator OpenDoorSequence()
    {
        if (doorObject == null) yield break;

        Vector3 startPos = doorObject.transform.position;
        Vector3 endPos = startPos + Vector3.up * 3f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * doorOpenSpeed;
            doorObject.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
        doorObject.transform.position = endPos;
    }
}