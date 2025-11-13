using UnityEngine;

public class FlashlightPuzzleDetector : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionRange = 10f;
    public LayerMask detectionMask; // Optional: set to "PuzzleObject" layer for precision

    private Phone phone;
    private Camera playerCam;

    private void Start()
    {
        phone = GetComponent<Phone>();
        if (phone != null && phone.explorer != null)
        {
            playerCam = phone.explorer.cameraTransform?.GetComponent<Camera>();
            if (playerCam == null)
                playerCam = Camera.main;
        }
        else
        {
            playerCam = Camera.main;
        }
    }

    private void Update()
    {
        if (phone == null || !phone.gameObject.activeInHierarchy) return;
        if (playerCam == null || phone.flashlight == null) return;

        // Only detect if flashlight is actually ON
        if (!phone.flashlight.enabled) return;

        Ray ray = new Ray(playerCam.transform.position, playerCam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, detectionRange, detectionMask))
        {
            LightReactiveObject reactive = hit.collider.GetComponent<LightReactiveObject>();
            if (reactive != null)
            {
                reactive.ActivateGlow();
            }
        }
    }
}
