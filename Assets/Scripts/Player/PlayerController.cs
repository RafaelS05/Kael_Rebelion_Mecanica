using UnityEngine;

namespace Kael
{
    /// <summary>
    /// Movimiento de Kael: caminar, saltar (con coyote time y buffer), retroceso al recibir daño.
    /// Controles (Input Manager clásico): A/D o flechas para moverse, Espacio para saltar.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Health))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movimiento")]
        public float moveSpeed = 7f;
        public float acceleration = 70f;
        public float deceleration = 90f;

        [Header("Salto")]
        public float jumpVelocity = 14.5f;
        [Tooltip("Al soltar el botón antes de tiempo, el salto se corta (salto variable).")]
        public float jumpCutMultiplier = 0.5f;
        public float coyoteTime = 0.1f;
        public float jumpBufferTime = 0.12f;
        public float maxFallSpeed = 20f;

        [Header("Detección de suelo")]
        public LayerMask groundLayer;
        public float groundCheckDepth = 0.08f;

        [Header("Daño")]
        public Vector2 knockback = new Vector2(7f, 6f);
        public float knockbackLockTime = 0.25f;

        public int Facing { get; private set; } = 1;
        public bool IsGrounded { get; private set; }
        public bool InputEnabled { get; set; } = true;

        Rigidbody2D rb;
        Collider2D col;
        SpriteRenderer sr;
        Animator anim;
        Health health;

        float moveInput;
        float lastGroundedTime = -10f;
        float lastJumpPressedTime = -10f;
        float controlLockedUntil;
        bool isJumping;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            sr = GetComponentInChildren<SpriteRenderer>();
            anim = GetComponentInChildren<Animator>();
            health = GetComponent<Health>();
            health.OnDamaged += HandleDamaged;
            health.OnDeath += HandleDeath;
        }

        void Update()
        {
            if (Time.timeScale == 0f || !InputEnabled) { moveInput = 0f; return; }

            moveInput = Input.GetAxisRaw("Horizontal");

            if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                lastJumpPressedTime = Time.time;

            bool jumpHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
            if (isJumping && !jumpHeld && rb.velocity.y > 0f)
            {
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * jumpCutMultiplier);
                isJumping = false;
            }

            if (moveInput > 0.01f) Facing = 1;
            else if (moveInput < -0.01f) Facing = -1;
            if (sr != null) sr.flipX = Facing < 0;

            UpdateAnimator();
        }

        void FixedUpdate()
        {
            CheckGround();

            // Movimiento horizontal (bloqueado brevemente durante el retroceso)
            if (Time.time >= controlLockedUntil)
            {
                float target = moveInput * moveSpeed;
                float rate = Mathf.Abs(target) > 0.01f ? acceleration : deceleration;
                float vx = Mathf.MoveTowards(rb.velocity.x, target, rate * Time.fixedDeltaTime);
                rb.velocity = new Vector2(vx, rb.velocity.y);
            }

            // Salto con buffer + coyote time
            bool canJump = Time.time - lastGroundedTime <= coyoteTime;
            bool wantsJump = Time.time - lastJumpPressedTime <= jumpBufferTime;
            if (canJump && wantsJump && InputEnabled)
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpVelocity);
                lastJumpPressedTime = -10f;
                lastGroundedTime = -10f;
                isJumping = true;
                AudioManager.Play(Sfx.Jump);
            }

            if (rb.velocity.y < -maxFallSpeed)
                rb.velocity = new Vector2(rb.velocity.x, -maxFallSpeed);
        }

        void CheckGround()
        {
            Bounds b = col.bounds;
            Vector2 center = new Vector2(b.center.x, b.min.y - groundCheckDepth * 0.5f);
            Vector2 size = new Vector2(b.size.x * 0.9f, groundCheckDepth);
            bool touching = Physics2D.OverlapBox(center, size, 0f, groundLayer) != null;

            // Solo cuenta como "en el suelo" si no está subiendo (evita saltos dobles al atravesar plataformas)
            IsGrounded = touching && rb.velocity.y <= 0.05f;
            if (IsGrounded)
            {
                lastGroundedTime = Time.time;
                isJumping = false;
            }
        }

        void HandleDamaged(int amount, Vector2 source)
        {
            float dir = Mathf.Sign(transform.position.x - source.x);
            if (dir == 0f) dir = -Facing;
            rb.velocity = new Vector2(dir * knockback.x, knockback.y);
            controlLockedUntil = Time.time + knockbackLockTime;
            isJumping = false;
        }

        void HandleDeath()
        {
            InputEnabled = false;
            moveInput = 0f;
            rb.velocity = new Vector2(0f, rb.velocity.y);
            PlayerShooter shooter = GetComponent<PlayerShooter>();
            if (shooter != null) shooter.enabled = false;
        }

        // --- Animaciones (opcional) ---
        // Si se agrega un Animator con parámetros "Speed" (float), "Grounded" (bool) y "VelY" (float),
        // este script los actualiza solo. Si no hay Animator, no pasa nada.
        void UpdateAnimator()
        {
            if (anim == null || anim.runtimeAnimatorController == null) return;
            SetFloatIfExists("Speed", Mathf.Abs(rb.velocity.x));
            SetBoolIfExists("Grounded", IsGrounded);
            SetFloatIfExists("VelY", rb.velocity.y);
        }

        void SetFloatIfExists(string name, float v)
        {
            foreach (var p in anim.parameters)
                if (p.name == name && p.type == AnimatorControllerParameterType.Float) { anim.SetFloat(name, v); return; }
        }

        void SetBoolIfExists(string name, bool v)
        {
            foreach (var p in anim.parameters)
                if (p.name == name && p.type == AnimatorControllerParameterType.Bool) { anim.SetBool(name, v); return; }
        }

        void OnDrawGizmosSelected()
        {
            Collider2D c = GetComponent<Collider2D>();
            if (c == null) return;
            Bounds b = c.bounds;
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(new Vector3(b.center.x, b.min.y - groundCheckDepth * 0.5f),
                                new Vector3(b.size.x * 0.9f, groundCheckDepth));
        }
    }
}
