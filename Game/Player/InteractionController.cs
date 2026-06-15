using UnityEngine;
using FishNet.Object;
using Tewi.Game.Interactable;

namespace Tewi.Game.Player 
{
    public class InteractionController : MonoBehaviour
    {
        public PlayerManager playerManager;
        public Transform casterTransform;
        private IInteractable lastLookAtInteractableItem = null;

        [Header("Interactable")]
        public IInteractable nowInteractItemPlayerLooks;
        public float interactableDistance = 5f;
        public bool interactKeyDown = false;
        public float holdInteractKeyTime = 0f;

        private void LateUpdate()
        {
            DetectInteractable();
        }

        internal void DetectInteractable()
        {
            var lineCastPositionStart = casterTransform.position - casterTransform.forward * .4f;
            var lineCastPositionEnd = casterTransform.forward;
            var detected = Physics.Raycast(lineCastPositionStart, lineCastPositionEnd, out var hitInfo, interactableDistance, ~LayerMask.GetMask("HitBox", "Damageable", "Ignore Raycast"));
            if (playerManager.isModalUIOpened) detected = false;
            Debug.DrawLine(lineCastPositionStart, lineCastPositionStart + lineCastPositionEnd * interactableDistance, Color.yellow);

            IInteractable result = null;
            if (detected)
            {
                if (hitInfo.collider != null)
                {
                    var rb = hitInfo.collider.attachedRigidbody;
                    if (rb != null)
                        result = rb.GetComponent<IInteractable>();
                }

                if (result is null && hitInfo.collider != null)
                {
                    result = hitInfo.collider.GetComponentInParent<IInteractable>();
                }
            }
            nowInteractItemPlayerLooks = result;
            LookAtInteractableItem(result);
            if (Input.GetKeyDown(playerManager.interactKey)) interactKeyDown = true;
            if (Input.GetKeyUp(playerManager.interactKey)) interactKeyDown = false;
            if (interactKeyDown && result is not null)
            {
                if (result.InteractTime > 0)
                {
                    holdInteractKeyTime += Time.deltaTime;
                    if (holdInteractKeyTime > result.InteractTime)
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

        public void LookAtInteractableItem(IInteractable nowItem)
        {
            if (nowItem is null)
            {
                lastLookAtInteractableItem?.OnPlayerNotLooking(playerManager);
                return;
            }

            if (nowItem == lastLookAtInteractableItem) return;
            if (!nowItem.IsInteractable) return;

            lastLookAtInteractableItem?.OnPlayerNotLooking(playerManager);
            nowItem.OnPlayerLookAt(playerManager);
        }

        public void ClearLastLookAtItem()
        {
            lastLookAtInteractableItem = null;
        }
    }
}
