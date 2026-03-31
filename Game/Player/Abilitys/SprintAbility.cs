using UnityEngine;
using PrimeTween;
using Tewi.Game.Player.Movement;

namespace Tewi.Game.Player.Abilitys
{
    public class SprintAbility : MonoBehaviour
    {
        [Space(15.0f)]
        public float maxSprintSpeed = 8f;
        public float maxWalkSpeed = 5.5f;
        public bool doSprint = true;
        
        public PlayerCharacter character;

        private bool _isSprinting;
        private bool _sprintInputPressed;
        
        /// <summary>
        /// Request the character to start to sprint. 
        /// </summary>

        public void Sprint()
        {
            _sprintInputPressed = true;
        }
        
        /// <summary>
        /// Request the character to stop sprinting. 
        /// </summary>

        public void StopSprinting()
        {
            _sprintInputPressed = false;
        }
        
        /// <summary>
        /// Return true if the character is sprinting, false otherwise.
        /// </summary>

        public bool IsSprinting()
        {
            return _isSprinting;
        }
        
        /// <summary>
        /// Determines if the character, is able to sprint in its current state.
        /// </summary>

        private bool CanSprint()
        {
            return (character.IsWalking() || character.IsFalling()) && !character.IsCrouched() && doSprint;
        }
        
        /// <summary>
        /// Handles sprint input and adjusts character speed accordingly.
        /// </summary>

        private void CheckSprintInput()
        {
            if (!_isSprinting && _sprintInputPressed && CanSprint())
            {
                _isSprinting = true;

                Tween.Custom(character.maxWalkSpeed, maxSprintSpeed, .2f, (newValue) => character.maxWalkSpeed = newValue, Ease.Linear);

            }
            else if (_isSprinting && (!_sprintInputPressed || !CanSprint()))
            {
                _isSprinting = false;

                Tween.Custom(character.maxWalkSpeed, maxWalkSpeed, .2f, (newValue) => character.maxWalkSpeed = newValue, Ease.Linear);
            }
        }
        
        private void OnBeforeSimulationUpdated(float deltaTime)
        {
            // Handle sprinting
            
            CheckSprintInput();
        }

        private void OnEnable()
        {
            // Subscribe to Character BeforeSimulationUpdated event
            
            character.BeforeSimulationUpdated += OnBeforeSimulationUpdated;
        }
        
        private void OnDisable()
        {
            // Un-Subscribe from Character BeforeSimulationUpdated event
            
            character.BeforeSimulationUpdated -= OnBeforeSimulationUpdated;
        }
    }
}