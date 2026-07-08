using UnityEngine;
using FishNet.Object;

namespace Tewi.Game.Player.Cameras
{
    public class CameraManager : NetworkBehaviour
    {
        public PlayerManager playerManager;
        public Camera playerCamera;
        public AudioListener cameraListener;
        public CameraCulling cameraCulling;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            if (Owner.IsLocalClient)
            {
                playerCamera.enabled = true;
                cameraListener.enabled = true;
            }
            else
            {
                playerCamera.enabled = false;
                cameraListener.enabled = false;
            }
        }

        public override void OnStopNetwork()
        {
            playerCamera.enabled = false;
            cameraListener.enabled = false;
            base.OnStopNetwork();
        }
    }
}
