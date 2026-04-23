using FishNet.Component.Prediction;
using FishNet.Object;
using Tewi.Game.Network;
using Tewi.Game.Player;
using Tewi.Helpers;
using UnityEngine;

namespace Tewi.Game.Interactable
{
    [RequireComponent(typeof(Collider))]
    public class FixedOnGroundPickupItemHitBox : NetworkBehaviour
    {
        public Collider hitbox;
        public ServerRigidbody parentRigidbody;
        [ReadOnly] public Collider hitOther;
        //public PickupItem pickupItem;
        [SerializeField] private bool fixedOnGround = false;
        //[SerializeField] private bool isMovingGround = false;
        public bool testOnTheGround { get; private set; } = true;
        public Vector3 fixedOnGroundPosition = Vector3.zero;

        public void ValidateData()
        {
            if (!hitbox) hitbox = GetComponent<Collider>();
            if (!parentRigidbody && transform.parent) parentRigidbody = transform.parent.GetComponent<ServerRigidbody>();
        }

        protected override void Reset()
        {
            ValidateData();
        }

        protected override void OnValidate()
        {
            ValidateData();
        }

        void Start()
        {
            ValidateData();
            hitbox.excludeLayers = LayerMask.GetMask("Ignore Raycast", "Interactable", "Damageable", "HitBox");
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServerStarted) return;
            if (other.GetComponentInParent<NetworkMovingPlatform>() is NetworkMovingPlatform platform)
            {
                hitOther = other;

                if (testOnTheGround)
                {
                    testOnTheGround = false;
                    fixedOnGround = true;

                    parentRigidbody.rigidbody.isKinematic = true;
                    parentRigidbody.rigidbodyInterpolationOnServer = RigidbodyInterpolation.None;

                    ObserversSetParent(platform);

                    transform.parent.localRotation = new Quaternion(0, transform.parent.localRotation.y, 0, transform.parent.localRotation.w);
                }
            }
        }

        [Server]
        public void StartTestGround()
        {
            testOnTheGround = true;
            fixedOnGround = false;
            parentRigidbody.rigidbody.isKinematic = true;
            parentRigidbody.rigidbodyInterpolationOnServer = RigidbodyInterpolation.Interpolate;
        }

        public void StopTestGround()
        {
            testOnTheGround = false;
        }

        [ObserversRpc(RunLocally = true)]
        private void ObserversSetParent(NetworkObject parent)
        {
            if (parent == null)
                NetworkObject.UnsetParent();
            else
                NetworkObject.SetParent(parent);
        }
    }
}