using UnityEngine;
using PrimeTween;
using Tewi.Game.Player;

namespace Tewi.Game.Interactable.Door
{
    public class InteractableDoorItem : InteractableItem
    {
        public bool isOpen = false;
        public Transform p;
        public Collider doorCollider;

        public override void OnPlayerLookAt(PlayerManager player)
        {
            base.OnPlayerLookAt(player);
        }

        public override void OnPlayerNotLooking(PlayerManager player)
        {
            base.OnPlayerNotLooking(player);
        }

        public override void OnInteract(PlayerManager player)
        {
            base.OnInteract(player);
            if (isOpen)
            {
                OnCloseDo(player);
            }
            else
            {
                OnOpenDo(player);
            }
        }

        private void OnValidate()
        {
            if (isOpen) OnOpenDo(null);
            else OnCloseDo(null);
        }

        public virtual void OnOpenDo(PlayerManager player)
        {
            interactTime = 0f;
            isOpen = true;
            if (p)
            {
                var seq = Tween.LocalRotation(p, Quaternion.Euler(0, 90, 0), .5f);
                //p.localRotation = Quaternion.Euler(0, 90, 0);
            }
            defaultPlayerLookText = "¹Ø±Õ";
            if (player) UpdatePlayerShowsTextUI(player);
            doorCollider.enabled = false;
        }

        public virtual void OnCloseDo(PlayerManager player)
        {
            isOpen = false;
            if (p)
            {
                Tween.LocalRotation(p, Quaternion.Euler(0, 0, 0), .5f);
                //p.localRotation = Quaternion.Euler(0, 0, 0);
            }
            defaultPlayerLookText = "´ò¿ª";
            interactTime = .87f;
            if (player) UpdatePlayerShowsTextUI(player);
            doorCollider.enabled = true;
        }
    }
}