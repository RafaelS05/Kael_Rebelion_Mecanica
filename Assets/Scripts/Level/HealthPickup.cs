using UnityEngine;

namespace Kael
{
    public class HealthPickup : PickupBase
    {
        public int amount = 1;

        protected override bool Collect(PlayerController player)
        {
            Health h = player.GetComponent<Health>();
            if (h == null || h.Current >= h.maxHealth) return false; // no se gasta si la vida está llena
            h.Heal(amount);
            return true;
        }
    }
}
