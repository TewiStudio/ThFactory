using UnityEngine;
using Tewi.Game.Player;

namespace Tewi.Game.Interactable.PickupItems
{
    public class Flashlight : PickupItem
    {
        public Transform model;
        public AudioSource openSound;

        public override void Attack(PlayerManager player)
        {
            openSound.Stop();
            openSound.Play();
            model.gameObject.SetActive(!model.gameObject.activeSelf);
        }
    }
}