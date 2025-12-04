using UnityEngine;

public class MinimapFollow : MonoBehaviour
{
    public Transform target;

    void LateUpdate()
    {
        Vector3 newPos = target.position;
        newPos.y = transform.position.y;
        transform.position = newPos;

        // Rotate only on Y
        transform.rotation = Quaternion.Euler(90f, target.eulerAngles.y, 0f);
    }
}
