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
    public Sprite chairIcon;
    public Sprite tableIcon;
    public Sprite defaultObjectIcon;

    [Header("Controller Navigation")]
    public Button firstSelectedButton;

    private List<Button> menuButtons = new List<Button>();
    private System.Action<GameObject> onSelectObject;

    public void Initialize(System.Action<GameObject> onSelectCallback)
    {
        onSelectObject = onSelectCallback;

        if (menuRoot != null)
            menuRoot.SetActive(false);
    }

    public void UpdateMenu(List<GameObject> possessedObjects)
    {
        // Clear existing buttons
        foreach (Transform child in buttonParent)
            Destroy(child.gameObject);

        menuButtons.Clear();

        // Always add Explorer option first
        AddMenuButton("Explorer", explorerIcon, null);

        // Add possessed objects
        foreach (var obj in possessedObjects)
        {
            if (obj != null)
            {
                string displayName = GetDisplayName(obj.name);
                Sprite icon = GetIconForObject(obj.name);
                AddMenuButton(displayName, icon, obj);
            }
        }

        // Set up controller navigation
        SetupControllerNavigation();

        // Select first button for controller support
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
            // Set button text
            Text textComponent = buttonObj.GetComponentInChildren<Text>();
            if (textComponent != null)
            {
                textComponent.text = displayText;
            }

            // Set button icon if available
            Image iconImage = buttonObj.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null && icon != null)
            {
                iconImage.sprite = icon;
                iconImage.gameObject.SetActive(true);
            }

            // Store reference to the object this button represents
            GameObject objRef = linkedObject;

            button.onClick.AddListener(() => {
                OnButtonSelected(objRef);
            });

            menuButtons.Add(button);
        }
    }

    private string GetDisplayName(string originalName)
    {
        // Clean up the name for display
        return originalName.Replace("(Clone)", "").Trim();
    }

    private Sprite GetIconForObject(string objectName)
    {
        string cleanName = GetDisplayName(objectName).ToLower();

        switch (cleanName)
        {
            case "chair":
                return chairIcon;
            case "table":
                return tableIcon;
            default:
                return defaultObjectIcon;
        }
    }

    private void SetupControllerNavigation()
    {
        for (int i = 0; i < menuButtons.Count; i++)
        {
            Navigation nav = new Navigation();
            nav.mode = Navigation.Mode.Explicit;

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