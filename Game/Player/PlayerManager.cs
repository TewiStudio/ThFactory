using System.Text;
using UnityEngine;
using ECM2;
using FishNet;
using FishNet.Object;
using FishNet.Connection;
using FishNet.Component.Transforming;
using Tewi.Console;
using Tewi.Game.Network;
using Tewi.Game.Player.Abilitys;
using Tewi.Game.Player.Body;
using Tewi.Game.Player.Cameras;
using Tewi.Game.Player.Damageable;
using Tewi.Game.Player.Movement;
using Tewi.Game.UI;
using Tewi.Helpers;
using Tewi.Helpers.Extensions;

namespace Tewi.Game.Player
{
    public struct InputData
    {
        public Vector2 direction;
        public Quaternion bodyRotation;
        public bool jump;
        public bool sprint;
        public bool crouch;
        public float xDelta;
        public float yDelta;
    }

    public class PlayerManager : NetworkBehaviour
    {
        static readonly InputData _defaultInputData = new()
        {
            direction = Vector2.zero,
            bodyRotation = Quaternion.identity,
            jump = false,
            sprint = false,
            crouch = false,
            xDelta = 0,
            yDelta = 0
        };

        #region attrs
        public NetworkGameManager gameManager;
        public int ID;
        public UIManager uiManager => gameManager.uiManager;
        public UIToolkitManager uiToolkitManager => gameManager.uiToolkitManager;

        [Header("Components")]
        public NetworkTransform networkTransform;
        public Transform head;
        public Transform body;
        public Transform originalParent;
        public Rigidbody characterRigidbody;

        [Header("Scripts")]
        public CameraManager cameraManager;
        public PlayerCharacter character;
        public CharacterMovement characterMovement;
        public PlayerInventory playerInventory;
        public PlayerHealth playerHealth;
        public PlayerGroundDetect groundDetect;
        public SprintAbility sprintAbility;
        public LadderClimbAbility ladderClimbAbility;
        public BodyManager bodyManager;
        public InteractionController interactionController;
        public ItemViewLag itemViewLag;
        public ItemWalkBob itemWalkBob;

        [Header("Stamina")]
        [Tooltip("耐力")]
        /// <summary>
        /// 耐力
        /// </summary>
        public int Stamina = 100;

        [Header("Mouse")]
        public float mouseSensitivity = 1f;
        [ReadOnly] public float mouseX;
        [ReadOnly] public float mouseY;
        public float _xRotation;
        public float _yRotation;
        [ReadOnly] public Quaternion bodyYRotation;

        [Header("Key Bindings")]
        public KeyCode scanKey = KeyCode.Mouse1;
        public KeyCode SprintKey = KeyCode.LeftShift;
        public KeyCode CrouchKey = KeyCode.LeftControl;
        public KeyCode interactKey = KeyCode.E;
        public KeyCode dropItemKey = KeyCode.G;
        [ReadOnly] public bool isModalUIOpened = false;

        private InputData _inputData;
        private Quaternion _lastCamRot;
        private Rigidbody _cacheGroundRigidbody;
        #endregion

        private void Update()
        {
            if (!IsOwner) return;
            HandleCharacterInput();
            SimulatePlayerMovement(_inputData);
        }

        private void LateUpdate()
        {
            if (!IsOwner)
            {
                head.localRotation = Quaternion.Euler(bodyManager.transform.localRotation.eulerAngles.SetX(0).SetZ(0));
                bodyManager.transform.rotation = body.rotation;
                return;
            }
            SimulateCameraInput(_inputData);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            gameManager = InstanceFinder.GetInstance<NetworkGameManager>();
            if (IsOwner)
            {
                gameManager.localPlayer = this;
                character.enabled = true;
                characterMovement.enabled = true;
                characterMovement.collider.enabled = true;
                interactionController.enabled = true;

                TimeManager.OnTick += TimeManager_OnTick;
                uiManager.isAnyModalUIActive.OnChanged += IsAnyModalUIActive_OnChanged;
                uiManager.OnPlayerAwake();

                if (IsServerInitialized)
                {
                    characterRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                }
            }
            else
            {
                character.enabled = false;
                characterMovement.enabled = false;
                characterMovement.collider.enabled = false;
                interactionController.enabled = false;
                //characterRigidbody.interpolation = RigidbodyInterpolation.None;
                //characterMovement.collisionLayers = characterMovement.collisionLayers & ~LayerMask.GetMask("Rigidbody");
            }
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            if (IsOwner)
            {
                gameManager.localPlayer = null;
                uiManager.OnPlayerDestroy();
                uiManager.isAnyModalUIActive.OnChanged -= IsAnyModalUIActive_OnChanged;
                TimeManager.OnTick -= TimeManager_OnTick;
            }
        }

        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);
            if (IsOwner)
            {
                if (IsClientOnlyInitialized)
                {
                    Debug.Log($"Player is requesting server sync to {Owner.ClientId}");
                    SyncToClient(Owner);
                }
            }
        }

        [ServerRpc]
        public void SyncToClient(NetworkConnection conn)
        {
            Debug.Log($"Syncing to client {conn.ClientId}");
            gameManager.FactoryManager.SyncSimulation(conn);
            gameManager.FactoryManager.SyncSpatial(conn);
        }

        private void TimeManager_OnTick()
        {
            if (transform.position.y < -1000) playerHealth.RequestKill();
        }

        private void IsAnyModalUIActive_OnChanged(bool value)
        {
            isModalUIOpened = value;
        }

        private void HandleCharacterInput()
        {
            if (isModalUIOpened)
            {
                _inputData = _defaultInputData;
                return;
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow)) DebugAddHorizontalVelocity(-.5f);
            if (Input.GetKeyDown(KeyCode.RightArrow)) DebugAddHorizontalVelocity(.5f);

            mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

            InputData inputData = new()
            {
                direction = new Vector2(Input.GetAxisRaw("Horizontal") + horizontalInput, Input.GetAxisRaw("Vertical") + verticalInput),
                bodyRotation = transform.rotation * body.rotation,
                crouch = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C),
                sprint = Input.GetKey(KeyCode.LeftShift),
                jump = Input.GetButton("Jump"),
                xDelta = mouseX,
                yDelta = mouseY
            };
            _inputData = inputData;
        }

        private void SimulatePlayerMovement(InputData inputData)
        {
            var pState = PlayerState.Idle;

            // Movement 
            Vector3 movementDirection = Vector3.zero;
            movementDirection += Vector3.right * inputData.direction.x;
            movementDirection += Vector3.forward * inputData.direction.y;
            movementDirection = movementDirection.relativeTo(body);
            character.SetMovementDirection(movementDirection);
            if (movementDirection.magnitude != 0)
            {
                pState = PlayerState.Walking;
            }

            // Crouch
            if (inputData.crouch)
                character.Crouch();
            else
                character.UnCrouch();

            // Sprint
            if (inputData.sprint)
                sprintAbility.Sprint();
            else
                sprintAbility.StopSprinting();

            // Jump
            if (inputData.jump)
                character.Jump();
            else
                character.StopJumping();

            bodyManager.SetPlayerState(pState);
        }

        private void SimulateCameraInput(InputData inputData)
        {
            _xRotation -= inputData.yDelta;
            _xRotation = Mathf.Clamp(_xRotation, -85, 85);
            _yRotation += inputData.xDelta % 360;
            _yRotation %= 360;

            head.localRotation = Quaternion.Euler(_xRotation, _yRotation, 0);
            body.localRotation = Quaternion.Euler(0, _yRotation, 0);
            if (cameraManager && cameraManager.playerCamera)
            {
                cameraManager.playerCamera.transform.rotation = head.rotation;
                bodyManager.transform.rotation = body.rotation;
            }

            Quaternion camRot = head.localRotation;
            Quaternion delta = camRot * Quaternion.Inverse(_lastCamRot);

            itemViewLag.AddRotationLag(delta);
            itemWalkBob.moveSpeed = characterMovement.velocity.magnitude;

            _lastCamRot = camRot;
        }

        float horizontalInput = 0;
        float verticalInput = 0;
        [ConsoleCommand("phor", "Add horizontal velocity")]
        public string DebugAddHorizontalVelocity(float value)
        {
            horizontalInput += value;
            return $"Added horizontal velocity: {value}";
        }

        [ConsoleCommand("pver", "Add vertical velocity")]
        public string DebugAddVerticalVelocity(float value)
        {
            verticalInput += value;
            return $"Added vertical velocity: {value}";
        }

        [ConsoleCommand("ptp", "Teleport to a specific position.")]
        public string DebugTeleport(Vector3 position)
        {
            character.SetPosition(position);
            return $"Teleported to: {position}";
        }

        [ConsoleCommand("pint", "Set interpolation value.")]
        public string DebugInterpolation(ushort value)
        {
            if (!IsServerStarted)
            {
                return "Cannot set interpolation value on client. This command can only be used on the server.";
            }
            ObserversInterpolation(value);
            return $"Set interpolation: {value}";
        }

        [ObserversRpc(RunLocally = true)]
        private void ObserversInterpolation(ushort value)
        {
            foreach (PlayerManager playerManager in FindObjectsByType<PlayerManager>(FindObjectsSortMode.None))
            {
                playerManager.networkTransform.SetInterpolation(value);
            }
        }

        [ConsoleCommand("psend", "Set send interval.")]
        public string DebugSendInterval(byte value)
        {
            if (!IsServerStarted)
            {
                return "Cannot set send interval on client. This command can only be used on the server.";
            }
            ObserversSetSendInterval(value);
            return $"Set send interval: {value}";
        }

        [ObserversRpc(RunLocally = true)]
        private void ObserversSetSendInterval(byte value)
        {
            networkTransform.SetInterval(value);
            Debug.Log($"Set send interval: {value}");
        }

        [ConsoleCommand("pause")]
        public string DebugPause(bool value)
        {
            character.Pause(value);
            return $"Player simulation paused: {value}";
        }
    }
}