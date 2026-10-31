using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Kael
{
    /// <summary>Menú principal: Jugar, Controles, Salir.</summary>
    public class MainMenuController : MonoBehaviour
    {
        public Button playButton;
        public Button controlsButton;
        public Button quitButton;

        [Header("Panel de controles")]
        public GameObject controlsPanel;
        public Button closeControlsButton;

        void Awake()
        {
            Time.timeScale = 1f;
            if (playButton != null) playButton.onClick.AddListener(Play);
            if (controlsButton != null) controlsButton.onClick.AddListener(() => ShowControls(true));
            if (closeControlsButton != null) closeControlsButton.onClick.AddListener(() => ShowControls(false));
            if (quitButton != null) quitButton.onClick.AddListener(Quit);
            if (controlsPanel != null) controlsPanel.SetActive(false);
        }

        void Start()
        {
            if (playButton != null) playButton.Select();
        }

        void Update()
        {
            if (controlsPanel != null && controlsPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                ShowControls(false);
        }

        public void Play()
        {
            AudioManager.Play(Sfx.UIClick);
            SceneManager.LoadScene(SceneNames.Level1);
        }

        public void ShowControls(bool show)
        {
            AudioManager.Play(Sfx.UIClick);
            if (controlsPanel != null) controlsPanel.SetActive(show);
            if (show && closeControlsButton != null) closeControlsButton.Select();
            else if (!show && playButton != null) playButton.Select();
        }

        public void Quit()
        {
            AudioManager.Play(Sfx.UIClick);
            Debug.Log("[Menu] Salir del juego.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
