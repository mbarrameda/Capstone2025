using UnityEngine;
using TMPro;

public class SimpleUIHintTMP : MonoBehaviour
{
    [Header("Hint UI Settings")]
    public TextMeshProUGUI hintText; 

    public void ShowHint(string message)
    {
        if (hintText != null)
        {
            hintText.text = message;
            hintText.gameObject.SetActive(true);
        }
    }

    public void HideHint()
    {
        if (hintText != null)
        {
            hintText.gameObject.SetActive(false);
        }
    }
}
