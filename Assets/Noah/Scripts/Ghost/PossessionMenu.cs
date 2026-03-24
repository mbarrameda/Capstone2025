
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

        // Pull all options from GameManager
        if (GameManager.Instance != null)
        {
            var options = GameManager.Instance.GetTransformationMenuData();
            for (int i = 0; i < options.Length; i++)
                AddMenuButton(options[i].name, options[i].icon, i);

            // Resize the menu container to fit the buttons
            ResizeMenuContainer(options.Length);
        }
        else
        {
            Debug.LogWarning("PossessionMenu: GameManager.Instance is null, menu will be empty");
        }

        // Set up navigation
        SetupControllerNavigation();

        // Auto-select first button for controller support
        if (firstSelectedButton != null)
            EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);
        else if (menuButtons.Count > 0)
            EventSystem.current.SetSelectedGameObject(menuButtons[0].gameObject);
    }

    private void ResizeMenuContainer(int buttonCount)
    {
        if (buttonParent == null) return;

        // Get the height of a single button from the prefab
        float buttonHeight = 160f; // fallback default
        float spacing = 10f;


        if (buttonPrefab != null)
        {
            var rt = buttonPrefab.GetComponent<RectTransform>();
            if (rt != null) buttonHeight = rt.sizeDelta.y;

            // Also try to read spacing from a VerticalLayoutGroup if one exists
            var layout = buttonParent.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            if (layout != null) spacing = layout.spacing;
        }

        float totalHeight = buttonCount * buttonHeight + Mathf.Max(0, buttonCount - 1) * spacing;

        // Resize buttonParent
        RectTransform parentRT = buttonParent.GetComponent<RectTransform>();
        if (parentRT != null)
        {
            var size = parentRT.sizeDelta;
            size.y = totalHeight;
            parentRT.sizeDelta = size;
        }

        // Also resize menuRoot if it exists and is separate from buttonParent
        if (menuRoot != null && menuRoot.transform != buttonParent)
        {
            RectTransform rootRT = menuRoot.GetComponent<RectTransform>();
            if (rootRT != null)
            {
                var size = rootRT.sizeDelta;
                size.y = totalHeight + 300f; // small padding around the content
                rootRT.sizeDelta = size;
            }
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
