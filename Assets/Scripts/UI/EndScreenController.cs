using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Kael
{
    /// <summary>Pantallas de Gane y Pérdida: muestran el puntaje y permiten reiniciar o volver al menú.</summary>
    public class EndScreenController : MonoBehaviour
    {
        public Text scoreText;
        public Button retryButton;
        public Button menuButton;

        void Awake()
        {
            Time.timeScale = 1f;
            if (retryButton != null) retryButton.onClick.AddListener(Retry);
            if (menuButton != null) menuButton.onClick.AddListener(GoToMenu);
        }

        void Start()
        {
            if (scoreText != null) scoreText.text = "PUNTAJE: " + ScoreManager.Score.ToString("00000");
            if (retryButton != null) retryButton.Select();
        }

        public void Retry()
        {
            AudioManager.Play(Sfx.UIClick);
            SceneManager.LoadScene(SceneNames.Level1);
        }

        public void GoToMenu()
        {
            AudioManager.Play(Sfx.UIClick);
            SceneManager.LoadScene(SceneNames.Menu);
        }
    }
}
