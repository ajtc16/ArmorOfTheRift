using System;
using UnityEngine;

/// <summary>Elige el sprite según el estado del jugador. Solo lee el juego: nunca mueve ni bloquea.</summary>
public class PlayerVisual : MonoBehaviour
{
    [Serializable]
    class Form
    {
        public Sprite[] idle, run;
        public Sprite takeoff, fall;
        [Tooltip("Opcional. Vacío = sin pose de aterrizaje.")] public Sprite[] landing;
    }

    [SerializeField] PlayerController2D player;
    [SerializeField] IceArmor armor;
    [SerializeField, Tooltip("Opcional.")] ArmorAttack attack;
    [SerializeField] Rigidbody2D body;
    [SerializeField] SpriteRenderer sprite;

    [Header("Formas")]
    [SerializeField] Form boy;
    [SerializeField] Form blueArmor;
    [SerializeField] Sprite[] invoke;

    [Header("Ritmo")]
    [SerializeField, Min(0.1f)] float idleFps = 1.6f;
    [SerializeField, Min(0.1f), Tooltip("Cuadros por unidad recorrida: el paso sigue a la velocidad real.")] float runFramesPerUnit = 1.9f;
    [SerializeField, Min(0f)] float takeoffTime = 0.16f;
    [SerializeField, Min(0.01f)] float landingFrameTime = 0.08f;
    [SerializeField, Min(0.01f)] float invokeFrameTime = 0.09f;
    [SerializeField, Tooltip("Altura local de las botas (base del collider).")] float feetLocalY = -0.8f;
    [SerializeField, Min(0f), Tooltip("Por encima de esta velocidad se considera carrera e interrumpe el aterrizaje.")] float runThreshold = 0.3f;

    [Header("Carga del hielo")]
    [SerializeField] SpriteRenderer chargeGem;
    [SerializeField] SpriteRenderer chargeGlow;
    [SerializeField] Color readyColor = new Color(0.45f, 0.95f, 1f);
    [SerializeField] Color spentColor = new Color(0.2f, 0.28f, 0.4f, 0.8f);

    float idleClock, runClock, takeoffLeft, landingClock = float.MaxValue, invokeClock = float.MaxValue, airTime;
    bool wasGrounded = true;

    public bool FacingLeft => sprite.flipX;

    void OnEnable()
    {
        player.Jumped += OnJumped;
        player.Respawned += OnRespawned;
        armor.StepCreated += OnStepCreated;
    }

    void OnDisable()
    {
        player.Jumped -= OnJumped;
        player.Respawned -= OnRespawned;
        armor.StepCreated -= OnStepCreated;
    }

    void OnJumped()
    {
        takeoffLeft = takeoffTime;
        landingClock = invokeClock = float.MaxValue;
    }

    void OnRespawned()
    {
        takeoffLeft = airTime = 0f;
        landingClock = invokeClock = float.MaxValue;
    }

    void OnStepCreated() => invokeClock = 0f;

    void LateUpdate()
    {
        float dt = Time.deltaTime, vx = body.linearVelocity.x, speed = Mathf.Abs(vx);
        bool running = speed > runThreshold;
        bool attacking = attack && attack.IsAttacking;
        if (attacking) sprite.flipX = attack.FacingLeft;
        else if (running) sprite.flipX = vx < 0f;

        Form form = armor.Unlocked ? blueArmor : boy;
        bool grounded = player.IsGrounded;
        if (grounded && !wasGrounded && airTime > 0.1f) landingClock = 0f;
        airTime = grounded ? 0f : airTime + dt;
        wasGrounded = grounded;

        if (attacking) invokeClock = float.MaxValue;
        sprite.sprite = attacking ? attack.CurrentSprite : Pick(form, grounded, running);
        // La física reposa hasta 0.05 sobre el suelo; se compensa solo en el dibujo.
        sprite.transform.localPosition = new Vector3(0f, feetLocalY - (grounded ? player.GroundGap : 0f), 0f);

        takeoffLeft -= dt;
        if (invokeClock < float.MaxValue) invokeClock += dt;
        if (landingClock < float.MaxValue) landingClock += dt;
        idleClock = running || !grounded ? 0f : idleClock + dt * idleFps;
        runClock = running && grounded ? runClock + speed * runFramesPerUnit * dt : 0f;

        UpdateCharge();
    }

    Sprite Pick(Form form, bool grounded, bool running)
    {
        if (invokeClock < invokeFrameTime * invoke.Length)
            return invoke[(int)(invokeClock / invokeFrameTime)];
        if (!grounded)
            return takeoffLeft > 0f ? form.takeoff : form.fall;
        if (running)
        {
            landingClock = float.MaxValue; // Correr interrumpe el aterrizaje.
            return form.run[(int)runClock % form.run.Length];
        }
        if (landingClock < landingFrameTime * form.landing.Length)
            return form.landing[(int)(landingClock / landingFrameTime)];
        return form.idle[(int)idleClock % form.idle.Length];
    }

    void UpdateCharge()
    {
        chargeGem.enabled = armor.Unlocked;
        chargeGlow.enabled = armor.Unlocked && armor.Charged;
        chargeGem.color = armor.Charged ? readyColor : spentColor;
        float pulse = armor.Charged ? 1f + 0.12f * Mathf.Sin(Time.time * 6f) : 0.8f;
        chargeGem.transform.localScale = new Vector3(0.2f, 0.2f, 1f) * pulse;
    }
}
