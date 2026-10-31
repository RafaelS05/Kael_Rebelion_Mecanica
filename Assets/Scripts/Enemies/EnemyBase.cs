using UnityEngine;

namespace Kael
{
    /// <summary>Funciones comunes de todos los enemigos: dirección, detectar paredes/bordes y a Kael.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Health))]
    public abstract class EnemyBase : MonoBehaviour
    {
        public LayerMask groundLayer;
        [Tooltip("Dirección inicial: 1 derecha, -1 izquierda.")]
        public int startFacing = -1;

        protected Rigidbody2D rb;
        protected Collider2D col;
        protected SpriteRenderer sr;
        protected Health health;
        protected Transform player;
        protected Health playerHealth;
        protected int facing;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            sr = GetComponentInChildren<SpriteRenderer>();
            health = GetComponent<Health>();
        }

        protected virtual void Start()
        {
            PlayerController p = FindObjectOfType<PlayerController>();
            if (p != null)
            {
                player = p.transform;
                playerHealth = p.GetComponent<Health>();
            }
            SetFacing(startFacing);
        }

        protected bool PlayerAlive => player != null && playerHealth != null && !playerHealth.IsDead;

        /// <summary>Los sprites miran a la derecha por defecto.</summary>
        protected void SetFacing(int dir)
        {
            facing = dir >= 0 ? 1 : -1;
            if (sr != null) sr.flipX = facing < 0;
        }

        protected bool WallAhead()
        {
            Bounds b = col.bounds;
            Vector2 origin = new Vector2(b.center.x, b.center.y - b.extents.y * 0.5f);
            return Physics2D.Raycast(origin, Vector2.right * facing, b.extents.x + 0.1f, groundLayer).collider != null;
        }

        protected bool GroundAhead()
        {
            Bounds b = col.bounds;
            Vector2 origin = new Vector2(b.center.x + facing * (b.extents.x + 0.05f), b.min.y + 0.05f);
            return Physics2D.Raycast(origin, Vector2.down, 0.6f, groundLayer).collider != null;
        }

        protected float HorizontalDistanceToPlayer => PlayerAlive ? Mathf.Abs(player.position.x - transform.position.x) : float.MaxValue;
        protected float VerticalDistanceToPlayer => PlayerAlive ? Mathf.Abs(player.position.y - transform.position.y) : float.MaxValue;
        protected int DirectionToPlayer => PlayerAlive && player.position.x < transform.position.x ? -1 : 1;
    }
}
