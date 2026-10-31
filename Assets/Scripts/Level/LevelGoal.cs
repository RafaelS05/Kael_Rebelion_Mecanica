using UnityEngine;

namespace Kael
{
    /// <summary>Meta del nivel: al tocarla, Kael gana.</summary>
    public class LevelGoal : MonoBehaviour
    {
        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            if (GameManager.Instance != null) GameManager.Instance.Win();
        }
    }
}
