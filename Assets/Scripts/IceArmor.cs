using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Poder de la armadura azul: un peldaño de hielo por cada visita a suelo firme.</summary>
[RequireComponent(typeof(PlayerController2D))]
public class IceArmor : MonoBehaviour
{
    [Header("Hielo")]
    [SerializeField, Min(0.1f), Tooltip("Segundos que dura el peldaño.")] float iceDuration = 2.5f;
    [SerializeField, Tooltip("Ancho y grosor del peldaño.")] Vector2 stepSize = new Vector2(2.4f, 0.25f);
    [SerializeField, Min(0f), Tooltip("Separación entre botas y peldaño.")] float gapBelowBoots = 0.03f;
    [SerializeField, Tooltip("Si creas hielo subiendo, frena el ascenso para caer sobre él en vez de pasarlo de largo.")] bool stopAscent = true;
    [SerializeField] IceStep stepPrefab;

    PlayerController2D player;
    InputAction iceAction;
    readonly List<IceStep> steps = new List<IceStep>();
    bool pressed;

    public bool Unlocked { get; private set; }
    public bool Charged { get; private set; }
    /// <summary>Avisos para presentación: primer núcleo y carga recuperada en suelo firme.</summary>
    public event Action CoreCollected, Recharged;
    /// <summary>Se emite después de crear el peldaño; solo para presentación.</summary>
    public event Action StepCreated;

    void Awake()
    {
        player = GetComponent<PlayerController2D>();
        iceAction = InputSystem.actions.FindAction("Player/IceStep", true);
    }

    void Update()
    {
        if (iceAction.WasPressedThisFrame()) pressed = true;
    }

    void FixedUpdate()
    {
        if (Unlocked && !Charged && player.IsOnSolidGround)
        {
            Charged = true;
            Recharged?.Invoke();
        }

        if (pressed && Unlocked && Charged && !player.IsGrounded) CreateStep();
        pressed = false; // Pulsar en el suelo no se guarda para más tarde.
    }

    public void Unlock()
    {
        if (Unlocked) return;
        Unlocked = true;
        Charged = true;
        CoreCollected?.Invoke();
    }

    void CreateStep()
    {
        var center = new Vector3(transform.position.x, player.FeetY - gapBelowBoots - stepSize.y * 0.5f, 0f);
        IceStep step = Instantiate(stepPrefab, center, Quaternion.identity);
        step.Begin(stepSize, iceDuration);
        steps.RemoveAll(s => !s);
        steps.Add(step);
        if (stopAscent) player.StopAscent();
        Charged = false;
        StepCreated?.Invoke();
    }

    public void ClearSteps()
    {
        foreach (IceStep s in steps) if (s) Destroy(s.gameObject);
        steps.Clear();
    }
}
