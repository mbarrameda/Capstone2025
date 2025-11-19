using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SceneUIManager : MonoBehaviour
{
    public static SceneUIManager Instance;

    [Header("Explorer UI References")]
    public Slider sanitySlider;
    public Image sanityFill;
    public Slider staminaSlider;
    public Image staminaFill;

    [Header("Ghost UI References")]
    public Slider fearBar;
    public Image fearFill;
    public TextMeshProUGUI stunStatusText;
    public Image phaseButton;
    public Image transformButton;
    public Image possessButton;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}