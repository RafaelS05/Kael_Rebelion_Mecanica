using System;
using UnityEngine;

namespace Kael
{
    [Serializable]
    public class WeaponData
    {
        public string displayName = "Pistola de energía";
        public int damage = 1;
        public float projectileSpeed = 14f;
        public float cooldown = 0.25f;
        public Color color = Color.cyan;
        public float projectileScale = 1f;
    }

    /// <summary>
    /// Disparo de Kael. Teclas: J, K, Ctrl izquierdo o clic izquierdo.
    /// Arma 0 = pistola básica, arma 1 = arma mejorada que se obtiene al vencer a Oxibot Gigante.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerShooter : MonoBehaviour
    {
        public Projectile projectilePrefab;
        public Vector2 muzzleOffset = new Vector2(0.6f, 0.05f);

        public WeaponData[] weapons =
        {
            new WeaponData { displayName = "Pistola de energía", damage = 1, projectileSpeed = 14f, cooldown = 0.25f, color = new Color(0.3f, 0.95f, 1f), projectileScale = 1f },
            new WeaponData { displayName = "Cañón de plasma", damage = 3, projectileSpeed = 18f, cooldown = 0.18f, color = new Color(1f, 0.45f, 0.95f), projectileScale = 1.6f },
        };

        public int WeaponIndex { get; private set; }
        public WeaponData Current => weapons[Mathf.Clamp(WeaponIndex, 0, weapons.Length - 1)];

        /// <summary>Se dispara cuando cambia el arma (para el HUD).</summary>
        public event Action<WeaponData> OnWeaponChanged;

        PlayerController controller;
        float nextShotTime;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
        }

        void Start()
        {
            OnWeaponChanged?.Invoke(Current);
        }

        void Update()
        {
            if (Time.timeScale == 0f || !controller.InputEnabled) return;

            bool fire = Input.GetKey(KeyCode.J) || Input.GetKey(KeyCode.K) || Input.GetButton("Fire1");
            if (fire && Time.time >= nextShotTime) Shoot();
        }

        void Shoot()
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning("[PlayerShooter] Falta asignar el prefab del proyectil.");
                return;
            }

            WeaponData w = Current;
            nextShotTime = Time.time + w.cooldown;
            int dir = controller.Facing;
            Vector3 pos = transform.position + new Vector3(muzzleOffset.x * dir, muzzleOffset.y, 0f);

            Projectile p = Instantiate(projectilePrefab, pos, Quaternion.identity);
            p.transform.localScale = Vector3.one * w.projectileScale;
            p.Launch(Team.Player, new Vector2(dir, 0f), w.projectileSpeed, w.damage, w.color);
            AudioManager.Play(Sfx.Shoot);
        }

        public void UpgradeWeapon()
        {
            if (WeaponIndex >= weapons.Length - 1) return;
            WeaponIndex++;
            Debug.Log("[PlayerShooter] Nueva arma: " + Current.displayName);
            OnWeaponChanged?.Invoke(Current);
        }
    }
}
