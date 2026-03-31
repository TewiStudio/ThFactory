using UnityEngine;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Component.Prediction;

namespace Tewi.Game.Player
{
    /// <summary>
    /// Layer is "Hitbox" then used to make the player and pickup items parented to moving ground objects.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class GroundParent : NetworkBehaviour
    {
        public List<PlayerManager> playersOnGround = new();
        public Rigidbody groundParentRigidbody;
        public NetworkTrigger networkTrigger;
        public bool triggerEnter = false;
        public bool triggerExit = true;
        /*
                public override void OnStartServer()
                {
                    base.OnStartServer();
                    networkTrigger.OnEnter += ServerNetworkTrigger_OnEnter;
                    networkTrigger.OnExit += ServerNetworkTrigger_OnExit;
                }

                public override void OnStopServer()
                {
                    base.OnStopServer();
                    networkTrigger.OnEnter -= ServerNetworkTrigger_OnEnter;
                    networkTrigger.OnExit -= ServerNetworkTrigger_OnExit;
                }

                private void ServerNetworkTrigger_OnEnter(Collider other)
                {
                    if (!other) return;
                    if (other.transform.GetComponent<PlayerManager>() is PlayerManager player)
                    {
                        player.NetworkObject.SetParent(this);
                    }
                }

                private void ServerNetworkTrigger_OnExit(Collider other)
                {
                    if (!other) return;
                    if (other.transform.GetComponent<PlayerManager>() is PlayerManager player)
                    {
                        if (player.NetworkObject == null || !player.NetworkObject.IsSpawned) return;

                        player.NetworkObject.UnsetParent();
                    }
                }
        */
        public override void OnStartClient()
        {
            base.OnStartClient();
            networkTrigger.OnEnter += ClientNetworkTrigger_OnEnter;
            networkTrigger.OnExit += ClientNetworkTrigger_OnExit;
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            networkTrigger.OnEnter -= ClientNetworkTrigger_OnEnter;
            networkTrigger.OnExit -= ClientNetworkTrigger_OnExit;
        }

        private void ClientNetworkTrigger_OnEnter(Collider other)
        {
            if (!other || !triggerEnter) return;
            if (other.transform.GetComponent<PlayerManager>() is PlayerManager player)
            {
                if (!player.IsOwner && (player.NetworkObject == null || !player.NetworkObject.IsSpawned)) return;

                //Debug.Log($"GroundParent detected {OwnerId} ground rigidbody changed to {groundParentRigidbody}.");
                player.groundDetect.OnGroundChanged(groundParentRigidbody);
                /*RequestPlayerGroundChanged(player, true);
                player.transform.SetParent(transform);
                player.characterMovement.AttachTo(groundParentRigidbody);
                player.character.impartPlatformMovement = false;
                player.character.impartPlatformRotation = false;
                player.character.impartPlatformVelocity = false;*/
            }
        }

        private void ClientNetworkTrigger_OnExit(Collider other)
        {
            if (!other || !triggerExit) return;
            if (other.transform.GetComponent<PlayerManager>() is PlayerManager player)
            {
                if (!player.IsOwner && player.characterMovement._parentPlatform != groundParentRigidbody && (player.NetworkObject == null || !player.NetworkObject.IsSpawned)) return;

                //Debug.Log($"GroundParent detected {OwnerId} ground rigidbody changed to null.");
                player.groundDetect.OnGroundChanged(null);
                /*RequestPlayerGroundChanged(player, false);
                player.transform.SetParent(null);
                player.characterMovement.AttachTo(null);
                player.character.impartPlatformMovement = true;
                player.character.impartPlatformRotation = true;
                player.character.impartPlatformVelocity = true;*/
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestPlayerGroundChanged(PlayerManager playerManager, bool onGround)
        {
            if (onGround)
                playerManager.NetworkObject.SetParent(this);
            else
                playerManager.NetworkObject.UnsetParent();
            ObserverPlayerGroundChanged(playerManager, onGround);
        }

        [ObserversRpc]
        private void ObserverPlayerGroundChanged(PlayerManager playerManager, bool onGround)
        {
            if (playerManager.IsOwner) return;
            if (onGround)
            {
                playerManager.transform.SetParent(transform);
            }
            else
            {
                playerManager.transform.SetParent(null);
            }
        }
    }
}