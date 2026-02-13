
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PossessionMenu : MonoBehaviour
{
    [Header("UI References")]
    public GameObject buttonPrefab;
    public Transform buttonParent;
    public GameObject menuRoot;

    [Header("Fallback Icons (used if settings don't have icons)")]
    public Sprite explorerIcon;
    public Sprite wallIcon;
    public Sprite otherIcon;

    [Header("Fallback Display Names")]
    public string explorerDisplayName = "Explorer";
    public string wallDisplayName = "Wall";
    public string otherDisplayName = "Other";

    [Header("Controller Navigation")]
    public Button firstSelectedButton;

    private List<Button> menuButtons = new List<Button>();
    private System.Action<int> onSelectTransformation;

    public void Initialize(System.Action<int> onSelectCallback)
    {
        onSelectTransformation = onSelectCallback;

        if (menuRoot != null)
            menuRoot.SetActive(false);

        foreach (Transform child in buttonParent)
        {
            Button btn = child.GetComponent<Button>();
            if (btn != null)
                btn.onClick.RemoveAllListeners();
        }
    }

    public void ClearCallbacks()
    {
        onSelectTransformation = null;
    }

    public void UpdateMenu()
    {
        // Clear existing UI
        foreach (Transform child in buttonParent)
            Destroy(child.gameObject);

        menuButtons.Clear();

        // Start with fallback values
        string explorerName = explorerDisplayName;
        Sprite explorerSprite = explorerIcon;

        string wallName = wallDisplayName;
        Sprite wallSprite = wallIcon;

        string otherName = otherDisplayName;
        Sprite otherSprite = otherIcon;

        // 🔥 NEW: Pull names/icons from GameManager settings
        if (GameManager.Instance != null)
        {
            // Get explorer settings
            if (GameManager.Instance.explorerSettings != null)
            {
                var settings = GameManager.Instance.explorerSettings;
                if (!string.IsNullOrEmpty(settings.displayName))
                {
                    explorerName = settings.displayName;
                    Debug.Log($"Using explorer name from settings: {explorerName}");
                }
                if (settings.icon != null)
                {
                    explorerSprite = settings.icon;
                    Debug.Log("Using explorer icon from settings");
                }
            }

            // Get wall settings
            if (GameManager.Instance.wallSettings != null)
            {
                var settings = GameManager.Instance.wallSettings;
                if (!string.IsNullOrEmpty(settings.displayName))
                {
                    wallName = settings.displayName;
                    Debug.Log($"Using wall name from settings: {wallName}");
                }
                if (settings.icon != null)
                {
                    wallSprite = settings.icon;
                    Debug.Log("Using wall icon from settings");
                }
            }

            // Get other settings
            if (GameManager.Instance.otherSettings != null)
            {
                var settings = GameManager.Instance.otherSettings;
                if (!string.IsNullOrEmpty(settings.displayName))
                {
                    otherName = settings.displayName;
                    Debug.Log($"Using other name from settings: {otherName}");
                }
                if (settings.icon != null)
                {
                    otherSprite = settings.icon;
                    Debug.Log("Using other icon from settings");
                }
            }
        }

        // Add 3 fixed options with correct names/icons
        AddMenuButton(explorerName, explorerSprite, 0);
        AddMenuButton(wallName, wallSprite, 1);
        AddMenuButton(otherName, otherSprite, 2);

        // Set up navigation
        SetupControllerNavigation();

        // Auto-select first button for controller support
        if (firstSelectedButton != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);
        }
        else if (menuButtons.Count > 0)
        {
            EventSystem.current.SetSelectedGameObject(menuButtons[0].gameObject);
        }
    }

    private void AddMenuButton(string displayText, Sprite icon, int transformIndex)
    {
        if (buttonPrefab == null || buttonParent == null)
        {
            Debug.LogError("Button prefab or parent is not assigned!");
            return;
        }

        var buttonObj = Instantiate(buttonPrefab, buttonParent);
        Button button = buttonObj.GetComponent<Button>();

        if (button != null)
        {
            // Set text
            var textComponent = buttonObj.GetComponentInChildren<UnityEngine.UI.Text>();
            var tmpTextComponent = buttonObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();

            if (tmpTextComponent != null)
                tmpTextComponent.text = displayText;
            else if (textComponent != null)
                textComponent.text = displayText;

            // Set icon
            Image iconImage = buttonObj.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null)
            {
                if (icon != null)
                {
                    iconImage.sprite = icon;
                    iconImage.gameObject.SetActive(true);
                    iconImage.enabled = true;
                }
                else
                {
                    // No icon assigned - hide or use placeholder
                    Debug.LogWarning($"No icon for {displayText}");
                    iconImage.enabled = false;
                }
            }

            // Add click listener
            int index = transformIndex;
            button.onClick.AddListener(() => { OnButtonSelected(index); });

            menuButtons.Add(button);
        }
    }

    private void SetupControllerNavigation()
    {
        for (int i = 0; i < menuButtons.Count; i++)
        {
            Navigation nav = new Navigation { mode = Navigation.Mode.Explicit };

            if (i > 0)
                nav.selectOnUp = menuButtons[i - 1];
            if (i < menuButtons.Count - 1)
                nav.selectOnDown = menuButtons[i + 1];

            menuButtons[i].navigation = nav;
        }
    }

    private void OnButtonSelected(int transformIndex)
    {
        Debug.Log($"🎯 Selected transformation: {transformIndex}");
        onSelectTransformation?.Invoke(transformIndex);
        CloseMenu();
    }

    public void OpenMenu()
    {
        if (menuRoot != null)
        {
            menuRoot.SetActive(true);
            UpdateMenu();
        }
    }

    public void CloseMenu()
    {
        if (menuRoot != null)
            menuRoot.SetActive(false);
    }
}
