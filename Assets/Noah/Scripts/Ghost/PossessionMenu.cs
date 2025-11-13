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

    [Header("Button Settings")]
    public Sprite explorerIcon;
    public Sprite defaultObjectIcon;

    [Header("Controller Navigation")]
    public Button firstSelectedButton;

    private List<Button> menuButtons = new List<Button>();
    private System.Action<GameObject> onSelectObject;

    // Store unique possessable types to avoid duplicates
    private Dictionary<string, (PossessableObject reference, int count)> uniquePossessables = new();

    public void Initialize(System.Action<GameObject> onSelectCallback)
    {
        onSelectObject = onSelectCallback;

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
        onSelectObject = null;
    }

    public void UpdateMenu(List<GameObject> possessedObjects)
    {
        // Clear existing UI
        foreach (Transform child in buttonParent)
            Destroy(child.gameObject);

        menuButtons.Clear();
        uniquePossessables.Clear();

        // Always add Explorer option first
        AddMenuButton("Explorer", explorerIcon, null);

        // Build unique list of object types
        foreach (var obj in possessedObjects)
        {
            if (obj == null) continue;

            PossessableObject p = obj.GetComponent<PossessableObject>();
            string key = p != null && !string.IsNullOrEmpty(p.displayName)
                ? p.displayName
                : GetDisplayName(obj.name);

            if (uniquePossessables.ContainsKey(key))
            {
                var entry = uniquePossessables[key];
                entry.count++;
                uniquePossessables[key] = entry;
            }
            else
            {
                uniquePossessables[key] = (p, 1);
            }
        }

        // Create one button per unique object type
        foreach (var kvp in uniquePossessables)
        {
            string displayName = kvp.Key;
            int count = kvp.Value.count;
            PossessableObject p = kvp.Value.reference;

            Sprite icon = p != null && p.icon != null
                ? p.icon
                : defaultObjectIcon;

            string finalText = count > 1 ? $"{displayName} x{count}" : displayName;
            GameObject linkedObject = p != null ? p.gameObject : null;

            AddMenuButton(finalText, icon, linkedObject);
        }

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

    private void AddMenuButton(string displayText, Sprite icon, GameObject linkedObject)
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
            else
                Debug.LogWarning($"No Text or TMP_Text found in {buttonObj.name}");

            // Set icon
            Image iconImage = buttonObj.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = icon != null ? icon : defaultObjectIcon;
                iconImage.gameObject.SetActive(true);
            }

            GameObject objRef = linkedObject;
            button.onClick.AddListener(() => { OnButtonSelected(objRef); });

            menuButtons.Add(button);
        }
    }

    private string GetDisplayName(string originalName)
    {
        return originalName.Replace("(Clone)", "").Trim();
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

    private void OnButtonSelected(GameObject selectedObject)
    {
        onSelectObject?.Invoke(selectedObject);
        CloseMenu();
    }

    public void OpenMenu(List<GameObject> possessedObjects)
    {
        if (menuRoot != null)
        {
            menuRoot.SetActive(true);
            UpdateMenu(possessedObjects);
        }
    }

    public void CloseMenu()
    {
        if (menuRoot != null)
            menuRoot.SetActive(false);
    }
}
