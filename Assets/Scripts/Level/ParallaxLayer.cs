using UnityEngine;

namespace Kael
{
    /// <summary>
    /// Capa de fondo con efecto parallax: se mueve una fracción del movimiento de la cámara.
    /// factor = 0 → fija en el mundo (como el escenario); factor = 1 → pegada a la cámara (como el cielo).
    /// Corre después de CameraFollow para que no tiemble.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class ParallaxLayer : MonoBehaviour
    {
        [Range(0f, 1f)] public float factorX = 0.5f;
        [Range(0f, 1f)] public float factorY = 0.5f;
        [Tooltip("Posición de la cámara para la que se diseñó la capa (vista inicial del nivel).")]
        public Vector2 referenceCameraPosition = new Vector2(12.5f, 7f);

        Transform cam;
        Vector3 basePosition;

        void Start()
        {
            if (Camera.main != null) cam = Camera.main.transform;
            basePosition = transform.position;
        }

        void LateUpdate()
        {
            if (cam == null) return;
            Vector2 delta = (Vector2)cam.position - referenceCameraPosition;
            transform.position = new Vector3(
                basePosition.x + delta.x * factorX,
                basePosition.y + delta.y * factorY,
                basePosition.z);
        }
    }
}
