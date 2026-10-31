using System;
using System.Collections;
using UnityEngine;

namespace Kael
{
    public enum Team { Player, Enemy }

    /// <summary>
    /// Sistema de vida compartido por Kael, enemigos y jefes.
    /// Todo el daño del juego pasa por TakeDamage().
    /// </summary>
    public class Health : MonoBehaviour
    {
        [Header("Vida")]
        public Team team = Team.Enemy;
        public int maxHealth = 3;
        [Tooltip("Segundos de invulnerabilidad después de recibir daño (Kael usa ~1s).")]
        public float invulnerableTime = 0f;

        [Header("Al morir")]
        public bool destroyOnDeath = true;
        [Tooltip("Puntos que suma al puntaje cuando muere (solo enemigos).")]
        public int scoreValue = 100;

        [Header("Feedback visual")]
        public Color hurtColor = new Color(1f, 0.35f, 0.35f);

        public int Current { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsInvulnerable => Time.time < invulnerableUntil;

        /// <summary>(vidaActual, vidaMaxima)</summary>
        public event Action<int, int> OnHealthChanged;
        /// <summary>(daño, posición de quien hizo el daño)</summary>
        public event Action<int, Vector2> OnDamaged;
        public event Action OnDeath;

        float invulnerableUntil;
        SpriteRenderer[] renderers;
        Color[] baseColors;
        Coroutine flashRoutine;

        void Awake()
        {
            Current = maxHealth;
            renderers = GetComponentsInChildren<SpriteRenderer>();
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;
        }

        void Start()
        {
            OnHealthChanged?.Invoke(Current, maxHealth);
        }

        public void TakeDamage(int amount, Vector2 sourcePosition)
        {
            if (IsDead || IsInvulnerable || amount <= 0) return;

            Current = Mathf.Max(0, Current - amount);
            invulnerableUntil = Time.time + invulnerableTime;

            OnHealthChanged?.Invoke(Current, maxHealth);
            OnDamaged?.Invoke(amount, sourcePosition);
            AudioManager.Play(team == Team.Player ? Sfx.PlayerHurt : Sfx.EnemyHit);

            if (Current <= 0) Die();
            else
            {
                if (flashRoutine != null) StopCoroutine(flashRoutine);
                flashRoutine = StartCoroutine(Flash());
            }
        }

        public void Heal(int amount)
        {
            if (IsDead) return;
            Current = Mathf.Min(maxHealth, Current + amount);
            OnHealthChanged?.Invoke(Current, maxHealth);
        }

        /// <summary>Muerte instantánea (caer al vacío).</summary>
        public void Kill()
        {
            if (IsDead) return;
            Current = 0;
            OnHealthChanged?.Invoke(Current, maxHealth);
            Die();
        }

        void Die()
        {
            IsDead = true;
            if (team == Team.Enemy)
            {
                ScoreManager.Add(scoreValue);
                AudioManager.Play(Sfx.EnemyDeath);
            }
            OnDeath?.Invoke();
            if (destroyOnDeath) Destroy(gameObject);
        }

        IEnumerator Flash()
        {
            // Kael parpadea durante la invulnerabilidad; los enemigos solo se tiñen un instante.
            float duration = Mathf.Max(0.12f, invulnerableTime);
            float end = Time.time + duration;
            bool on = true;
            while (Time.time < end)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] == null) continue;
                    Color c = on ? hurtColor : baseColors[i];
                    if (team == Team.Player && !on) c.a = 0.35f;
                    renderers[i].color = c;
                }
                on = !on;
                yield return new WaitForSeconds(0.08f);
            }
            RestoreColors();
            flashRoutine = null;
        }

        /// <summary>Permite que otros scripts cambien el color base (p.ej. el jefe al anticipar un ataque).</summary>
        public void SetBaseColor(SpriteRenderer sr, Color c)
        {
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] == sr) baseColors[i] = c;
            if (flashRoutine == null && sr != null) sr.color = c;
        }

        void RestoreColors()
        {
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].color = baseColors[i];
        }
    }
}
