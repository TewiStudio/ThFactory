using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using FishNet.Object;
using Tewi.Helpers;
using Tewi.Game.Console;
using Tewi.Game.Factory.Presentation;

namespace Tewi.Game.Player.Cameras
{
    public class CameraManager : NetworkBehaviour
    {
        public PlayerManager playerManager;
        public Camera playerCamera;
        public CameraCulling cameraCulling;

        public override void OnStartClient()
        {
            base.OnStartClient();
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
        }
    }
}
