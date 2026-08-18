using UnityEngine;
using FishNet.Object;
using UnityEngine.Rendering.Universal;

namespace Tewi.Game.Player.Cameras
{
    public class CameraManager : NetworkBehaviour
    {
        public PlayerManager playerManager;
        public UniversalAdditionalCameraData cameraData;
        public Camera playerCamera;
        public CameraCulling cameraCulling;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            if (Owner.IsLocalClient)
            {
                playerCamera.enabled = true;
            }
            else
            {
                playerCamera.enabled = false;
            }
        }

        public override void OnStopNetwork()
        {
            playerCamera.enabled = false;
            base.OnStopNetwork();
        }

        private void LateUpdate()
        {
            if (!playerManager.gameManager) return;

            var audioListener = playerManager.gameManager.AudioListener;
            var camera = playerCamera.transform;
            if (audioListener)
            {
                audioListener.transform.position = camera.position;
                audioListener.transform.rotation = camera.rotation;
            }
        }
    }
}
