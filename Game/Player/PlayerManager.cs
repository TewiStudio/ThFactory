using UnityEngine;
using ECM2;
using FishNet;
using FishNet.Object;
using FishNet.Component.Transforming;
using Tewi.Helpers;
using Tewi.Helpers.Extensions;
using Tewi.Game.Player.UI;
using Tewi.Game.Player.Body;
using Tewi.Game.Player.Cameras;
using Tewi.Game.Player.Abilitys;
using Tewi.Game.Player.Movement;
using Tewi.Game.Player.Damageable;
using Tewi.Game.Network;

namespace Tewi.Game.Player
{
    public struct InputData
    {
        public Vector2 direction;
        public Quaternion bodyRotation;
        public bool jump;
        public bool sprint;
        public bool crouch;
        public float xRotation;
        public float yRotation;
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
            xRotation = 0,
            yRotation = 0
        };

        #region attrs
        public NetworkGameManager gameManager;
        public int ID;
        [Header("Components")]
        public NetworkTransform networkTransform;
        public Transform head;
        public Transform body;
        public Transform originalParent;
        public Rigidbody characterRigidbody;
        public UIManager uiManager;

        [Header("Scripts")]
        public PlayerCamera playerCamera;
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
            if (isModalUIOpened) return;
            HandleCharacterInput();
            SimulatePlayerMovement(_inputData);
        }

        private void LateUpdate()
        {
            if (!IsOwner)
            {
                head.localRotation = Quaternion.Euler(bodyManager.transform.localRotation.eulerAngles.SetX(0).SetZ(0));
                return;
            }
            if (isModalUIOpened) return;
            SimulateCameraInput(_inputData);
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            gameManager = InstanceFinder.GetInstance<NetworkGameManager>();
            if (Owner.IsLocalClient)
            {
                uiManager.gameObject.SetActive(true);
                uiManager.isAnyModalUIActive.OnChanged += IsAnyModalUIActive_OnChanged;

                TimeManager.OnTick += TimeManager_OnTick;
                characterRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                character.enabled = true;
                characterMovement.enabled = true;
            }
            else
            {
                uiManager.gameObject.SetActive(false);
                characterRigidbody.interpolation = RigidbodyInterpolation.None;
                character.enabled = false;
                characterMovement.enabled = false;
                characterMovement.collisionLayers = characterMovement.collisionLayers & ~LayerMask.GetMask("Rigidbody");
            }
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            uiManager.isAnyModalUIActive.OnChanged -= IsAnyModalUIActive_OnChanged;
            TimeManager.OnTick -= TimeManager_OnTick;
        }

        private void TimeManager_OnTick()
        {
            if (transform.position.y < -1000) playerHealth.RequestKill();
        }

        private void IsAnyModalUIActive_OnChanged(bool value)
        {
            isModalUIOpened = value;
            SimulatePlayerMovement(_defaultInputData);
        }

        private void HandleCharacterInput()
        {
            mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

            _yRotation += mouseX;
            _yRotation %= 360;
            _xRotation -= mouseY;
            _xRotation = Mathf.Clamp(_xRotation, -85f, 85f);

            InputData inputData = new()
            {
                direction = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
                bodyRotation = transform.rotation * body.rotation,
                crouch = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C),
                sprint = Input.GetKey(KeyCode.LeftShift),
                jump = Input.GetButton("Jump"),
                xRotation = _xRotation,
                yRotation = _yRotation
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
            head.localRotation = Quaternion.Euler(inputData.xRotation, inputData.yRotation, 0);
            body.localRotation = Quaternion.Euler(0, inputData.yRotation, 0);

            Quaternion camRot = head.localRotation;
            Quaternion delta = camRot * Quaternion.Inverse(_lastCamRot);

            itemViewLag.AddRotationLag(delta);
            itemWalkBob.moveSpeed = characterMovement.velocity.magnitude;

            _lastCamRot = camRot;
        }
    }
}