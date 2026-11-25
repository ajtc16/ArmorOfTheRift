using UnityEngine;

/// <summary>Núcleo azul: desbloquea el hielo al tocarlo.</summary>
[RequireComponent(typeof(Collider2D))]
public class IceCore : MonoBehaviour
{
    [SerializeField] float bobHeight = 0.15f;
    Vector3 origin;

    void Awake() => origin = transform.position;

    void Update() => transform.position = origin + Vector3.up * (Mathf.Sin(Time.time * 3f) * bobHeight);

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out IceArmor armor)) return;
        armor.Unlock();
        gameObject.SetActive(false);
    }
}
