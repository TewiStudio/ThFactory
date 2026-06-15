using UnityEngine;
using FishNet.Object;
using FishNet.Component.Transforming;

namespace Tewi.Game.Network
{
    [RequireComponent(typeof(Rigidbody), typeof(NetworkTransform))]
    public class ServerRigidbody : NetworkBehaviour
    {
        public new Rigidbody rigidbody;
        public RigidbodyInterpolation rigidbodyInterpolationOnServer = RigidbodyInterpolation.Interpolate;
        public RigidbodyInterpolation rigidbodyInterpolationOnClient = RigidbodyInterpolation.None; // only client

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            if (IsServerStarted)
            {
                rigidbody.isKinematic = false;
            }
            else
            {
                rigidbody.isKinematic = true;
            }
            TimeManager.OnTick -= TimeManager_OnTick;
            TimeManager.OnTick += TimeManager_OnTick;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            TimeManager.OnTick -= TimeManager_OnTick;
        }

        private void TimeManager_OnTick()
        {
            if (IsServerStarted)
            {
                if (rigidbody.interpolation != rigidbodyInterpolationOnServer)
                {
                    rigidbody.interpolation = rigidbodyInterpolationOnServer;
                }
            }
            else
            {
                if (rigidbody.interpolation != rigidbodyInterpolationOnClient)
                {
                    rigidbody.interpolation = rigidbodyInterpolationOnClient;
                }
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if (rigidbody == null) rigidbody = GetComponent<Rigidbody>();
        }

        [Server]
        public void AddForce(Vector3 force, Vector3 point, ForceMode mode = ForceMode.VelocityChange)
        {
            rigidbody.AddForceAtPosition(force, point, mode);
        }
    }
}
