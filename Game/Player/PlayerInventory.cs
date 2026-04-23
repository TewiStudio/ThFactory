using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using Tewi.Game.Interactable;
using Tewi.Helpers;
using UnityEngine;

namespace Tewi.Game.Player
{
    public class PlayerInventory : NetworkBehaviour
    {
        struct InventoryItem
        {
            public PickupItem pickupItem;
            public int inventoryIndex;
        }

        public PlayerManager playerManager;
        public Transform pickupParent;

        [Header("Interactable")]
        public List<byte> _usedPickupItemIndex = new();
        public byte _inventoryIndex = 0;
        public int _maxPickupItemCount = 4;

        public readonly SyncVar<PickupItem> _nowPickupItem = new(null);
        private PickupItem _tempPickupItem = null;

        public PickupItem nowPickupItem;

        [ServerRpc]
        public void RequestPickupItem(PickupItem pickupItem)
        {
            if (!pickupItem) return;
            if (transform.childCount >= _maxPickupItemCount) return;

            pickupItem.GiveOwnership(Owner);

            ObserversPickupItem(pickupItem);
            pickupItem.OnPickUp(playerManager);

            _nowPickupItem.Value = pickupItem;
        }

        [ServerRpc]
        public void RequestDropDownItem(PickupItem pickupItem, Vector2 playerSpeed)
        {
            if (!pickupItem) return;

            ObserversRemovePickupItem(pickupItem);
            pickupItem.RemoveOwnership();
            _nowPickupItem.Value = null;
            pickupItem.OnDrop(playerManager, playerSpeed);
        }

        [ServerRpc]
        public void RequestChangeNowPickupItem(PickupItem pickupItem)
        {
            _nowPickupItem.Value = pickupItem;
        }

        [ObserversRpc(RunLocally = true)]
        public void ObserversPickupItem(PickupItem pickupItem)
        {
            if (IsOwner)
            {
                for (byte i = 1; i < _maxPickupItemCount; i++)
                {
                    if (!_usedPickupItemIndex.Contains(i))
                    {
                        pickupItem.inInventoryIndex = i;
                        break;
                    }
                }
                _usedPickupItemIndex.Add(pickupItem.inInventoryIndex);
                _inventoryIndex = pickupItem.inInventoryIndex;
            }
            SetItemParent(pickupItem, true);
        }

        [ObserversRpc(RunLocally = true)]
        public void ObserversRemovePickupItem(PickupItem pickupItem)
        {
            if (IsOwner)
            {
                _usedPickupItemIndex.Remove(pickupItem.inInventoryIndex);
                pickupItem.inInventoryIndex = 0;
            }
            SetItemParent(pickupItem, false);
        }

        public void SetItemParent(PickupItem pickupItem, bool isChild)
        {
            if (isChild)
            {
                pickupItem.NetworkObject.SetParent(this);

            }
            else
                pickupItem.NetworkObject.UnsetParent();
        }

        private List<PickupItem> _itemCache = new();
        private void SwitchInventory(byte index)
        {
            GetComponentsInChildren(true, _itemCache);

            // 判断这个 index 是否在背包中
            bool isValidIndex = _usedPickupItemIndex.Contains(index);

            PickupItem temp = null;
            foreach (var item in _itemCache)
            {
                // 如果按键的 index 是有效的，且恰好匹配当前遍历到的物品
                if (isValidIndex && item.inInventoryIndex == index)
                {
                    // 启用模型
                    item.gameObject.SetActive(true);
                    temp = item;
                }
                else
                {
                    // 其余所有物品全部隐藏
                    item.gameObject.SetActive(false);
                }
            }
            RequestChangeNowPickupItem(temp);
        }

        private void SwitchInventory(PickupItem pickupItem)
        {
            GetComponentsInChildren(true, _itemCache);
            foreach (var item in _itemCache)
            {
                if (item != pickupItem) item.gameObject.SetActive(false);
            }
            if (pickupItem) pickupItem.gameObject.SetActive(true);
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _nowPickupItem.OnChange += _nowPickupItem_OnChange;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            _nowPickupItem.OnChange -= _nowPickupItem_OnChange;
        }

        private void _nowPickupItem_OnChange(PickupItem prev, PickupItem next, bool asServer)
        {
            nowPickupItem = next;
            SwitchInventory(next);
        }

        private void Update()
        {
            if (!IsOwner) return;
            var item = _nowPickupItem.Value;
            if (item)
            {
                foreach (var key in item.InPlayerHandUseKeys)
                {
                    if (Input.GetKeyDown(key))
                        item.OnKeyDown(key, playerManager);
                    else if (Input.GetKeyUp(key))
                        if (item) item.OnKeyUp(key, playerManager);
                }
            }

            if (item)
            {
                foreach (var button in item.InPlayerHandUseButton)
                {
                    if (Input.GetButtonDown(button)) item.OnButtonDown(button, playerManager);
                    else if (Input.GetButtonUp(button))
                        if (item) item.OnButtonUp(button, playerManager);
                }
            }

            var mouseWheelData = Input.GetAxisRaw("Mouse ScrollWheel");
            if (mouseWheelData != 0f)
            {
                byte index = _inventoryIndex;
                if (mouseWheelData > 0)
                    index += 1;
                else
                    index -= 1;
                if (index >= _maxPickupItemCount) index = 1;
                if (index < 1) index = (byte)_maxPickupItemCount;
                _inventoryIndex = index;
                SwitchInventory(_inventoryIndex);
            }
        }

        [ContextMenu("Print Sync State")]
        private void PrintState()
        {
            Debug.Log($"IsServer:{IsServerStarted} | IsClient:{IsClientStarted} | Index:{_inventoryIndex} | Item:{(_nowPickupItem.Value ? _nowPickupItem.Value.name : null)}");
        }
    }
}