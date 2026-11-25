using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Tajo horizontal de la armadura azul. No frena ni bloquea al jugador.</summary>
[RequireComponent(typeof(PlayerController2D), typeof(IceArmor))]
public class ArmorAttack : MonoBehaviour
{
    const int HitFrame = 2, FadeFrame = 3; // Fotogramas 3 y 4.

    [Header("Animación")]
    [SerializeField] Sprite[] frames = new Sprite[6];
    [SerializeField, Tooltip("Segundos por fotograma, uno por sprite.")] float[] durations = { 0.12f, 0.05f, 0.07f, 0.07f, 0.09f, 0.1f };

    [Header("Arco (posición mirando a la derecha)")]
    [SerializeField] SpriteRenderer arc;
    [SerializeField] Vector2 arcOffset = new Vector2(0.72f, 0.2f);

    [Header("Zona de impacto (mirando a la derecha)")]
    [SerializeField] Vector2 hitCenter = new Vector2(0.75f, 0.2f);
    [SerializeField] Vector2 hitSize = new Vector2(1.5f, 0.4f);

    [SerializeField] PlayerVisual visual;

    PlayerController2D player;
    IceArmor armor;
    InputAction attackAction;
    ContactFilter2D hitFilter;
    readonly List<Collider2D> overlaps = new List<Collider2D>();
    readonly HashSet<PossessedRobot> struck = new HashSet<PossessedRobot>();
    float clock;

    public bool IsAttacking { get; private set; }
    public bool IsHitActive { get; private set; }
    public bool FacingLeft { get; private set; }
    public int FrameIndex { get; private set; }
    public Sprite CurrentSprite => frames[FrameIndex];
    public event Action<PossessedRobot> Struck;

    void Awake()
    {
        player = GetComponent<PlayerController2D>();
        armor = GetComponent<IceArmor>();
        attackAction = InputSystem.actions.FindAction("Player/Attack", true);
        hitFilter.useTriggers = true;
        Cancel();
    }

    void OnEnable()
    {
        player.Jumped += Cancel;
        player.Respawned += Cancel;
    }

    void OnDisable()
    {
        player.Jumped -= Cancel;
        player.Respawned -= Cancel;
        Cancel();
    }

    void Update()
    {
        if (IsAttacking)
        {
            // Las pulsaciones durante el tajo se descartan: no hay cola ni encadenado.
            if (player.IsGrounded) Advance(Time.deltaTime);
            else Cancel();
        }
        else if (attackAction.WasPressedThisFrame() && armor.Unlocked && player.IsGrounded)
        {
            Begin();
        }
    }

    void Begin()
    {
        IsAttacking = true;
        FacingLeft = visual.FacingLeft;
        clock = 0f;
        FrameIndex = 0;
        struck.Clear();
    }

    void Advance(float dt)
    {
        float from = clock;
        clock += dt;

        float hitStart = StartOf(HitFrame), hitEnd = StartOf(FadeFrame);
        bool inWindow = clock >= hitStart && clock < hitEnd;
        bool skippedWindow = from < hitStart && clock >= hitEnd; // Un frame lento no puede saltársela.
        if (inWindow || skippedWindow) Strike();

        if (clock >= StartOf(frames.Length)) { Cancel(); return; }

        FrameIndex = 0;
        while (FrameIndex < frames.Length - 1 && clock >= StartOf(FrameIndex + 1)) FrameIndex++;
        IsHitActive = FrameIndex == HitFrame;

        arc.enabled = FrameIndex == HitFrame || FrameIndex == FadeFrame;
        float side = FacingLeft ? -1f : 1f;
        arc.flipX = FacingLeft;
        arc.transform.localPosition = new Vector3(arcOffset.x * side, arcOffset.y, 0f);
        Color c = arc.color;
        c.a = FrameIndex == FadeFrame ? 1f - (clock - hitEnd) / durations[FadeFrame] : 1f;
        arc.color = c;
    }

    float StartOf(int frame)
    {
        float t = 0f;
        for (int i = 0; i < frame; i++) t += durations[i];
        return t;
    }

    void Strike()
    {
        float side = FacingLeft ? -1f : 1f;
        Vector2 center = (Vector2)transform.position + new Vector2(hitCenter.x * side, hitCenter.y);
        Physics2D.OverlapBox(center, hitSize, 0f, hitFilter, overlaps);
        foreach (Collider2D other in overlaps)
            if (other.TryGetComponent(out PossessedRobot robot) && struck.Add(robot) && robot.TakeHit(side))
                Struck?.Invoke(robot);
    }

    void Cancel()
    {
        IsAttacking = IsHitActive = false;
        FrameIndex = 0;
        if (arc) arc.enabled = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.8f);
        float side = FacingLeft ? -1f : 1f;
        Gizmos.DrawWireCube(transform.position + new Vector3(hitCenter.x * side, hitCenter.y, 0f), hitSize);
    }
}
