using UnityEngine;
using Tewi.Game.Player;

namespace Tewi.Game.Interactable.Ladders
{
    public sealed class Ladder : InteractableItem
    {
        public override void OnInteract(PlayerManager player)
        {
            base.OnInteract(player);
            if (player.ladderClimbAbility.IsClimbing())
            {
                player.ladderClimbAbility.StopClimbing();
            }
            else
            {
                player.ladderClimbAbility.activeLadder = this;
                player.ladderClimbAbility.Climb();
            }
        }

        #region EDITOR EXPOSED FIELDS

        [Header("Ladder Path")]
        public float PathLength = 10.0f;
        public Vector3 PathOffset = new Vector3(0f, 0f, -0.5f);

        [Header("Anchor Points")]
        public Transform TopPoint;
        public Transform BottomPoint;

        #endregion

        #region PROPERTIES

        public Vector3 bottomAnchorPoint => transform.position + transform.TransformVector(PathOffset);

        public Vector3 topAnchorPoint => bottomAnchorPoint + transform.up * PathLength;

        #endregion

        #region METHODS

        public Vector3 ClosestPointOnPath(Vector3 position, out float pathPosition)
        {
            Vector3 path = topAnchorPoint - bottomAnchorPoint;
            Vector3 pathToPoint = position - bottomAnchorPoint;
            
            float height = Vector3.Dot(pathToPoint, path.normalized);

            if (height > 0.0f)
            {
                // If we are below top point
                
                if (height <= path.magnitude)
                {
                    pathPosition = 0;
                    return bottomAnchorPoint + path.normalized * height;
                }

                // If we are higher than top point

                pathPosition = height - path.magnitude;
                return topAnchorPoint;
            }

            // Below bottom point

            pathPosition = height;
            return bottomAnchorPoint;
        }

        #endregion

        #region MONOBEHAVIOUR

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(bottomAnchorPoint, topAnchorPoint);

            if (BottomPoint == null || TopPoint == null)
                return;

            Gizmos.DrawWireCube(BottomPoint.position, Vector3.one * 0.1f);
            Gizmos.DrawWireCube(TopPoint.position, Vector3.one * 0.1f);
        }

        #endregion
    }
}