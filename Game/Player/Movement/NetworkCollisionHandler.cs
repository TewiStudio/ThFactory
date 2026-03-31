using ECM2;
using UnityEngine;
using FishNet.Object;
using Tewi.Game.Network;

namespace Tewi.Game.Player.Movement
{
    namespace Tewi.Game.Player
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
                if (_characterMovement != null)
                    _characterMovement.collisionResponseCallback += OnCustomCollisionResponse;
            }

            public override void OnStopNetwork()
            {
                base.OnStopNetwork();
                if (_characterMovement != null)
                    _characterMovement.collisionResponseCallback -= OnCustomCollisionResponse;
            }

            private void OnCustomCollisionResponse(ref CollisionResult result, ref Vector3 characterImpulse, ref Vector3 otherImpulse)
            {
                if (result.rigidbody == null) return;

                if (result.rigidbody.TryGetComponent<ServerRigidbody>(out ServerRigidbody targetServerBody))
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
                        if (finalForce.sqrMagnitude > 0.01f) // 避免发送微小的误差力
                        {
                            RequestPushObject(targetServerBody, result.point, finalForce);
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
}
