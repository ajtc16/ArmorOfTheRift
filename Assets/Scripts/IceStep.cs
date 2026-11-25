using UnityEngine;

/// <summary>Plataforma fija y temporal. Parpadea antes de romperse.</summary>
[RequireComponent(typeof(BoxCollider2D))]
public class IceStep : MonoBehaviour
{
    [SerializeField, Min(0f), Tooltip("Segundos de aviso antes de desaparecer.")] float warningTime = 0.7f;

    [SerializeField, Tooltip("Hijo con el dibujo. Su pivote está en la superficie de apoyo.")] SpriteRenderer visual;

    float timeLeft;

    public void Begin(Vector2 size, float duration)
    {
        transform.localScale = new Vector3(size.x, size.y, 1f);
        // El sprite mide 1 unidad de ancho útil: se compensa la escala del collider para no deformarlo.
        visual.transform.localScale = new Vector3(1f, size.x / size.y, 1f);
        visual.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        timeLeft = duration;
    }

    void Update()
    {
        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f) { Destroy(gameObject); return; }
        Color c = visual.color;
        c.a = timeLeft < warningTime && Mathf.Repeat(timeLeft, 0.16f) < 0.08f ? 0.35f : 1f;
        visual.color = c;
    }
}
