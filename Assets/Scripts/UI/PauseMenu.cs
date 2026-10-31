using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Kael
{
    /// <summary>Menú de pausa (Esc o P): Continuar, Reiniciar nivel, Menú principal.</summary>
    public class PauseMenu : MonoBehaviour
    {
        public GameObject panel;
        public Button resumeButton;
        public Button restartButton;
        public Button menuButton;

        public bool IsPaused { get; private set; }

        void Awake()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (restartButton != null) restartButton.onClick.AddListener(Restart);
            if (menuButton != null) menuButton.onClick.AddListener(GoToMenu);
            if (panel != null) panel.SetActive(false);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
                if (IsPaused) Resume(); else Pause();
            }
        }

        public void Pause()
        {
            IsPaused = true;
            Time.timeScale = 0f;
            if (panel != null) panel.SetActive(true);
            if (resumeButton != null) resumeButton.Select();
        }

        public void Resume()
        {
            AudioManager.Play(Sfx.UIClick);
            IsPaused = false;
            Time.timeScale = 1f;
            if (panel != null) panel.SetActive(false);
        }

        public void Restart()
        {
            AudioManager.Play(Sfx.UIClick);
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void GoToMenu()
        {
            AudioManager.Play(Sfx.UIClick);
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneNames.Menu);
        }
    }
}
