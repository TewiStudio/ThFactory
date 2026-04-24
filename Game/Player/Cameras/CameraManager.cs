using UnityEngine;
using FishNet.Object;

namespace Tewi.Game.Player.Cameras
{
    public class PlayerCamera : NetworkBehaviour
    {
        public PlayerManager playerManager;
        public new Camera camera;
        [SerializeField] private Camera _cameraPrefab;
        [SerializeField] private Transform _cameraHolder;

        public override void OnStartClient()
        {
            // Since this will run on all clients that this object spawns for
            // we need to only instantiate the camera for the object we own.
            if (IsOwner)
            {
                camera = Instantiate(_cameraPrefab, _cameraHolder);
                //playerManager.character.camera = camera;
            }
        }
    }
}
