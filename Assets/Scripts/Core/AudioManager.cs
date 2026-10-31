using UnityEngine;

namespace Kael
{
    public enum Sfx { Jump, Shoot, PlayerHurt, EnemyHit, EnemyDeath, Pickup, BossSlam, UIClick }

    /// <summary>
    /// Reproduce música y efectos. Si un clip está vacío simplemente no suena,
    /// así el juego funciona aunque todavía no se hayan agregado los audios CC0.
    /// Arrastre los clips en el Inspector del objeto "AudioManager" de cada escena.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Música (loop)")]
        public AudioClip music;
        [Range(0f, 1f)] public float musicVolume = 0.5f;

        [Header("Efectos de sonido")]
        public AudioClip jump;
        public AudioClip shoot;
        public AudioClip playerHurt;
        public AudioClip enemyHit;
        public AudioClip enemyDeath;
        public AudioClip pickup;
        public AudioClip bossSlam;
        public AudioClip uiClick;
        [Range(0f, 1f)] public float sfxVolume = 0.8f;

        AudioSource musicSource;
        AudioSource sfxSource;

        void Awake()
        {
            Instance = this;
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }

        void Start()
        {
            if (music != null)
            {
                musicSource.clip = music;
                musicSource.volume = musicVolume;
                musicSource.Play();
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void Play(Sfx sfx)
        {
            if (Instance == null) return;
            AudioClip clip = Instance.ClipFor(sfx);
            if (clip != null) Instance.sfxSource.PlayOneShot(clip, Instance.sfxVolume);
        }

        AudioClip ClipFor(Sfx sfx)
        {
            switch (sfx)
            {
                case Sfx.Jump: return jump;
                case Sfx.Shoot: return shoot;
                case Sfx.PlayerHurt: return playerHurt;
                case Sfx.EnemyHit: return enemyHit;
                case Sfx.EnemyDeath: return enemyDeath;
                case Sfx.Pickup: return pickup;
                case Sfx.BossSlam: return bossSlam;
                case Sfx.UIClick: return uiClick;
            }
            return null;
        }
    }
}
