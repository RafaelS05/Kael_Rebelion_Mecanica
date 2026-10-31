using UnityEngine;

namespace Kael
{
    /// <summary>
    /// Hace daño a quien lo toque si es del equipo contrario.
    /// Sirve para enemigos (collider sólido) y trampas como espinas (collider trigger).
    /// </summary>
    public class DamageOnContact : MonoBehaviour
    {
        public Team team = Team.Enemy;
        public int damage = 1;

        void OnCollisionStay2D(Collision2D collision) => TryDamage(collision.collider);
        void OnTriggerStay2D(Collider2D other) => TryDamage(other);

        void TryDamage(Collider2D other)
        {
            if (!enabled) return;
            Health h = other.GetComponentInParent<Health>();
            if (h == null || h.team == team || h.IsDead) return;
            h.TakeDamage(damage, transform.position);
        }
    }
}
