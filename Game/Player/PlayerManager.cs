using System;
using UnityEngine;
using ECM2;
using TMPro;
using FishNet;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Component.Transforming;
using Tewi.Helpers;
using Tewi.Helpers.Extensions;
using Tewi.Game.Player.UI;
using Tewi.Game.Player.Body;
using Tewi.Game.Player.Cameras;
using Tewi.Game.Player.Abilitys;
using Tewi.Game.Player.Movement;
using Tewi.Game.Player.Damageable;
using Tewi.Game.Network.Server;
using Tewi.Game.Interactable;

namespace Tewi.Game.Player
{
    public class PlayerManager : NetworkBehaviour
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

        public struct ReconcileData : IReconcileData
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 velocity;
            public bool isGrounded;

            private uint _tick;
            public void Dispose() { }
            public uint GetTick() => _tick;
            public void SetTick(uint value) => _tick = value;
        }

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
        public ItemViewLag itemViewLag;
        public ItemWalkBob itemWalkBob;

        [Header("Interactable")]
        public InteractableItem nowInteractItemPlayerLooks;
        public float interactableDistance = 5f;
        public bool interactKeyDown = false;
        public float holdInteractKeyTime = 0f;

        [Header("Stamina")]
        [Tooltip("ÄÍÁ¦")]
        /// <summary>
        /// ÄÍÁ¦
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
        public KeyCode interactKey = KeyCode.E;
        public KeyCode dropItemKey = KeyCode.G;
        public KeyCode SprintKey = KeyCode.LeftShift;
        public KeyCode CrouchKey = KeyCode.LeftControl;
        [ReadOnly] public bool isPaused = false;

        private InputData _inputData;
        private Quaternion _lastCamRot;
        private Rigidbody _cacheGroundRigidbody;
        #endregion

        private void Update()
        {
            if (!IsOwner) return;
            if (isPaused) return;
            HandleCharacterInput();
            SimulatePlayerMovement(_inputData);
        }

        private void LateUpdate()
        {
            if (!IsOwner) return;
            if (isPaused) return;
            SimulateCameraInput(_inputData);
            DetectInteractable();
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            gameManager = InstanceFinder.GetInstance<NetworkGameManager>();
            characterMovement.fastPlatformMove = true;
            if (Owner.IsLocalClient)
            {
                TimeManager.OnTick += TimeManager_OnTick;
                uiManager.gameObject.SetActive(true);
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
            TimeManager.OnTick -= TimeManager_OnTick;
        }

        private void TimeManager_OnTick()
        {
            if (transform.position.y < -1000) playerHealth.RequestKill();
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

        private InteractableItem lastLookAtInteractableItem = null;
        private void DetectInteractable()
        {
            var lineCastPositionStart = head.position - head.forward * .4f;
            var lineCastPositionEnd = head.forward;
            var detected = Physics.Raycast(lineCastPositionStart, lineCastPositionEnd, out var hitInfo, interactableDistance, ~LayerMask.GetMask("HitBox", "Damageable", "Ignore Raycast"));
            Debug.DrawLine(lineCastPositionStart, lineCastPositionStart + lineCastPositionEnd * interactableDistance, Color.yellow);
            InteractableItem result = null;
            if (detected)
            {
                if (hitInfo.collider != null)
                {
                    result = hitInfo.collider.GetComponent<InteractableItem>();
                }

                if (!result)
                {
                    var rb = hitInfo.collider.attachedRigidbody;
                    if (rb != null)
                    {
                        result = rb.GetComponent<InteractableItem>();
                    }
                }
            }
            nowInteractItemPlayerLooks = result;
            LookAtInteractableItem(result);
            if (Input.GetKeyDown(interactKey)) interactKeyDown = true;
            if (Input.GetKeyUp(interactKey)) interactKeyDown = false;
            if (interactKeyDown && result)
            {
                if (result.interactTime > 0)
                {
                    holdInteractKeyTime += Time.deltaTime;
                    if (holdInteractKeyTime > result.interactTime)
                    {
                        result.OnInteract(this);
                        interactKeyDown = false;
                    }
                }
                else
                {
                    result.OnInteract(this);
                    interactKeyDown = false;
                }
            }
            else holdInteractKeyTime = 0;
            lastLookAtInteractableItem = result;
        }

        public void LookAtInteractableItem(InteractableItem nowItem)
        {
            if (!nowItem)
            {
                if (lastLookAtInteractableItem) lastLookAtInteractableItem.OnPlayerNotLooking(this);
                return;
            }

            if (nowItem == lastLookAtInteractableItem) return;
            if (!nowItem.interactable.Value) return;

            if (lastLookAtInteractableItem) lastLookAtInteractableItem.OnPlayerNotLooking(this);
            nowItem.OnPlayerLookAt(this);
        }
/*
        [ServerRpc]
        public void land(bool a)
        {
            if (a)
                gameManager.homeManager.HomeLand(gameManager.homeManager.transform.position, gameManager.homeManager.transform.position.SetY(700).SetZ(100), Vector3.zero);
            else
                gameManager.homeManager.HomeLeave(gameManager.homeManager.transform.position.SetY(900).SetZ(0), gameManager.homeManager.transform.position.SetY(900).SetZ(0), Vector3.zero);
        }*/
    }
}