using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonSelectSound : MonoBehaviour, ISelectHandler
{
    public MainMenuManager menu;

    public void OnSelect(BaseEventData eventData)
    {
        menu.PlayMoveSound();
    }
}