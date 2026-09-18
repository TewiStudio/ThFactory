using System;
using System.Collections.Generic;
using Tewi.Game.UI.Core;
using Tewi.Game.UI.Views;
using UnityEngine;

namespace Tewi.Game.UI
{
    public class ModalManager : MonoBehaviour
    {
        public UIToolkitManager uiToolkitManager;

        private readonly List<IViewUI> _modalStack = new();
        public bool HasModal => _modalStack.Count > 0;
        private IViewUI TopModal => _modalStack.Count > 0 ? _modalStack[^1] : null;

        public void Push(IViewUI view)
        {
            if (view == null || !view.IsModal)
                return;

            if (_modalStack.Contains(view))
                return;

            _modalStack.Add(view);
            view.Closed += Modal_Closed;
            OnModalStackChanged();
        }

        private void Modal_Closed(IViewUI modal)
        {
            modal.Closed -= Modal_Closed;
            _modalStack.Remove(modal);
            OnModalStackChanged();
        }

        public void CloseTop()
        {
            TopModal?.Close();
        }

        private void OnModalStackChanged()
        {
            UpdateCursorState();
            if (uiToolkitManager.IsPlayerReady)
                uiToolkitManager.playerManager.isModalUIOpened = HasModal;
        }

        private void UpdateCursorState()
        {
            if (!uiToolkitManager.IsPlayerReady || HasModal)
                Cursor.lockState = CursorLockMode.None;
            else
                Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (HasModal)
                {
                    CloseTop();
                }
                else
                {
                    uiToolkitManager.Open<PauseView>();
                }
            }
        }
    }
}