using System;
using UnityEngine;

/// <summary>Música, ambiente y efectos. Solo escucha avisos del juego.</summary>
public class GameAudio : MonoBehaviour
{
    [Header("Volúmenes")]
    [SerializeField, Range(0f, 1f)] float musicVolume = 0.28f;
    [SerializeField, Range(0f, 1f)] float ambienceVolume = 0.2f;
    [SerializeField, Range(0f, 1f)] float effectsVolume = 0.8f;

    [Header("Bucles (vacío = en silencio)")]
    [SerializeField] AudioClip music;
    [SerializeField] AudioClip ambience;

    [Header("Efectos")]
    [SerializeField] AudioClip coreCollected;
    [SerializeField] AudioClip powerRecharged;
    [SerializeField] AudioClip iceLanding;
    [SerializeField, Tooltip("Provisional. Sustituir por Ice cracking hq.")] AudioClip stepCreated;

    [Header("Fragmento de la creación del peldaño")]
    [SerializeField, Min(0f), Tooltip("Segundo del clip en el que empieza.")] float stepClipStart;
    [SerializeField, Min(0.05f), Tooltip("Duración máxima, con fundido final.")] float stepClipLength = 0.6f;

    [Header("Referencias")]
    [SerializeField] PlayerController2D player;
    [SerializeField] IceArmor armor;
    [SerializeField] AudioSource musicSource, ambienceSource, effectsSource, stepSource;

    const float StepFade = 0.08f;

    /// <summary>Se emite cada vez que arranca un efecto.</summary>
    public event Action<AudioClip> EffectPlayed;

    void OnEnable()
    {
        armor.CoreCollected += OnCoreCollected;
        armor.Recharged += OnRecharged;
        armor.StepCreated += OnStepCreated;
        player.LandedOnIce += OnLandedOnIce;
    }

    void OnDisable()
    {
        armor.CoreCollected -= OnCoreCollected;
        armor.Recharged -= OnRecharged;
        armor.StepCreated -= OnStepCreated;
        player.LandedOnIce -= OnLandedOnIce;
    }

    void Start()
    {
        ApplyVolumes();
        StartLoop(musicSource, music);
        StartLoop(ambienceSource, ambience);
    }

    void Update() => ApplyVolumes(); // Permite ajustar los volúmenes durante Play.

    void ApplyVolumes()
    {
        musicSource.volume = musicVolume;
        ambienceSource.volume = ambienceVolume;
        effectsSource.volume = effectsVolume;
        float left = stepSource.isPlaying ? stepClipStart + stepClipLength - stepSource.time : StepFade;
        if (left <= 0f) stepSource.Stop();
        stepSource.volume = effectsVolume * Mathf.Clamp01(left / StepFade);
    }

    static void StartLoop(AudioSource source, AudioClip clip)
    {
        if (!clip) return;
        source.clip = clip;
        source.loop = true;
        source.Play();
    }

    void OnCoreCollected() => PlayEffect(coreCollected);
    void OnRecharged() => PlayEffect(powerRecharged);
    void OnLandedOnIce() => PlayEffect(iceLanding);

    void OnStepCreated()
    {
        if (!stepCreated) return;
        stepSource.clip = stepCreated;
        stepSource.volume = effectsVolume;
        stepSource.time = stepClipStart < stepCreated.length ? stepClipStart : 0f;
        stepSource.Play();
        EffectPlayed?.Invoke(stepCreated);
    }

    void PlayEffect(AudioClip clip)
    {
        if (!clip) return;
        effectsSource.PlayOneShot(clip);
        EffectPlayed?.Invoke(clip);
    }
}
