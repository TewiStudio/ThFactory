using Animancer;
using FishNet.Component.Animating;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;

namespace Tewi.Game.Player.Body
{
    public class BodyManager : NetworkBehaviour
    {
        public PlayerManager playerManager;
        public readonly SyncVar<PlayerState> playerState = new();
        public SkinnedMeshRenderer[] bodySkinnedMeshRenderers;
        public MeshRenderer[] bodyMeshRenderers;

        [Space(15)]
        public Animator bodyAnimator;
        public NetworkAnimator networkAnimator;
        public AnimancerComponent bodyAnimancerComponent;

        [Space(15)]
        public AnimationClip idleAnimation;
        public AnimationClip walkForwardAnimation;
        public AnimationClip walkBackAnimation;
        public AnimationClip walkLeftAnimation;
        public AnimationClip walkRightAnimation;

        public override void OnStartClient()
        {
            base.OnStartNetwork();
            playerState.OnChange += PlayerState_OnChange;
            bodyAnimancerComponent.Play(idleAnimation);
            if (IsOwner)
            {
                foreach (var item in bodyMeshRenderers)
                {
                    item.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
                foreach (var item in bodySkinnedMeshRenderers)
                {
                    item.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
            }
            else
            {
                foreach (var item in bodyMeshRenderers)
                {
                    item.shadowCastingMode = ShadowCastingMode.On;
                }
                foreach (var item in bodySkinnedMeshRenderers)
                {
                    item.shadowCastingMode = ShadowCastingMode.On;
                }
            }
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            playerState.OnChange -= PlayerState_OnChange;
        }

        public void SetPlayerState(PlayerState state)
        {
            SetPlayerStateLocal(state);
            if (IsOwner)
                RequestSetPlayerState(state);
        }

        [ServerRpc]
        public void RequestSetPlayerState(PlayerState state)
        {
            playerState.Value = state;
        }

        private void PlayerState_OnChange(PlayerState prev, PlayerState next, bool asServer)
        {
            if (IsOwner) return;
            SetPlayerStateLocal(next);
        }

        private void SetPlayerStateLocal(PlayerState state)
        {
            if (state == PlayerState.Idle)
            {
                bodyAnimancerComponent.Play(idleAnimation, AnimancerGraph.DefaultFadeDuration);
            }
            else
            {
                bodyAnimancerComponent.Play(walkForwardAnimation, AnimancerGraph.DefaultFadeDuration);
            }
        }
    }
}