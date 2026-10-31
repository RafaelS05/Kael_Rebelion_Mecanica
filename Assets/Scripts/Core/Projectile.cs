using UnityEngine;

namespace Kael
{
    /// <summary>
    /// Proyectil genérico: disparos de Kael, descargas de enemigos y ondas de choque del jefe.
    /// Necesita Rigidbody2D (Dynamic, gravedad 0) y un Collider2D marcado como Trigger.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        public Team team = Team.Player;
        public int damage = 1;
        public float speed = 14f;
        public float lifetime = 2.5f;
        [Tooltip("Si es falso, atraviesa el suelo/paredes (ondas de choque).")]
        public bool destroyOnGround = true;
        public LayerMask groundLayer;

        Rigidbody2D rb;
        bool launched;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
        }

        void Start()
        {
            Destroy(gameObject, lifetime);
            if (!launched) Launch(team, Vector2.right, speed, damage, Color.white, false);
        }

        public void Launch(Team owner, Vector2 direction, float newSpeed, int newDamage, Color color, bool tint = true)
        {
            launched = true;
            team = owner;
            speed = newSpeed;
            damage = newDamage;
            rb.velocity = direction.normalized * speed;

            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                if (tint) sr.color = color;
                sr.flipX = direction.x < 0f;
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.isTrigger) return; // ignora zonas, pickups, otras balas

            Health h = other.GetComponentInParent<Health>();
            if (h != null)
            {
                if (h.team == team) return; // no daña a su propio equipo
                h.TakeDamage(damage, transform.position);
                Destroy(gameObject);
                return;
            }

            if (destroyOnGround && ((1 << other.gameObject.layer) & groundLayer) != 0)
                Destroy(gameObject);
        }
    }
}
