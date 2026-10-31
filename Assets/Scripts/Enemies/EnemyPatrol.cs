using UnityEngine;

namespace Kael
{
    /// <summary>
    /// Chatarra: camina de un lado a otro por la plataforma y da vuelta al topar
    /// con una pared o con el borde. Hace daño por contacto (DamageOnContact).
    /// </summary>
    public class EnemyPatrol : EnemyBase
    {
        public float speed = 2f;

        void FixedUpdate()
        {
            if (health.IsDead) return;
            if (WallAhead() || !GroundAhead()) SetFacing(-facing);
            rb.velocity = new Vector2(facing * speed, rb.velocity.y);
        }
    }
}
