using UnityEngine;
using FishNet.Object;
using Tewi.Game.Interactable;

namespace Tewi.Game.Player 
{
    public class InteractionController : MonoBehaviour
    {
        public PlayerManager playerManager;
        private Transform head => playerManager.head;
        private InteractableItem lastLookAtInteractableItem = null;

        [Header("Interactable")]
        public InteractableItem nowInteractItemPlayerLooks;
        public float interactableDistance = 5f;
        public bool interactKeyDown = false;
        public float holdInteractKeyTime = 0f;

        private void LateUpdate()
        {
            DetectInteractable();
        }

        internal void DetectInteractable()
        {
            var lineCastPositionStart = head.position - head.forward * .4f;
            var lineCastPositionEnd = head.forward;
            var detected = Physics.Raycast(lineCastPositionStart, lineCastPositionEnd, out var hitInfo, interactableDistance, ~LayerMask.GetMask("HitBox", "Damageable", "Ignore Raycast"));
            if (playerManager.isModalUIOpened) detected = false;
            Debug.DrawLine(lineCastPositionStart, lineCastPositionStart + lineCastPositionEnd * interactableDistance, Color.yellow);

            InteractableItem result = null;
            if (detected)
            {
                if (hitInfo.collider.GetComponentInParent<NetworkObject>() is NetworkObject networkObject &&
                    networkObject.GetComponent<InteractableItem>() is InteractableItem interactableItem)
                {
                    result = interactableItem;
                }

                if (!result && hitInfo.collider != null)
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
            if (Input.GetKeyDown(playerManager.interactKey)) interactKeyDown = true;
            if (Input.GetKeyUp(playerManager.interactKey)) interactKeyDown = false;
            if (interactKeyDown && result)
            {
                if (result.interactTime > 0)
                {
                    holdInteractKeyTime += Time.deltaTime;
                    if (holdInteractKeyTime > result.interactTime)
                    {
                        result.OnInteract(playerManager);
                        interactKeyDown = false;
                    }
                }
                else
                {
                    result.OnInteract(playerManager);
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
                if (lastLookAtInteractableItem) lastLookAtInteractableItem.OnPlayerNotLooking(playerManager);
                return;
            }

            if (nowItem == lastLookAtInteractableItem) return;
            if (!nowItem.interactable.Value) return;

            if (lastLookAtInteractableItem) lastLookAtInteractableItem.OnPlayerNotLooking(playerManager);
            nowItem.OnPlayerLookAt(playerManager);
        }

        public void ClearLastLookAtItem()
        {
            lastLookAtInteractableItem = null;
        }
    }
}
