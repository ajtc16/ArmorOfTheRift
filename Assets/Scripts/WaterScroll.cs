using UnityEngine;

/// <summary>Desplaza en bucle una fila de tiles de agua. Solo dibujo.</summary>
public class WaterScroll : MonoBehaviour
{
    [SerializeField, Tooltip("Unidades por segundo; negativo = hacia la izquierda.")] float speed = -0.25f;
    [SerializeField, Min(0.01f), Tooltip("Ancho del tile que se repite.")] float period = 1.7f;
    [SerializeField, Min(0f)] float bob = 0.03f;

    Vector3 origin;

    void Awake() => origin = transform.localPosition;

    void Update()
    {
        // Siempre entre -period y 0: las filas llevan un tile sobrante a la derecha.
        float x = Mathf.Repeat(Time.time * speed, period) - period;
        transform.localPosition = origin + new Vector3(x, Mathf.Sin(Time.time * 1.6f) * bob, 0f);
    }
}
