using UnityEngine;
using TMPro;
using System.Collections; // Required for Coroutines

public class SimpleUIHintTMP : MonoBehaviour
{
    [Header("Hint UI Settings")]
    public TextMeshProUGUI hintText;
    private Coroutine hideCoroutine;

    // Added an optional 'duration' parameter
    public void ShowHint(string message, float duration = 2f)
    {
        if (hintText != null)
        {
            // Stop any existing timer so they don't overlap
            if (hideCoroutine != null) StopCoroutine(hideCoroutine);

            hintText.text = message;
            hintText.gameObject.SetActive(true);

            // Start a timer to hide the hint
            hideCoroutine = StartCoroutine(HideAfterDelay(duration));
        }
    }

    public void HideHint()
    {
        if (hintText != null)
        {
            hintText.gameObject.SetActive(false);
        }
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideHint();
        hideCoroutine = null;
    }
}