using UnityEngine;
using TMPro;
using System.Collections;

public class BookGroupManager : MonoBehaviour
{
    [Header("Puzzle Setup")]
    [Tooltip("Drag only the books belonging to THIS specific puzzle here.")]
    public BookInteract[] books;
    public GameObject door;
    public float doorOpenSpeed = 2f;

    [Header("UI Feedback")]
    public TextMeshProUGUI hintText;
    public float hintDuration = 2f;
    [TextArea] public string correctMessage = "You heard a door open!";
    [TextArea] public string wrongMessage = "Nothing happened...";

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip pickupSound;
    public AudioClip successSound;

    private bool puzzleSolved = false;

    private void Start()
    {
        if (books.Length == 0) return;

        int randomIndex = Random.Range(0, books.Length);

        for (int i = 0; i < books.Length; i++)
        {
            books[i].bookManager = this;
            bool isWinner = (i == randomIndex);
            books[i].SetAsCorrect(isWinner);

            // This will tell us in the Console which book is the right one!
            if (isWinner) Debug.Log($"<color=green>PUZZLE START:</color> The winner is {books[i].gameObject.name}");
        }

        if (hintText != null) hintText.enabled = false;
    }

    public void OnBookInteracted(BookInteract book)
    {
        if (puzzleSolved) return;

        if (audioSource != null && pickupSound != null)
            audioSource.PlayOneShot(pickupSound);

        if (book.CheckIfCorrect())
        {
            puzzleSolved = true;
            StartCoroutine(ShowHint(correctMessage));

            if (audioSource != null && successSound != null)
                audioSource.PlayOneShot(successSound);

            OpenDoor();
        }
        else
        {
            StartCoroutine(ShowHint(wrongMessage));
        }
    }

    private void OpenDoor()
    {
        if (door != null)
        {
            Quaternion targetRotation = Quaternion.Euler(door.transform.eulerAngles - new Vector3(0, 90, 0));
            StartCoroutine(RotateDoor(door, targetRotation));
        }
    }

    private IEnumerator RotateDoor(GameObject doorObj, Quaternion targetRot)
    {
        Debug.Log("Door is now opening...");
        Quaternion startRot = doorObj.transform.rotation;
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * doorOpenSpeed;
            doorObj.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }
        Debug.Log("Door opening complete.");
    }

    private IEnumerator ShowHint(string message)
    {
        if (hintText == null) yield break;
        hintText.text = message;
        hintText.enabled = true;
        yield return new WaitForSeconds(hintDuration);
        hintText.enabled = false;
    }
}