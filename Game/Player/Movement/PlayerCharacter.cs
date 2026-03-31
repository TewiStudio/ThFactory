using ECM2;

namespace Tewi.Game.Player.Movement
{
    public class PlayerCharacter : Character
    {
        public PlayerManager playerManager;
        private float speedModifier = 1f;
        public override float GetMaxSpeed()
        {
            float maxSpeed = base.GetMaxSpeed();
            
            return maxSpeed * speedModifier;
            /*
            float maxSpeed = base.GetMaxSpeed();

            float slopeAngle = GetSignedSlopeAngle();
            float speedModifier = slopeAngle > 0.0f
                ? 1.0f - Mathf.InverseLerp(0.0f, 90.0f, +slopeAngle)    // Decrease speed when moving up-slope
                : 1.0f + Mathf.InverseLerp(0.0f, 90.0f, -slopeAngle);   // Increase speed when moving down-slope

            return maxSpeed * speedModifier;*/
        }

        public void AddSpeedModifier(float add)
        {
            speedModifier += add;
        }

        public void SubstantSpeedModifier(float sub)
        {
            speedModifier -= sub;
        }
    }
}