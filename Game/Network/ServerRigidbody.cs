using UnityEngine;
using FishNet.Object;
using FishNet.Component.Transforming;

namespace Tewi.Game.Network
{
    [RequireComponent(typeof(Rigidbody), typeof(NetworkTransform), typeof(NetworkObject))]
    public class ServerRigidbody : NetworkBehaviour
    {
        public new Rigidbody rigidbody;
        public RigidbodyInterpolation rigidbodyInterpolationOnServer = RigidbodyInterpolation.Interpolate;

        public override void OnStartServer()
        {
            base.OnStartServer();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!IsServerStarted)
            {
                rigidbody.isKinematic = true;
            }

            if (IsHostInitialized)
            {
                TimeManager.OnTick += TimeManager_OnTick;
            }
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            TimeManager.OnTick -= TimeManager_OnTick;
        }

        private void TimeManager_OnTick()
        {
            if (rigidbody.interpolation != rigidbodyInterpolationOnServer)
            {
                rigidbody.interpolation = rigidbodyInterpolationOnServer;
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if (rigidbody == null) rigidbody = GetComponent<Rigidbody>();
        }

        public void AddForce(Vector3 force, Vector3 point, ForceMode mode = ForceMode.VelocityChange)
        {
            rigidbody.AddForceAtPosition(force, point, mode);
        }

    }
}
