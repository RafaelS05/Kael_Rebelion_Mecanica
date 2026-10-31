using System;
using System.Collections;
using UnityEngine;

namespace Kael
{
    /// <summary>
    /// Oxibot Gigante (primer jefe). Patrón fijo y fácil de aprender:
    ///   Caminar → Anticipar golpe (se pone naranja) → Golpe al suelo (lanza 2 ondas de choque)
    ///   → Anticipar embestida (se pone rojo) → Embestida → Aturdido (ventana para dispararle) → repetir.
    /// Bajo 50% de vida se vuelve un poco más rápido. Al morir suelta el arma mejorada y abre la puerta.
    /// </summary>
    public class BossOxibot : EnemyBase
    {
        public static event Action<BossOxibot> OnBossActivated;
        public static event Action<BossOxibot> OnBossDefeated;

        public string bossName = "Oxibot Gigante";
        public float activationDistance = 11f;

        [Header("Caminar")]
        public float walkSpeed = 2f;
        public float walkTime = 1.5f;

        [Header("Golpe al suelo")]
        public float slamTelegraph = 0.8f;
        public Projectile shockwavePrefab;
        public float shockwaveSpeed = 7f;
        public Color slamColor = new Color(1f, 0.6f, 0.2f);

        [Header("Embestida")]
        public float chargeTelegraph = 0.6f;
        public float chargeSpeed = 10f;
        public float maxChargeTime = 2.2f;
        public float stunTime = 1.3f;
        public Color chargeColor = new Color(1f, 0.25f, 0.25f);
        public Color stunColor = new Color(0.55f, 0.6f, 0.75f);

        [Header("Al morir")]
        public GameObject dropOnDeath;
        public GameObject[] doorsToOpen;

        public Health Health => health;
        public bool IsActive { get; private set; }

        Color normalColor = Color.white;
        float speedMultiplier = 1f;
        float desiredVX;
        CameraFollow cameraFollow;

        protected override void Start()
        {
            base.Start();
            if (sr != null) normalColor = sr.color;
            health.OnDeath += HandleDeath;
            health.OnHealthChanged += (cur, max) => { if (cur <= max / 2) speedMultiplier = 1.3f; };
            if (Camera.main != null) cameraFollow = Camera.main.GetComponent<CameraFollow>();
        }

        void Update()
        {
            if (!IsActive && !health.IsDead && PlayerAlive && HorizontalDistanceToPlayer < activationDistance)
            {
                IsActive = true;
                Debug.Log("[Boss] " + bossName + " se activó.");
                OnBossActivated?.Invoke(this);
                StartCoroutine(Pattern());
            }
        }

        void FixedUpdate()
        {
            if (health.IsDead) return;
            rb.velocity = new Vector2(desiredVX, rb.velocity.y);
        }

        IEnumerator Pattern()
        {
            while (!health.IsDead)
            {
                // 1) Caminar hacia Kael
                float end = Time.time + walkTime;
                while (Time.time < end)
                {
                    SetFacing(DirectionToPlayer);
                    desiredVX = WallAhead() ? 0f : facing * walkSpeed * speedMultiplier;
                    yield return null;
                }
                desiredVX = 0f;

                // 2) Golpe al suelo
                Tint(slamColor);
                yield return new WaitForSeconds(slamTelegraph / speedMultiplier);
                Slam();
                Tint(normalColor);
                yield return new WaitForSeconds(0.8f);

                // 3) Embestida
                SetFacing(DirectionToPlayer);
                Tint(chargeColor);
                yield return new WaitForSeconds(chargeTelegraph / speedMultiplier);
                end = Time.time + maxChargeTime;
                while (Time.time < end && !WallAhead())
                {
                    desiredVX = facing * chargeSpeed * speedMultiplier;
                    yield return null;
                }
                desiredVX = 0f;
                if (cameraFollow != null) cameraFollow.Shake(0.2f, 0.15f);

                // 4) Aturdido: momento ideal para atacar
                Tint(stunColor);
                yield return new WaitForSeconds(stunTime);
                Tint(normalColor);
            }
        }

        void Slam()
        {
            AudioManager.Play(Sfx.BossSlam);
            if (cameraFollow != null) cameraFollow.Shake(0.25f, 0.25f);
            if (shockwavePrefab == null) return;

            Bounds b = col.bounds;
            for (int dir = -1; dir <= 1; dir += 2)
            {
                Vector3 pos = new Vector3(b.center.x + dir * (b.extents.x + 0.3f), b.min.y + 0.3f, 0f);
                Projectile wave = Instantiate(shockwavePrefab, pos, Quaternion.identity);
                wave.Launch(Team.Enemy, new Vector2(dir, 0f), shockwaveSpeed, 1, slamColor);
            }
        }

        void Tint(Color c)
        {
            if (sr != null) health.SetBaseColor(sr, c);
        }

        void HandleDeath()
        {
            StopAllCoroutines();
            desiredVX = 0f;
            Debug.Log("[Boss] " + bossName + " derrotado.");

            DamageOnContact dmg = GetComponent<DamageOnContact>();
            if (dmg != null) dmg.enabled = false;

            if (dropOnDeath != null)
                Instantiate(dropOnDeath, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            foreach (GameObject door in doorsToOpen)
                if (door != null) Destroy(door);

            if (cameraFollow != null) cameraFollow.Shake(0.4f, 0.3f);
            OnBossDefeated?.Invoke(this);
            Destroy(gameObject, 0.05f);
        }
    }
}
