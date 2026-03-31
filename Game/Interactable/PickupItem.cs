using FishNet.Object.Synchronizing;
using NaughtyAttributes;
using System.Collections.Generic;
using Tewi.Game.Network;
using Tewi.Game.Player;
using UnityEngine;

namespace Tewi.Game.Interactable
{
    [RequireComponent(typeof(ColliderGroupController), typeof(ServerRigidbody))]
    public class PickupItem : InteractableItem
    {
        [Space(15)]
        public ServerRigidbody serverRigidbody;
        public ColliderGroupController colliders;
        public FixedOnGroundPickupItemHitBox fixedOnGroundRigidbody;

        [Space(15)]
        [Header("Inventory Settings")]
        public Vector3 pickupOffset = new(.3f, -.3f, .5f);
        public Vector3 pickupRotate = Vector3.zero;
        public byte inInventoryIndex = 0;

        public virtual List<KeyCode> InPlayerHandUseKeys { get; set; } = new() { KeyCode.G, KeyCode.A, KeyCode.Mouse0 };
        public virtual List<string> InPlayerHandUseButton { get; set; } = new() { };

        public readonly SyncVar<bool> isEquipped = new(false);

        public override void OnInteract(PlayerManager player)
        {
            player.playerInventory.RequestPickupItem(this);
        }

        public virtual void OnKeyDown(KeyCode key, PlayerManager player)
        {
            if (key == player.dropItemKey)
            {
                player.playerInventory.RequestDropDownItem(this);
            }
            if (key == KeyCode.Mouse0) WindUp(player);
        }

        public virtual void OnKeyUp(KeyCode key, PlayerManager player)
        {
            if (key == KeyCode.Mouse0) Attack(player);
        }
        
        public virtual void OnButtonDown(string button, PlayerManager player)
        {

        }
        
        public virtual void OnButtonUp(string button, PlayerManager player)
        {

        }

        public virtual void WindUp(PlayerManager player)
        {

        }

        public virtual void Attack(PlayerManager player)
        {

        }

        public virtual void OnPickUp(PlayerManager player)
        {
            interactable.Value = false;
            isEquipped.Value = true;
            if (fixedOnGroundRigidbody)
            {
                fixedOnGroundRigidbody.StopTestGround();
                fixedOnGroundRigidbody.enabled = false;
            }
        }

        public virtual void OnDrop(PlayerManager player)
        {
            interactable.Value = true;
            isEquipped.Value = false;

            serverRigidbody.rigidbody.MovePosition(player.body.transform.forward * 1.5f + player.transform.position);

            /*if (player.playerGroundParent) rigidbody.AddForce(player.characterMovement.velocity + player.playerGroundParent.groundParentRigidbody.linearVelocity, ForceMode.VelocityChange);
            else */
            serverRigidbody.rigidbody.AddForce(player.characterMovement.velocity + player.characterMovement.movingPlatform.platformVelocity, ForceMode.VelocityChange);
            serverRigidbody.rigidbody.PublishTransform();

            if (fixedOnGroundRigidbody)
            {
                fixedOnGroundRigidbody.enabled = true;
                fixedOnGroundRigidbody.StartTestGround();
            }
        }

        public void ValidateData()
        {
            if (!colliders) colliders = GetComponent<ColliderGroupController>();
            if (!serverRigidbody) serverRigidbody = GetComponent<ServerRigidbody>();
            if (!fixedOnGroundRigidbody)
            {
                foreach (Transform child in transform)
                {
                    var fixedOnGroundRigidbody = child.GetComponent<FixedOnGroundPickupItemHitBox>();
                    if (fixedOnGroundRigidbody != null)
                    {
                        this.fixedOnGroundRigidbody = fixedOnGroundRigidbody;
                        break;
                    }
                }
            }
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            isEquipped.OnChange += IsEquipped_OnChange;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            isEquipped.OnChange -= IsEquipped_OnChange;
        }

        private void IsEquipped_OnChange(bool prev, bool next, bool asServer)
        {
            colliders.ForEachCollider(c => c.enabled = !next);

            bool shouldBeKinematic = next || !IsServerStarted;
            serverRigidbody.rigidbody.isKinematic = shouldBeKinematic;

            if (asServer) serverRigidbody.rigidbodyInterpolationOnServer = next ? RigidbodyInterpolation.None : RigidbodyInterpolation.Interpolate;
        }

        void Start()
        {
            //if (isOtherPickupItemParent) colliders.ForEachCollider(c => c.excludeLayers = LayerMask.GetMask("Interactable", "HitBox"));
            //else
            colliders.ForEachCollider(c => c.excludeLayers =LayerMask.GetMask("Ignore Raycast", "Damageable", "Interactable", "HitBox"));
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            ValidateData();
        }

        private void FixedUpdate()
        {
            if (IsServerStarted)
            {
                if (transform.position.y < -1100 || transform.position.y > 1100) Despawn(this);
            }

            if (isEquipped.Value)
            {
                transform.SetLocalPositionAndRotation(
                    pickupOffset,
                    Quaternion.Euler(pickupRotate));
            }
        }

        [Button("Print Sync State")]
        private void PrintState()
        {
            Debug.Log($"IsServer:{IsServerStarted} | IsClient:{IsClientStarted} | Index:{inInventoryIndex}");
        }
    }
}