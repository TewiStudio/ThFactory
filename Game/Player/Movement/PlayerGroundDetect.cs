using ECM2;
using FishNet.Object;
using FishNet.Transporting;
using Tewi.Game.Network;
using UnityEngine;

namespace Tewi.Game.Player.Movement
{
    public class PlayerGroundDetect : NetworkBehaviour
    {
        public PlayerManager playerManager;
        private Rigidbody _cacheGroundRigidbody;
        private Rigidbody _lastGroundRigidbody;

        public override void OnStartClient()
        {
            base.OnStartClient();
            TimeManager.OnTick += TimeManager_OnTick;
            playerManager.characterMovement.FoundGround += CharacterMovement_FoundGround;
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            TimeManager.OnTick -= TimeManager_OnTick;
            playerManager.characterMovement.FoundGround -= CharacterMovement_FoundGround;
        }

        private void TimeManager_OnTick()
        {
            GetGroundRigidbody();
        }

        private void GetGroundRigidbody()
        {
            if (playerManager.characterMovement.groundRigidbody is null) { _cacheGroundRigidbody = null; return; }
            if (_cacheGroundRigidbody == playerManager.characterMovement.groundRigidbody) return;
            _cacheGroundRigidbody = playerManager.characterMovement.groundRigidbody;

            //Debug.Log($"TimeTick detected {OwnerId} ground rigidbody changed to {_cacheGroundRigidbody}.");
            OnGroundChanged(_cacheGroundRigidbody);
        }

        private void CharacterMovement_FoundGround(ref FindGroundResult foundGround)
        {
            _cacheGroundRigidbody = foundGround.rigidbody;
            //Debug.Log($"FoundGround detected {OwnerId} ground rigidbody changed to {_cacheGroundRigidbody}.");
            OnGroundChanged(foundGround.rigidbody);
        }

        public void OnGroundChanged(Rigidbody rigidbody)
        {
            if (rigidbody == _lastGroundRigidbody) return;
            _lastGroundRigidbody = rigidbody;

            if (!rigidbody)
            {
                playerManager.characterMovement.AttachTo(null);
                RequestSetParent(null);
                //Debug.Log($"Player {OwnerId} ground rigidbody changed to null.");
                return;
            }

            if (rigidbody.GetComponent<ServerRigidbody>() is ServerRigidbody serverRigidbody)
            {
                return;
            }

            if (!rigidbody.isKinematic)
            {
                playerManager.characterMovement.AttachTo(null);
                RequestSetParent(null);
                //Debug.Log($"Player {OwnerId} ground rigidbody changed to null.");
                return;
            }

            //Debug.Log($"Player {OwnerId} ground rigidbody changed to {rigidbody}.");
            playerManager.characterMovement.AttachTo(rigidbody);
            RequestSetParent(rigidbody.GetComponent<NetworkObject>());
        }

        [ServerRpc(RunLocally = true, OrderType = DataOrderType.Last)]
        private void RequestSetParent(NetworkObject parent, Channel channel = Channel.Reliable)
        {
            ApplySetParent(parent);
            //if (IsServerInitialized) ObserverSetParent(parent);
        }

        [ObserversRpc]
        private void ObserverSetParent(NetworkObject parent, Channel channel = Channel.Reliable)
        {
            ApplySetParent(parent);
            /*if (IsOwner)
            {
                playerManager.networkTransform.SetSynchronizePosition(true);
                playerManager.networkTransform.SetSynchronizeRotation(true);
            }*/
        }

        private void ApplySetParent(NetworkObject parent)
        {
            if (parent == null)
            {
                playerManager.characterMovement.fastPlatformMove = false;
                playerManager.character.enablePhysicsInteraction = true;
                playerManager.character.impartPlatformMovement = true;
                playerManager.character.impartPlatformRotation = true;
                playerManager.character.impartPlatformVelocity = true;
                NetworkObject.UnsetParent();
            }
            else
            {
                playerManager.character.enablePhysicsInteraction = false;
                playerManager.character.impartPlatformMovement = false;
                playerManager.character.impartPlatformRotation = false;
                playerManager.character.impartPlatformVelocity = false;
                NetworkObject.SetParent(parent);
            }
        }
    }
}