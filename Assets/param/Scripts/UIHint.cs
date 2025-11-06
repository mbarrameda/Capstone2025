using UnityEngine;
using TMPro;

/// <summary>
/// Displays on-screen "Press □ / X to Interact" hints using TextMeshProUGUI.
/// Attach this to a TMP text element in your Canvas.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class SimpleUIHintTMP : MonoBehaviour
{
    public TextMeshProUGUI uiText;

    private void Awake()
    {
        uiText = GetComponent<TextMeshProUGUI>();
        HideHint();
    }

    /// <summary>
    /// Show a custom interaction hint.
    /// </summary>
    public void ShowHint(string message)
    {
        if (uiText == null) return;
        uiText.text = message;
        uiText.enabled = true;
    }

    /// <summary>
    /// Hide the current hint.
    /// </summary>
    public void HideHint()
    {
        if (uiText == null) return;
        uiText.text = "";
        uiText.enabled = false;
    }
}
