using FishNet.Object;
using UnityEngine;

namespace Tewi.Game.Player.Cameras
{
    // This script will be a NetworkBehaviour so that we can use the 
    // OnStartClient override.
    public class PlayerCamera : NetworkBehaviour
    {
        public PlayerManager playerManager;
        public new Camera camera;
        [SerializeField] private Camera _cameraPrefab;
        [SerializeField] private Transform _cameraHolder;

        // This method will run on the client once this object is spawned.
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
