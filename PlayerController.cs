/*
using FishNet.Object;
using System.Collections.Generic;
using Tewi.Game.Interactable;
using Tewi.Game.Player;
using UnityEngine;

public class PlayerInventory1 : NetworkBehaviour
{
    public PlayerManager playerManager;
    public Transform inventory;
    public Transform pickupParent;

    [Header("Interactable")]
    public List<int> usedPickupItemIndex = new();
    public PickupItem nowPickupItem;
    public int nowPickupItemIndex = 0;
    public int maxPickupItemCount = 4;
    //public Vector3 pickupItemPosition = new(0, 1, 1);

    float mouseWheelData = 0f;

    private void Update()
    {
        if (nowPickupItem)
        {
            foreach (var key in nowPickupItem.InPlayerHandUseKeys)
            {
                if (Input.GetKeyDown(key))
                    nowPickupItem.OnKeyDown(key, playerManager);
                else if (Input.GetKeyUp(key))
                    if (nowPickupItem) nowPickupItem.OnKeyUp(key, playerManager);
            }
        }
        if (nowPickupItem)
        {
            foreach (var button in nowPickupItem.InPlayerHandUseButton)
            {
                if (Input.GetButtonDown(button)) nowPickupItem.OnButtonDown(button, playerManager);
                else if (Input.GetButtonUp(button))
                    if (nowPickupItem) nowPickupItem.OnButtonUp(button, playerManager);
            }
        }

        mouseWheelData = Input.GetAxisRaw("Mouse ScrollWheel");
        if (mouseWheelData != 0f)
        {
            int index = 0;
            if (mouseWheelData > 0)
            {
                index = nowPickupItemIndex + 1;
                if (index >= maxPickupItemCount) index = 0;
            }
            else if (mouseWheelData < 0)
            {
                index = nowPickupItemIndex - 1;
                if (index < 0) index = maxPickupItemCount - 1;
            }
            nowPickupItemIndex = index;
            SwitchToInventoryAt(index);
        }
    }

    private void LateUpdate()
    {
        if (nowPickupItem)
        {
            nowPickupItem.transform.SetLocalPositionAndRotation(nowPickupItem.pickupOffset, Quaternion.Euler(nowPickupItem.pickupRotate));
        }
    }

    /// <summary>
    /// 捡起 <paramref name="item"/>
    /// </summary>
    /// <param name="item">将要捡起的<see cref="PickupItem"/></param>
    /// <returns>成功捡起返回 true，否则返回 false</returns>
    public bool PickupPickupItem(PickupItem item)
    {
        if (inventory.childCount >= maxPickupItemCount) return false;
        if (!item) return false;
        for (var i = 0; i < maxPickupItemCount; i++)
        {
            if (!usedPickupItemIndex.Contains(i))
            {
                item.inInventoryIndex = i;
                break;
            }
        }
        item.interactable.Value = false;
        item.SetRigidbodyActive(false);
        AddPickupItemInventory(item);
        return true;
    }

    /// <summary>
    /// 扔掉 <paramref name="item"/>
    /// </summary>
    /// <param name="item">将要扔掉的<see cref="PickupItem"/></param>
    /// <returns>成功扔掉返回 true，否则返回 false</returns>
    public bool DropDownPickupItem(PickupItem item)
    {
        if (!item) return false;
        RemovePickupItemInventory(item);
        item.inInventoryIndex = -1;
        item.interactable.Value = true;
        item.SetRigidbodyActive(true);
        //StartCoroutine(RemoveItemFormInventory(item));
        item.OnDrop(playerManager);
        return true;
        //item.transform.localPosition = new(0, 1, 3);
    }

    public void SwitchToInventoryAt(int index)
    {
        if (usedPickupItemIndex.Contains(index))
        {
            var list = inventory.GetComponentsInChildren<PickupItem>(true);
            foreach (var i in list)
            {
                if (i.inInventoryIndex == index)
                {
                    if (!i.isOtherPickupItemChildren) nowPickupItem = i;
                    i.SetActive(true);
                }
                else
                {
                    i.SetActive(false);
                }
            }
        }
        else
        {
            var list = inventory.GetComponentsInChildren<PickupItem>();
            foreach (var i in list)
            {
                i.SetActive(false);
            }
            nowPickupItem = null;
        }
    }

    private void AddPickupItemInventory(PickupItem item)
    {
        usedPickupItemIndex.Add(item.inInventoryIndex);
        item.transform.SetParent(inventory.transform);
        item.transform.SetLocalPositionAndRotation(item.pickupOffset, Quaternion.Euler(item.pickupRotate));
        nowPickupItemIndex = item.inInventoryIndex;
        SwitchToInventoryAt(item.inInventoryIndex);
    }

    private void RemovePickupItemInventory(PickupItem item)
    {
        usedPickupItemIndex.Remove(item.inInventoryIndex);
        item.transform.SetParent(pickupParent);
        item.transform.localPosition = playerManager.transform.forward * 1.5f + playerManager.transform.position;
        //item.transform.localRotation = new Quaternion(0, item.transform.rotation.y, 0, item.transform.rotation.w);
        nowPickupItem = null;
        //StartCoroutine(SetDropItemRotation(item));
    }
*//*
    private IEnumerator SetDropItemRotation(PickupItem item)
    {
        yield return new WaitUntil(() => item.transform.localRotation.x != 0 || item.transform.localRotation.z != 0 || item.fixedOnGroundRigidbody ? !item.fixedOnGroundRigidbody.testOnTheGround : true);
        item.transform.localRotation = new Quaternion(0, item.transform.rotation.y, 0, item.transform.rotation.w);
        //Debug.Log(item);
    }*//*

}*/