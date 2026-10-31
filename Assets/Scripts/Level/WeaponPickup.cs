using UnityEngine;

namespace Kael
{
    /// <summary>El arma mejorada que deja Oxibot Gigante al ser derrotado.</summary>
    public class WeaponPickup : PickupBase
    {
        protected override bool Collect(PlayerController player)
        {
            PlayerShooter shooter = player.GetComponent<PlayerShooter>();
            if (shooter == null) return false;
            shooter.UpgradeWeapon();
            ScoreManager.Add(250);
            return true;
        }
    }
}
