using PrimeTween;
using Tewi.Game.Player;
using UnityEngine;

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
            Ray ray = new(player.playerCamera.camera.transform.position,
                          player.playerCamera.camera.transform.forward);

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
                    Vector3 forceDir = player.playerCamera.camera.transform.forward;
                    rb.AddForceAtPosition(forceDir * hitForce,
                                          hit.point,
                                          ForceMode.Impulse);

                    //Debug.Log($"Crowbar attack: {hit.point}");
                })
                .Chain(Tween.LocalRotation(
                    pivot,
                    Vector3.zero,
                    0.1f));
        }
    }
}