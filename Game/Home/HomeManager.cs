using System;
using UnityEngine;
using PrimeTween;
using FishNet.Object;
using Tewi.Game.Player;

namespace Tewi.Game.Home
{
    public class HomeManager : NetworkBehaviour
    {
        public GroundParent groundParent;
        public Rigidbody Rigidbody;
        public Transform openOnStart;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();/*
            if (IsClientOnlyStarted)
            {
                Rigidbody.interpolation = RigidbodyInterpolation.None;
            }*/
        }

        [ObserversRpc]
        public void HomeLand(Vector3 startAnimationPosition, Vector3 endAnimationPosition, Vector3 rotation, bool fastMode = false)
        {
            //transform.SetPositionAndRotation(startAnimationPosition, Quaternion.Euler(rotation));
            Rigidbody.Move(startAnimationPosition, Quaternion.Euler(rotation));
            SetDoorOpen(true);
            Sequence.Create(updateType: UpdateType.FixedUpdate)
                .ChainDelay(.3f)
                .Chain(Tween.RigidbodyMovePosition(Rigidbody, endAnimationPosition, fastMode ? 1 : 16, Ease.Default))
                .OnComplete(() =>
                {
                    //onComplete?.Invoke();
                });
        }

        [ObserversRpc]
        public void HomeLeave(Vector3 startAnimationPosition, Vector3 endAnimationPosition, Vector3 rotation)
        {
            Sequence.Create(updateType: UpdateType.FixedUpdate)
                .Chain(Tween.RigidbodyMovePosition(Rigidbody, endAnimationPosition, 20, Ease.InQuad))
                .ChainCallback(() =>
                {
                    SetDoorOpen(false);
                })
                .Chain(Tween.RigidbodyMoveRotation(Rigidbody, Quaternion.identity, .1f, Ease.Linear))
                .Chain(Tween.RigidbodyMovePosition(Rigidbody, startAnimationPosition, .1f, Ease.Linear))
                .OnComplete(() =>
                {
                    Rigidbody.Move(startAnimationPosition, Quaternion.identity);
                    //onComplete?.Invoke();
                });
        }

        public void SetDoorOpen(bool isDoorOpen)
        {
            openOnStart.gameObject.SetActive(!isDoorOpen);
        }
    }
}