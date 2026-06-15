using UnityEngine;
using PrimeTween;
using Tewi.Game.Player;
using Tewi.Game.Network;
using FishNet.Object;

namespace Tewi.Game.Interactable.PickupItems
{
    public class Crowbar : PickupItem
    {
        public float attackRange = 2.5f;
        public float hitForce = 8f;
        public Transform pivot;

        public override void WindUp(PlayerManager player)
        {
            Tween.LocalRotation(
                pivot,
                new Vector3(-30f, 0f, 0f),
                0.1f);
        }

        public override void Attack(PlayerManager player)
        {
            Ray ray = new(player.cameraManager.playerCamera.transform.position,
                          player.cameraManager.playerCamera.transform.forward);

            var isHit = Physics.Raycast(ray, out RaycastHit hit, attackRange);
            Sequence.Create()
                .Chain(Tween.LocalRotation(
                    pivot,
                    new Vector3(50f, 0f, 0f),
                    0.1f, Ease.InCubic))
                .ChainCallback(() =>
                {
                    if (!isHit) return;
                    Rigidbody rb = hit.collider.attachedRigidbody;

                    if (!rb) return;
                    if (rb.transform.GetComponent<ServerRigidbody>() is not ServerRigidbody serverRigidbody) return;
                    Vector3 forceDir = player.cameraManager.playerCamera.transform.forward;
                    RequestAddForce(serverRigidbody, forceDir, hit.point);
                })
                .Chain(Tween.LocalRotation(
                    pivot,
                    Vector3.zero,
                    0.1f));
        }

        [ServerRpc]
        private void RequestAddForce(ServerRigidbody serverRigidbody, Vector3 forceDir, Vector3 hitPoint)
        {
            serverRigidbody.AddForce(forceDir * hitForce, hitPoint, ForceMode.Impulse);
        }
    }
}