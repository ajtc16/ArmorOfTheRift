using UnityEngine;

/// <summary>Agua de un hueco. Devuelve al jugador al punto de reintento de ese hueco.</summary>
[RequireComponent(typeof(Collider2D))]
public class CanalWater : MonoBehaviour
{
    [SerializeField] Transform respawnPoint;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out PlayerController2D player)) player.Respawn(respawnPoint.position);
    }
}
