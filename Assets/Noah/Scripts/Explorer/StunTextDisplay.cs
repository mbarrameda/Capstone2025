using UnityEngine;

public class StunTextDisplay : MonoBehaviour
{
    public static StunTextDisplay Instance;

    public GameObject stunTextObject; // Drag your StunText here in Inspector

    private float showTime = 0f;

    void Awake()
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

        // Start hidden
        if (stunTextObject != null)
            stunTextObject.SetActive(false);
    }

    void Update()
    {
        if (stunTextObject != null && stunTextObject.activeSelf && Time.time > showTime)
        {
            stunTextObject.SetActive(false);
        }
    }

    public void ShowStunText(float duration = 0f)
    {
        if (stunTextObject != null)
        {
            stunTextObject.SetActive(true);
            showTime = Time.time + duration;
        }
    }
}