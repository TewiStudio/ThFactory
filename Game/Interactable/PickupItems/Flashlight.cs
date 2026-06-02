using UnityEngine;
using Tewi.Game.Player;
using FishNet.Object;

namespace Tewi.Game.Interactable.PickupItems
{
    public class Flashlight : PickupItem
    {
        public Transform model;
        public AudioSource openSound;

        public override void Attack(PlayerManager player)
        {
            var state = !model.gameObject.activeSelf;
            
            PlaySwitchSound();
            ApplyState(state);

            RequestSwitch(state);
        }

        [ServerRpc]
        private void RequestSwitch(bool isActive)
        {
            ObserversSwitch(isActive);
        }

        [ObserversRpc]
        private void ObserversSwitch(bool isActive)
        {
            if (!IsOwner) PlaySwitchSound();
            ApplyState(isActive);
        }

        private void ApplyState(bool isActive)
        {
            model.gameObject.SetActive(isActive);
        }

        private void PlaySwitchSound()
        {
            openSound.Stop();
            openSound.Play();
        }
    }
}