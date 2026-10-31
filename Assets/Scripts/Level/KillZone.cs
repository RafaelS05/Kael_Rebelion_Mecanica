using UnityEngine;

namespace Kael
{
    /// <summary>Zona bajo el nivel: Kael muere al caer, los enemigos se destruyen.</summary>
    public class KillZone : MonoBehaviour
    {
        void OnTriggerEnter2D(Collider2D other)
        {
            Health h = other.GetComponentInParent<Health>();
            if (h == null) return;
            if (h.team == Team.Player) h.Kill();
            else Destroy(h.gameObject);
        }
    }
}
