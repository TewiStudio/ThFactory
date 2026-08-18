using UnityEngine;
using FishNet.Object;
using FishNet.Component.Prediction;
using Tewi.Game.Player;

namespace Tewi.Game.Network
{
    public class ParentTrigger : NetworkBehaviour
    {
        public Rigidbody _rigidbody;
        public Collider _collider;
        public bool triggerEnter = false;
        public bool triggerExit = true;

        protected override void OnValidate()
        {
            base.OnValidate();
            if (!_collider) _collider = GetComponent<Collider>();
            if (!_rigidbody) _rigidbody = GetComponentInParent<Rigidbody>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!triggerEnter) return;
            if (other.transform.GetComponent<PlayerManager>() is PlayerManager player)
            {
                if (!player.IsOwner && (player.NetworkObject == null || !player.NetworkObject.IsSpawned)) return;
                player.groundDetect.OnGroundChanged(_rigidbody);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!triggerExit) return;
            if (other.transform.GetComponent<PlayerManager>() is PlayerManager player)
            {
                if (!player.IsOwner && player.characterMovement._parentPlatform != _rigidbody && (player.NetworkObject == null || !player.NetworkObject.IsSpawned)) return;
                player.groundDetect.OnGroundChanged(null);
            }
        }
    }
}