using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kael
{
    public static class SceneNames
    {
        public const string Menu = "MenuPrincipal";
        public const string Level1 = "Nivel1";
        public const string Victory = "Victoria";
        public const string Defeat = "Derrota";
    }

    /// <summary>
    /// Controla el core loop del nivel: perder cuando Kael muere, ganar al llegar a la meta.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Tooltip("Segundos de espera antes de mostrar la pantalla de gane/pérdida.")]
        public float endDelay = 1.2f;

        public bool IsGameOver { get; private set; }

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            ScoreManager.Reset();

            // Los enemigos no chocan entre sí (evita que se amontonen o se empujen).
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0) Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
        }

        void Start()
        {
            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                Health h = player.GetComponent<Health>();
                if (h != null) h.OnDeath += Lose;
            }
            else
            {
                Debug.LogWarning("[GameManager] No se encontró a Kael (PlayerController) en la escena.");
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Win()
        {
            if (IsGameOver) return;
            IsGameOver = true;
            Debug.Log("[GameManager] ¡Nivel completado! Puntaje: " + ScoreManager.Score);
            StartCoroutine(LoadAfterDelay(SceneNames.Victory));
        }

        public void Lose()
        {
            if (IsGameOver) return;
            IsGameOver = true;
            Debug.Log("[GameManager] Kael fue derrotado.");
            StartCoroutine(LoadAfterDelay(SceneNames.Defeat));
        }

        IEnumerator LoadAfterDelay(string scene)
        {
            yield return new WaitForSecondsRealtime(endDelay);
            Time.timeScale = 1f;
            SceneManager.LoadScene(scene);
        }
    }
}
