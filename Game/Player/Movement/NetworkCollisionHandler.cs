using UnityEngine;
using ECM2;
using FishNet.Object;
using Tewi.Game.Network;

namespace Tewi.Game.Player.Movement
{
    public class NetworkCollisionHandler : NetworkBehaviour
    {
        private CharacterMovement _characterMovement;

        private void Awake()
        {
            _characterMovement = GetComponent<CharacterMovement>();
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            if (_characterMovement != null && !IsHostStarted)
                _characterMovement.collisionResponseCallback += OnCustomCollisionResponse;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            if (_characterMovement != null)
                _characterMovement.collisionResponseCallback -= OnCustomCollisionResponse;
        }

        private Vector3 _lastPoint;
        private Vector3 _lastForce;
        private void OnCustomCollisionResponse(ref CollisionResult result, ref Vector3 characterImpulse, ref Vector3 otherImpulse)
        {
            if (result.rigidbody == null) return;

            if (result.rigidbody.TryGetComponent(out ServerRigidbody targetServerBody))
            {
                // 获取双方质量
                float myMass = _characterMovement.rigidbody.mass;
                float otherMass = result.rigidbody.mass;

                // 计算质量比
                float massRatio = myMass / (myMass + otherMass);

                // ECM2 的相对速度点乘法线公式
                float velocityDotNormal = Vector3.Dot(result.velocity, result.normal);
                float otherVelocityDotNormal = Vector3.Dot(result.otherVelocity, result.normal);

                Vector3 customOtherImpulse = Vector3.zero;

                if (otherVelocityDotNormal > velocityDotNormal)
                {
                    Vector3 relVel = (otherVelocityDotNormal - velocityDotNormal) * result.normal;
                    customOtherImpulse = -(relVel * massRatio);
                }

                Vector3 finalForce = customOtherImpulse * _characterMovement.pushForceScale;

                if (IsOwner)
                {
                    //Debug.Log($"计算出的推力为: {finalForce.magnitude}");
                    if (finalForce.sqrMagnitude > 0.01f && _lastForce != finalForce && _lastPoint != result.point)
                    {
                        RequestPushObject(targetServerBody, result.point, finalForce);
                        _lastForce = finalForce;
                        _lastPoint = result.point;
                        //Debug.Log($"计算出的推力为: {finalForce.magnitude} - {result.point}");
                    }
                }

                // 无论是客户端还是服务器，都拦截掉 ECM2/Unity 默认的物理力
                otherImpulse = Vector3.zero;
            }
        }

        [ServerRpc]
        private void RequestPushObject(ServerRigidbody target, Vector3 point, Vector3 force)
        {
            if (target != null)
            {
                target.AddForce(force, point, ForceMode.VelocityChange);

            }
        }
    }
}
