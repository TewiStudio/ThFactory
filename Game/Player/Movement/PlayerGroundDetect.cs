using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using ECM2;
using Tewi.Helpers;
using Tewi.Game.Network;

namespace Tewi.Game.Player.Movement
{
    public class PlayerGroundDetect : NetworkBehaviour
    {
        public PlayerManager playerManager;

        [Tooltip("平台最大安全角度。若平台倾斜超过此角度（如翻船），玩家将自动脱离平台")]
        public float maxSafeAngle = 55f;

        private Rigidbody _cacheGroundRigidbody;
        [ReadOnly] public Rigidbody _lastGroundRigidbody;
        [ReadOnly] public Rigidbody currentGroundRigidbody;

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

        private void CurrentParent_OnChange(ParentTrigger prev, ParentTrigger next, bool asServer)
        {
            if (next == null)
                currentGroundRigidbody = null;
            else
                currentGroundRigidbody = next.NetworkObject.GetComponent<Rigidbody>();
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
                return;
            }

            var parentTrigger = rigidbody.GetComponentInChildren<ParentTrigger>();
            if (parentTrigger is null)
            {
                return;
            }

            playerManager.characterMovement.AttachTo(rigidbody);
            RequestSetParent(parentTrigger);
        }

        [ServerRpc(RunLocally = true, OrderType = DataOrderType.Last)]
        private void RequestSetParent(ParentTrigger parent)
        {
            Debug.Log($"Player {playerManager.OwnerId} attached to {parent}");
            ApplySetParent(parent);
            if (IsServerInitialized)
            {
                ObserversSetGround(parent);
            }
        }

        private void ApplySetParent(ParentTrigger parent)
        {
            if (parent == null)
            {
                playerManager.character.enablePhysicsInteraction = true;
                playerManager.character.impartPlatformMovement = true;
                playerManager.character.impartPlatformRotation = true;
                playerManager.character.impartPlatformVelocity = true;
                NetworkObject.UnsetParent();

                if (IsOwner)
                {
                    //if (!IsServerStarted) playerManager.characterRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                    playerManager.character.SetRotation(Quaternion.Euler(0f, playerManager.transform.eulerAngles.y, 0f));
                }
            }
            else
            {
                playerManager.character.enablePhysicsInteraction = true;
                playerManager.character.impartPlatformMovement = false;
                playerManager.character.impartPlatformRotation = false;
                playerManager.character.impartPlatformVelocity = false;

                if (IsOwner)
                {
                    Vector3 targetUp = parent.transform.up;
                    Vector3 currentForward = playerManager.character.transform.forward;
                    Vector3 targetForward = Vector3.ProjectOnPlane(currentForward, targetUp);

                    if (targetForward != Vector3.zero)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(targetForward, targetUp);
                        playerManager.character.SetRotation(targetRotation);
                    }
                }

                NetworkObject.SetParent(parent.NetworkObject);
            }
        }

        private void LateUpdate()
        {
            if (!_lastGroundRigidbody) return;

            Transform platform = _lastGroundRigidbody.transform;

            // 计算平台当前的倾斜角度
            Vector3 platformUp = platform.up;
            float tiltAngle = Vector3.Angle(Vector3.up, platformUp);

            // 如果倾斜角超过最大安全阈值，强制解除父子关系并脱离平台
            if (tiltAngle > maxSafeAngle)
            {
                OnGroundChanged(null);
            }
        }

        [ObserversRpc(ExcludeOwner = true)]
        private void ObserversSetGround(ParentTrigger parentTrigger)
        {
            currentGroundRigidbody = parentTrigger == null ? null : parentTrigger.NetworkObject.GetComponentInParent<Rigidbody>();
        }
    }
}