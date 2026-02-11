using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Simplified possession menu that shows 3 fixed transformation options:
/// Explorer, Wall, and Other object.
/// No possession system needed - these are always available.
/// </summary>
public class PossessionMenu : MonoBehaviour
{
    [Header("UI References")]
    public GameObject buttonPrefab;
    public Transform buttonParent;
    public GameObject menuRoot;

    [Header("Icons")]
    public Sprite explorerIcon;
    public Sprite wallIcon;
    public Sprite otherIcon;

    [Header("Display Names")]
    public string explorerDisplayName = "Explorer";
    public string wallDisplayName = "Wall";
    public string otherDisplayName = "Other";

    [Header("Controller Navigation")]
    public Button firstSelectedButton;

    private List<Button> menuButtons = new List<Button>();
    private System.Action<int> onSelectTransformation; // 0 = Explorer, 1 = Wall, 2 = Other

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

        // Add 3 fixed options
        AddMenuButton(explorerDisplayName, explorerIcon, 0);
        AddMenuButton(wallDisplayName, wallIcon, 1);
        AddMenuButton(otherDisplayName, otherIcon, 2);

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
            if (iconImage != null && icon != null)
            {
                iconImage.sprite = icon;
                iconImage.gameObject.SetActive(true);
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