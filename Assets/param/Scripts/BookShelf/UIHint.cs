using UnityEngine;
using TMPro;

/// <summary>
/// Attach this script to a TextMeshProUGUI component.
/// Used to show/hide the "Press E to interact" prompt.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class SimpleUIHintTMP : MonoBehaviour
{
    private TextMeshProUGUI uiText;

    private void Awake()
    {
        uiText = GetComponent<TextMeshProUGUI>();
        if (uiText == null)
        {
            Debug.LogError("SimpleUIHintTMP requires a TextMeshProUGUI component.");
        }
        HideHint();
    }

    /// <summary>
    /// Displays a hint message.
    /// </summary>
    public void ShowHint(string message)
    {
        if (uiText != null)
        {
            uiText.text = message;
            uiText.enabled = true;
        }
    }

    /// <summary>
    /// Hides the hint message.
    /// </summary>
    public void HideHint()
    {
        if (uiText != null)
        {
            uiText.text = "";
            uiText.enabled = false;
        }
    }
}
