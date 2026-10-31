using UnityEngine;

namespace Kael
{
    /// <summary>
    /// Voltio: espera quieto; cuando Kael está a su altura y cerca, se carga (parpadea)
    /// y corre en línea recta hacia él. Se detiene al topar pared/borde y descansa.
    /// </summary>
    public class EnemyCharger : EnemyBase
    {
        enum State { Idle, Windup, Charge, Cooldown }

        public float detectRange = 7f;
        public float verticalTolerance = 1.2f;
        public float windupTime = 0.45f;
        public float chargeSpeed = 9f;
        public float maxChargeTime = 1.6f;
        public float cooldownTime = 1.2f;
        public Color windupColor = new Color(1f, 0.45f, 0.45f);

        State state = State.Idle;
        float stateEndTime;
        Color normalColor = Color.white;

        protected override void Start()
        {
            base.Start();
            if (sr != null) normalColor = sr.color;
        }

        void FixedUpdate()
        {
            if (health.IsDead) return;

            switch (state)
            {
                case State.Idle:
                    rb.velocity = new Vector2(0f, rb.velocity.y);
                    if (HorizontalDistanceToPlayer < detectRange && VerticalDistanceToPlayer < verticalTolerance)
                    {
                        SetFacing(DirectionToPlayer);
                        Enter(State.Windup, windupTime);
                        health.SetBaseColor(sr, windupColor);
                    }
                    break;

                case State.Windup:
                    rb.velocity = new Vector2(0f, rb.velocity.y);
                    if (Time.time >= stateEndTime)
                    {
                        Enter(State.Charge, maxChargeTime);
                        health.SetBaseColor(sr, normalColor);
                    }
                    break;

                case State.Charge:
                    rb.velocity = new Vector2(facing * chargeSpeed, rb.velocity.y);
                    if (Time.time >= stateEndTime || WallAhead() || !GroundAhead())
                    {
                        rb.velocity = new Vector2(0f, rb.velocity.y);
                        Enter(State.Cooldown, cooldownTime);
                    }
                    break;

                case State.Cooldown:
                    rb.velocity = new Vector2(0f, rb.velocity.y);
                    if (Time.time >= stateEndTime) Enter(State.Idle, 0f);
                    break;
            }
        }

        void Enter(State s, float duration)
        {
            state = s;
            stateEndTime = Time.time + duration;
        }
    }
}
