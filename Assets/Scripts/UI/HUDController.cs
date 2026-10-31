using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Kael
{
    /// <summary>
    /// HUD de la partida: vida de Kael, arma equipada, puntaje y barra del jefe (solo durante la pelea).
    /// Las barras funcionan cambiando el anchorMax.x del relleno (0 = vacía, 1 = llena).
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Vida de Kael")]
        public RectTransform healthFill;
        public Text healthText;
        public Image damageFlash;

        [Header("Arma")]
        public Image weaponIcon;
        public Text weaponText;

        [Header("Puntaje")]
        public Text scoreText;

        [Header("Jefe")]
        public GameObject bossPanel;
        public Text bossNameText;
        public RectTransform bossFill;

        Health playerHealth;
        PlayerShooter shooter;
        BossOxibot boss;
        Coroutine flashRoutine;

        void OnEnable()
        {
            ScoreManager.OnScoreChanged += UpdateScore;
            BossOxibot.OnBossActivated += ShowBoss;
            BossOxibot.OnBossDefeated += HideBoss;
        }

        void OnDisable()
        {
            ScoreManager.OnScoreChanged -= UpdateScore;
            BossOxibot.OnBossActivated -= ShowBoss;
            BossOxibot.OnBossDefeated -= HideBoss;
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= UpdatePlayerHealth;
                playerHealth.OnDamaged -= HandlePlayerDamaged;
            }
            if (shooter != null) shooter.OnWeaponChanged -= UpdateWeapon;
            if (boss != null) boss.Health.OnHealthChanged -= UpdateBossHealth;
        }

        void Start()
        {
            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                playerHealth = player.GetComponent<Health>();
                shooter = player.GetComponent<PlayerShooter>();
                playerHealth.OnHealthChanged += UpdatePlayerHealth;
                playerHealth.OnDamaged += HandlePlayerDamaged;
                UpdatePlayerHealth(playerHealth.Current, playerHealth.maxHealth);
                if (shooter != null)
                {
                    shooter.OnWeaponChanged += UpdateWeapon;
                    UpdateWeapon(shooter.Current);
                }
            }
            if (bossPanel != null) bossPanel.SetActive(false);
            if (damageFlash != null) damageFlash.color = new Color(1f, 0f, 0f, 0f);
            UpdateScore(ScoreManager.Score);
        }

        void UpdatePlayerHealth(int current, int max)
        {
            SetFill(healthFill, max > 0 ? (float)current / max : 0f);
            if (healthText != null) healthText.text = "VIDA " + current + "/" + max;
        }

        void HandlePlayerDamaged(int amount, Vector2 source)
        {
            if (damageFlash == null) return;
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine());
        }

        IEnumerator FlashRoutine()
        {
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.unscaledDeltaTime;
                damageFlash.color = new Color(1f, 0f, 0f, Mathf.Lerp(0.35f, 0f, t / 0.35f));
                yield return null;
            }
            damageFlash.color = new Color(1f, 0f, 0f, 0f);
        }

        void UpdateWeapon(WeaponData w)
        {
            if (weaponText != null) weaponText.text = w.displayName;
            if (weaponIcon != null) weaponIcon.color = w.color;
        }

        void UpdateScore(int score)
        {
            if (scoreText != null) scoreText.text = "PUNTOS " + score.ToString("00000");
        }

        void ShowBoss(BossOxibot b)
        {
            boss = b;
            if (bossPanel != null) bossPanel.SetActive(true);
            if (bossNameText != null) bossNameText.text = b.bossName.ToUpper();
            b.Health.OnHealthChanged += UpdateBossHealth;
            UpdateBossHealth(b.Health.Current, b.Health.maxHealth);
        }

        void HideBoss(BossOxibot b)
        {
            if (boss != null) boss.Health.OnHealthChanged -= UpdateBossHealth;
            boss = null;
            if (bossPanel != null) bossPanel.SetActive(false);
        }

        void UpdateBossHealth(int current, int max)
        {
            SetFill(bossFill, max > 0 ? (float)current / max : 0f);
        }

        static void SetFill(RectTransform fill, float ratio)
        {
            if (fill == null) return;
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
        }
    }
}
