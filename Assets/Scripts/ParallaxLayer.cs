using UnityEngine;

/// <summary>Sigue a la cámara una fracción de su recorrido. 0 = fijo en el mundo, 1 = pegado a la cámara.</summary>
[DefaultExecutionOrder(100)]
public class ParallaxLayer : MonoBehaviour
{
    [SerializeField, Tooltip("Fracción del movimiento de cámara en X e Y.")] Vector2 follow = new Vector2(0.5f, 0f);

    Transform cam;
    Vector3 origin;
    Vector2 camOrigin;

    void Start()
    {
        cam = Camera.main.transform;
        origin = transform.position;
        camOrigin = cam.position;
    }

    void LateUpdate() // Después de CameraFollow2D.
    {
        Vector2 moved = (Vector2)cam.position - camOrigin;
        transform.position = origin + new Vector3(moved.x * follow.x, moved.y * follow.y, 0f);
    }
}
