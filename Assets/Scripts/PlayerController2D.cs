using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Movimiento lateral y salto. Distingue suelo firme de peldaños de hielo.</summary>
[DefaultExecutionOrder(-10)]
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField, Min(0f), Tooltip("Velocidad horizontal máxima (unidades/s).")] float moveSpeed = 7f;
    [SerializeField, Min(0f)] float groundAcceleration = 80f;
    [SerializeField, Min(0f)] float airAcceleration = 55f;

    [Header("Salto")]
    [SerializeField, Min(0.1f), Tooltip("Altura del salto manteniendo Espacio (unidades).")] float jumpHeight = 2.5f;
    [SerializeField, Min(0.05f), Tooltip("Segundos hasta el punto más alto.")] float timeToApex = 0.4f;
    [SerializeField, Min(1f), Tooltip("Multiplica la gravedad al caer.")] float fallMultiplier = 1.25f;
    [SerializeField, Range(0f, 1f), Tooltip("Velocidad conservada al soltar Espacio subiendo. 1 = altura fija.")] float jumpCut = 0.5f;
    [SerializeField, Min(1f)] float maxFallSpeed = 18f;
    [SerializeField, Min(0f), Tooltip("Margen para saltar tras dejar un borde.")] float coyoteTime = 0.08f;
    [SerializeField, Min(0f), Tooltip("Margen para pulsar Espacio antes de aterrizar.")] float jumpBuffer = 0.12f;

    const float GroundProbe = 0.05f;

    Rigidbody2D body;
    Collider2D hitbox;
    InputAction moveAction, jumpAction;
    readonly RaycastHit2D[] hits = new RaycastHit2D[8];
    ContactFilter2D groundFilter;
    float coyoteLeft, bufferLeft;
    bool cutRequested, jumpRising;

    public bool IsGrounded { get; private set; }
    /// <summary>Apoyado en algo que no es hielo.</summary>
    public bool IsOnSolidGround { get; private set; }
    /// <summary>Avisos para la presentación; no afectan a la física.</summary>
    public event Action Jumped, Respawned, LandedOnIce;
    /// <summary>Separación física con el apoyo (máx. GroundProbe). 0 en el aire.</summary>
    public float GroundGap { get; private set; }
    public float FeetY => hitbox.bounds.min.y;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<Collider2D>();
        body.gravityScale = 0f; // La gravedad se deriva de la altura y el tiempo de salto.
        groundFilter.useTriggers = false;
        moveAction = InputSystem.actions.FindAction("Player/Move", true);
        jumpAction = InputSystem.actions.FindAction("Player/Jump", true);
    }

    void Update()
    {
        bufferLeft -= Time.deltaTime;
        if (jumpAction.WasPressedThisFrame()) { bufferLeft = jumpBuffer + Time.fixedDeltaTime; cutRequested = false; }
        if (jumpAction.WasReleasedThisFrame()) cutRequested = true;
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        Vector2 v = body.linearVelocity;
        float gravity = 2f * jumpHeight / (timeToApex * timeToApex);

        ProbeGround(v.y);
        coyoteLeft = IsGrounded ? coyoteTime + dt : coyoteLeft - dt;
        if (IsGrounded) jumpRising = false;

        float x = moveAction.ReadValue<Vector2>().x;
        float target = Mathf.Abs(x) > 0.25f ? Mathf.Sign(x) * moveSpeed : 0f;
        v.x = Mathf.MoveTowards(v.x, target, (IsGrounded ? groundAcceleration : airAcceleration) * dt);

        if (!IsGrounded)
            v.y = Mathf.Max(v.y - gravity * (v.y < 0f ? fallMultiplier : 1f) * dt, -maxFallSpeed);

        if (bufferLeft > 0f && coyoteLeft > 0f)
        {
            // Medio paso de compensación: el integrador semiimplícito alcanza así jumpHeight.
            v.y = gravity * timeToApex - gravity * dt * 0.5f;
            bufferLeft = coyoteLeft = 0f;
            IsGrounded = IsOnSolidGround = false;
            jumpRising = true;
            Jumped?.Invoke();
        }

        if (jumpRising && cutRequested && v.y > 0f) { v.y *= jumpCut; jumpRising = false; }
        if (v.y <= 0f) jumpRising = false;

        body.linearVelocity = v;
    }

    void ProbeGround(float verticalSpeed)
    {
        bool wasGrounded = IsGrounded;
        IsGrounded = IsOnSolidGround = false;
        GroundGap = 0f;
        if (verticalSpeed > 0.1f) return;
        int count = body.Cast(Vector2.down, groundFilter, hits, GroundProbe);
        for (int i = 0; i < count; i++)
        {
            if (hits[i].normal.y < 0.7f) continue;
            GroundGap = IsGrounded ? Mathf.Min(GroundGap, hits[i].distance) : hits[i].distance;
            IsGrounded = true;
            if (!hits[i].collider.TryGetComponent<IceStep>(out _)) IsOnSolidGround = true;
        }
        if (IsGrounded && !IsOnSolidGround && !wasGrounded) LandedOnIce?.Invoke();
    }

    public void StopAscent()
    {
        Vector2 v = body.linearVelocity;
        body.linearVelocity = new Vector2(v.x, Mathf.Min(v.y, 0f));
        jumpRising = false;
    }

    public void Respawn(Vector2 position)
    {
        body.linearVelocity = Vector2.zero;
        body.position = position;
        transform.position = position;
        coyoteLeft = bufferLeft = 0f;
        jumpRising = IsGrounded = IsOnSolidGround = false;
        if (TryGetComponent(out IceArmor armor)) armor.ClearSteps();
        Respawned?.Invoke();
    }
}
