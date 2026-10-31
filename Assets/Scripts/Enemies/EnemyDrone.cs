using UnityEngine;

namespace Kael
{
    /// <summary>
    /// Electabot: dron que flota de lado a lado y suelta descargas eléctricas hacia abajo
    /// cuando Kael pasa debajo. Usa un Rigidbody2D Kinematic (no le afecta la gravedad).
    /// </summary>
    public class EnemyDrone : EnemyBase
    {
        public float patrolDistance = 3f;
        public float speed = 1.8f;
        public float hoverAmplitude = 0.25f;
        public float hoverSpeed = 3f;

        [Header("Ataque")]
        public Projectile boltPrefab;
        public float attackWidth = 1.2f;
        public float attackCooldown = 1.6f;
        public float boltSpeed = 7f;
        public Color boltColor = new Color(1f, 0.95f, 0.3f);

        Vector2 origin;
        float nextAttack;

        protected override void Start()
        {
            base.Start();
            origin = rb.position;
        }

        void FixedUpdate()
        {
            if (health.IsDead) return;

            Vector2 pos = rb.position;
            float targetX = pos.x + facing * speed * Time.fixedDeltaTime;
            if (Mathf.Abs(targetX - origin.x) > patrolDistance) SetFacing(-facing);
            float y = origin.y + Mathf.Sin(Time.time * hoverSpeed) * hoverAmplitude;
            rb.MovePosition(new Vector2(targetX, y));

            bool playerBelow = PlayerAlive && HorizontalDistanceToPlayer < attackWidth && player.position.y < transform.position.y;
            if (playerBelow && Time.time >= nextAttack && boltPrefab != null)
            {
                nextAttack = Time.time + attackCooldown;
                Projectile p = Instantiate(boltPrefab, transform.position + Vector3.down * 0.6f, Quaternion.identity);
                p.Launch(Team.Enemy, Vector2.down, boltSpeed, 1, boltColor);
            }
        }
    }
}
