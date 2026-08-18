using FishNet.Managing.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using Tewi.Game.Network;
using Tewi.Game.Player.UI.Styles;
using Tewi.Helpers;
using Unity.VisualScripting;
using UnityEngine;

namespace Tewi.Game.Player.UI
{
    public class UIManager : MonoBehaviour
    {
        public Bindable<bool> isAnyModalUIActive = new(false);
        public NetworkGameManager gameManager;
        public Canvas UIRoot;
        public float scaler = 1f;

        public PlayerManager playerManager => gameManager.localPlayer;
        public bool IsPlayerReady => playerManager;

        private readonly Dictionary<Type, IUIBase> _uiRegistry = new();
        private readonly List<IUIBase> _modalStack = new();

        private void Awake()
        {
            // 自动注册场景中已有的 UI
            var existingUIs = GetComponentsInChildren<IUIBase>(true);
            foreach (var ui in existingUIs)
            {
                _uiRegistry[ui.GetType()] = ui;
            }
            OnModalStackChanged();
        }

        public TUI Open<TUI, TContext>(TContext context) where TUI : UIBase<TContext>
        {
            Type type = typeof(TUI);
            if (_uiRegistry.TryGetValue(type, out IUIBase ui))
            {
                OpenUI(ui, context);
                return (TUI)ui;
            }

            Debug.LogError($"[UIManager] UI {type} 未在注册表中，请检查是否已挂载到 UI 根节点下");
            return null;
        }

        public void Close<TUI>() where TUI : IUIBase
        {
            if (_uiRegistry.TryGetValue(typeof(TUI), out IUIBase ui))
            {
                ui.Close();
            }
        }

        private void OpenUI(IUIBase ui, object context = null)
        {
            ui.Open(context);
            HandleModalOpen(ui);
        }

        private void HandleModalOpen(IUIBase ui)
        {
            if (!ui.IsModal)
                return;

            AddToModalStack(ui);
            playerManager.isModalUIOpened = true;
        }

        internal void NotifyClosed(IUIBase ui)
        {
            if (ui.IsModal)
            {
                _modalStack.Remove(ui);
                OnModalStackChanged();
            }
        }

        internal void AddToModalStack(IUIBase ui)
        {
            if (!_modalStack.Contains(ui))
            {
                _modalStack.Add(ui);
                OnModalStackChanged();
            }
        }

        private void UpdateModalDimmer()
        {
            // 这里可以控制黑色背景遮罩的层级
            // Dimmer.SetAsLastSibling(); 

            if (isAnyModalUIActive.Value)
                (_modalStack.Last() as MonoBehaviour).transform.SetAsLastSibling();
        }

        private void UpdateCursorState()
        {
            if (isAnyModalUIActive.Value)
                Cursor.lockState = CursorLockMode.None;
            else
                Cursor.lockState = CursorLockMode.Locked;
        }

        private void OnModalStackChanged()
        {
            if (!IsPlayerReady) return;
            isAnyModalUIActive.Value = _modalStack.Count > 0;
            UpdateModalDimmer();
            UpdateCursorState();
        }

        public void Init()
        {
            OnModalStackChanged();
        }

        void Update()
        {
            if (!IsPlayerReady) return;
            if (!playerManager.IsOwner) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (isAnyModalUIActive.Value)
                {
                    _modalStack[^1].Close();
                }
                else
                {
                    Open<PauseUI, object>(null);
                }
            }

            foreach (var item in _uiRegistry)
            {
                if (item.Value.IsOpen) continue;
                if (Input.GetKeyDown(item.Value.ModalHotKey))
                {
                    OpenUI(item.Value);
                }
            }

            if (Input.GetKeyDown(KeyCode.T) && !isAnyModalUIActive.Value)
            {
                gameManager.NodeCoordinator.ServerRequestCreateNode(0,
                    playerManager.transform.position + playerManager.body.forward,
                    playerManager.body.rotation);
            }
/*

            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                if (isAnyModalUIActive.Value)
                {
                    foreach (var item in _modalStack)
                    {
                        if (item as NodeDebugUI is NodeDebugUI nodeDebugUI)
                        {
                            nodeDebugUI.Close();
                            break;
                        }
                    }
                } 
                else 
                    Open<NodeDebugUI, object>(null);
            }*/
        }
    }
}