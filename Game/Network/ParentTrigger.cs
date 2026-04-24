using UnityEngine;
using FishNet.Object;
using FishNet.Component.Prediction;
using Tewi.Game.Player;

namespace Tewi.Game.Network
{
    /// <summary>
    /// Provides networked trigger handling for a parent object, enabling player ground detection updates when entering
    /// or exiting the associated trigger area in a multiplayer environment.
    /// </summary>
    /// <remarks>Requires a NetworkTrigger component on the same GameObject. This component is typically used
    /// to synchronize trigger events with player state in networked gameplay scenarios. The triggerEnter and
    /// triggerExit fields control whether enter and exit events are processed on the client.</remarks>
    [RequireComponent(typeof(NetworkTrigger))]
    public class ParentTrigger : NetworkBehaviour
    {
        public Rigidbody _rigidbody;
        public NetworkTrigger networkTrigger;
        public bool triggerEnter = false;
        public bool triggerExit = true;

        protected override void OnValidate()
        {
            base.OnValidate();
            if (!networkTrigger) networkTrigger = GetComponent<NetworkTrigger>();
            if (!_rigidbody) _rigidbody = GetComponentInParent<Rigidbody>();
        }

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
                player.groundDetect.OnGroundChanged(_rigidbody);
            }
        }

        private void ClientNetworkTrigger_OnExit(Collider other)
        {
            if (!other || !triggerExit) return;
            if (other.transform.GetComponent<PlayerManager>() is PlayerManager player)
            {
                if (!player.IsOwner && player.characterMovement._parentPlatform != _rigidbody && (player.NetworkObject == null || !player.NetworkObject.IsSpawned)) return;
                player.groundDetect.OnGroundChanged(null);
            }
        }
    }
}