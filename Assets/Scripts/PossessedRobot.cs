using UnityEngine;

/// <summary>Patrulla lenta entre dos puntos. Tocarlo usa el mismo reintento que el canal.</summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PossessedRobot : MonoBehaviour
{
    [Header("Patrulla")]
    [SerializeField] Transform pointA;
    [SerializeField] Transform pointB;
    [SerializeField, Min(0f), Tooltip("Unidades por segundo.")] float speed = 0.8f;
    [SerializeField, Min(0f), Tooltip("Pausa en cada extremo antes de girar.")] float turnPause = 0.7f;

    [Header("Contacto")]
    [SerializeField] Transform respawnPoint;

    [Header("Aspecto")]
    [SerializeField, Tooltip("Raíz del dibujo; se voltea al girar. El arte mira a la derecha.")] Transform visual;
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] Transform visorGlow;
    [SerializeField] Sprite[] idle, walk;
    [SerializeField, Min(0.1f)] float idleFps = 10f;
    [SerializeField, Min(0.1f), Tooltip("Cuadros por unidad recorrida.")] float walkFramesPerUnit = 8f;
    [SerializeField] float visorIdleY = 1.084f, visorWalkY = 1.136f;

    [Header("Reacción al golpe")]
    [SerializeField, Min(0.05f)] float reactionTime = 0.45f;
    [SerializeField, Min(0f)] float recoil = 0.2f;
    [SerializeField] Color hitColor = new Color(0.45f, 0.85f, 1f);
    [SerializeField] Color disabledColor = new Color(0.4f, 0.4f, 0.45f);

    Rigidbody2D body;
    float reactionClock, hitDirection, pauseLeft, idleClock, walkClock;
    int direction = 1;

    public bool IsPaused => pauseLeft > 0f;
    public bool IsDisabled { get; private set; }

    void Awake() => body = GetComponent<Rigidbody2D>();

    void FixedUpdate()
    {
        if (IsDisabled) return;
        float dt = Time.fixedDeltaTime;
        if (pauseLeft > 0f)
        {
            pauseLeft -= dt;
            if (pauseLeft <= 0f) direction = -direction;
            return;
        }
        float min = Mathf.Min(pointA.position.x, pointB.position.x);
        float max = Mathf.Max(pointA.position.x, pointB.position.x);
        float goal = direction > 0 ? max : min;
        float x = Mathf.MoveTowards(body.position.x, goal, speed * dt);
        body.MovePosition(new Vector2(x, body.position.y));
        if (Mathf.Approximately(x, goal)) pauseLeft = Mathf.Max(turnPause, dt);
    }

    void LateUpdate()
    {
        if (IsDisabled) { React(); return; }
        bool paused = IsPaused;
        idleClock = paused ? idleClock + Time.deltaTime * idleFps : 0f;
        walkClock = paused ? 0f : walkClock + Time.deltaTime * speed * walkFramesPerUnit;
        sprite.sprite = paused ? idle[(int)idleClock % idle.Length] : walk[(int)walkClock % walk.Length];
        visual.localScale = new Vector3(direction, 1f, 1f);
        Vector3 glow = visorGlow.localPosition;
        visorGlow.localPosition = new Vector3(glow.x, paused ? visorIdleY : visorWalkY, glow.z);
    }

    /// <summary>Apaga al robot: deja de patrullar y de dañar en el acto.</summary>
    /// <returns>false si ya estaba apagado.</returns>
    public bool TakeHit(float direction)
    {
        if (IsDisabled) return false;
        IsDisabled = true;
        GetComponent<Collider2D>().enabled = false;
        hitDirection = Mathf.Sign(direction);
        sprite.sprite = idle[0];
        visorGlow.gameObject.SetActive(false);
        return true;
    }

    void React()
    {
        reactionClock += Time.deltaTime;
        float t = Mathf.Clamp01(reactionClock / reactionTime);
        float shake = Mathf.Sin(reactionClock * 70f) * 0.06f * (1f - t);
        visual.localPosition = new Vector3(hitDirection * recoil * Mathf.Sqrt(t) + shake, 0f, 0f);
        bool flash = t < 1f && Mathf.Repeat(reactionClock, 0.12f) < 0.06f;
        sprite.color = flash ? hitColor : Color.Lerp(Color.white, disabledColor, t);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsDisabled && other.TryGetComponent(out PlayerController2D player)) player.Respawn(respawnPoint.position);
    }
}
