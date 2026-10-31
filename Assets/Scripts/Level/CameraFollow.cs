using System.Collections;
using UnityEngine;

namespace Kael
{
    /// <summary>Cámara que sigue a Kael con suavizado, mira hacia adelante y no se sale del nivel.</summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector2 offset = new Vector2(0f, 1.5f);
        public float lookAhead = 2f;
        public float smoothTime = 0.15f;

        [Header("Límites del nivel (mundo)")]
        public bool useBounds = true;
        public Vector2 minBounds = new Vector2(0f, 0f);
        public Vector2 maxBounds = new Vector2(150f, 16f);

        Camera cam;
        Vector3 velocity;
        Vector3 shakeOffset;
        PlayerController player;

        void Awake()
        {
            cam = GetComponent<Camera>();
        }

        void Start()
        {
            if (target == null)
            {
                PlayerController p = FindObjectOfType<PlayerController>();
                if (p != null) target = p.transform;
            }
            if (target != null)
            {
                player = target.GetComponent<PlayerController>();
                transform.position = Clamp(DesiredPosition());
            }
        }

        void LateUpdate()
        {
            if (target == null) return;
            Vector3 pos = Vector3.SmoothDamp(transform.position - shakeOffset, Clamp(DesiredPosition()), ref velocity, smoothTime);
            transform.position = pos + shakeOffset;
        }

        Vector3 DesiredPosition()
        {
            float ahead = player != null ? player.Facing * lookAhead : 0f;
            return new Vector3(target.position.x + offset.x + ahead, target.position.y + offset.y, transform.position.z);
        }

        Vector3 Clamp(Vector3 p)
        {
            if (!useBounds || cam == null) return p;
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            p.x = (maxBounds.x - minBounds.x) > halfW * 2f ? Mathf.Clamp(p.x, minBounds.x + halfW, maxBounds.x - halfW) : (minBounds.x + maxBounds.x) * 0.5f;
            p.y = (maxBounds.y - minBounds.y) > halfH * 2f ? Mathf.Clamp(p.y, minBounds.y + halfH, maxBounds.y - halfH) : (minBounds.y + maxBounds.y) * 0.5f;
            return p;
        }

        public void Shake(float duration, float magnitude)
        {
            StopAllCoroutines();
            StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float end = Time.time + duration;
            while (Time.time < end)
            {
                shakeOffset = (Vector3)(Random.insideUnitCircle * magnitude);
                yield return null;
            }
            shakeOffset = Vector3.zero;
        }
    }
}
