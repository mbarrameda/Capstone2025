using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class LightPuzzleManager : MonoBehaviour
{
    [Header("Puzzle Objects")]
    public LightReactiveObject[] glowObjects;
    public GameObject doorObject;
    public float doorOpenSpeed = 2f;

    [Header("Feedback")]
    public AudioClip doorOpenSound;
    public AudioSource audioSource;
    public SimpleUIHintTMP uiHint;

    private HashSet<LightReactiveObject> activatedObjects = new HashSet<LightReactiveObject>();
    private bool puzzleCompleted = false;

    private void Start()
    {
        foreach (var obj in glowObjects)
        {
            obj.puzzleManager = this;
        }

        if (uiHint == null)
            uiHint = FindObjectOfType<SimpleUIHintTMP>();
    }

    public void RegisterGlow(LightReactiveObject obj)
    {
        if (puzzleCompleted) return;

        if (!activatedObjects.Contains(obj))
        {
            activatedObjects.Add(obj);
            Debug.Log($"Activated glow object: {obj.name}");

            if (activatedObjects.Count == glowObjects.Length)
            {
                PuzzleSolved();
            }
        }
    }

    private void PuzzleSolved()
    {
        puzzleCompleted = true;
        Debug.Log("All glow objects activated! Opening door...");

        if (audioSource != null && doorOpenSound != null)
            audioSource.PlayOneShot(doorOpenSound);

        if (uiHint != null)
            uiHint.ShowHint("A door has opened!");

        StartCoroutine(OpenDoor());
        Invoke(nameof(HideHint), 3f);
    }

    private void HideHint()
    {
        if (uiHint != null)
            uiHint.HideHint();
    }

    private System.Collections.IEnumerator OpenDoor()
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
