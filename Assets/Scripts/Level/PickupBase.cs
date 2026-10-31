using UnityEngine;

namespace Kael
{
    /// <summary>Movimiento flotante para que los objetos recogibles llamen la atención.</summary>
    public abstract class PickupBase : MonoBehaviour
    {
        public float bobAmplitude = 0.15f;
        public float bobSpeed = 3f;
        Vector3 startPos;

        protected virtual void Start() { startPos = transform.position; }

        void Update()
        {
            transform.position = startPos + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;
            if (Collect(player))
            {
                AudioManager.Play(Sfx.Pickup);
                Destroy(gameObject);
            }
        }

        /// <returns>true si se consumió el objeto.</returns>
        protected abstract bool Collect(PlayerController player);
    }
}
