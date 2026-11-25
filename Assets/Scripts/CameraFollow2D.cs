using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector2 offset = new Vector2(1.5f, 1.2f);
    [SerializeField, Min(0f)] float smoothTime = 0.15f;
    [SerializeField, Tooltip("Límites X del centro de la cámara.")] Vector2 limitsX = new Vector2(-2f, 30f);
    [SerializeField, Tooltip("Límites Y del centro de la cámara.")] Vector2 limitsY = new Vector2(1.5f, 4f);
    Vector3 velocity;

    void LateUpdate()
    {
        if (!target) return;
        var goal = new Vector3(
            Mathf.Clamp(target.position.x + offset.x, limitsX.x, limitsX.y),
            Mathf.Clamp(target.position.y + offset.y, limitsY.x, limitsY.y),
            transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
    }
}
