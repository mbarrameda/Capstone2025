using UnityEngine;

public class BookInteract : MonoBehaviour
{
    [Header("Puzzle Connection")]
    public BookGroupManager bookManager;

    [HideInInspector][SerializeField] private bool isCorrectBook = false;

    [Header("Hover Settings")]
    [Tooltip("The text that appears in the Player's UI Hint.")]
    public string hoverMessage = "Pick up book";

    private bool hasBeenInteracted = false;
    private SimpleUIHintTMP playerHintUI;

    private void Start()
    {
        playerHintUI = FindObjectOfType<SimpleUIHintTMP>();
    }

    public void SetAsCorrect(bool state) => isCorrectBook = state;
    public bool CheckIfCorrect() => isCorrectBook;

    // Called by Player Raycast
    public void ShowHover()
    {
        if (!hasBeenInteracted && playerHintUI != null)
        {
            playerHintUI.ShowHint(hoverMessage);
        }
    }

    // Called by Player Raycast
    public void HideHover()
    {
        if (playerHintUI != null)
        {
            playerHintUI.HideHint();
        }
    }

    public void OnInteract()
    {
        if (hasBeenInteracted) return;

        if (bookManager == null) return;

        hasBeenInteracted = true;
        if (playerHintUI != null) playerHintUI.HideHint();

        bookManager.OnBookInteracted(this);
        gameObject.SetActive(false);
    }
}