using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Tewi.Helpers;
using Tewi.Game.Network;
using Tewi.Game.Player.UI.Styles;

namespace Tewi.Game.Player.UI
{
    public class UIManager : MonoBehaviour
    {
        private NetworkGameManager gameManager => playerManager.gameManager;

        public Bindable<bool> isAnyModalUIActive = new(false);
        public PlayerManager playerManager;
        public float scaler = 1f;

        [SerializeField] private Canvas UIRoot;

        private Dictionary<Type, IUIBase> _uiRegistry = new();
        private List<IUIBase> _modalStack = new();

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
                TUI targetUI = (TUI)ui;
                targetUI.OnOpen(context);

                // 如果是模态窗口，处理遮罩和层级
                if (targetUI.IsModal)
                {
                    AddToModalStack(targetUI);
                    playerManager.isModalUIOpened = true;
                }

                return targetUI;
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

        internal void NotifyClosed(IUIBase ui)
        {
            if (ui.IsModal)
            {
                _modalStack.Remove(ui);
                OnModalStackChanged();
            }
        }

        private void AddToModalStack(IUIBase ui)
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
            isAnyModalUIActive.Value = _modalStack.Count > 0;
            UpdateModalDimmer();
            UpdateCursorState();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (isAnyModalUIActive.Value)
                {
                    _modalStack.Last().Close();
                }
                else
                {
                    Open<PauseUI, object>(null);
                }
            }

            if (Input.GetKeyDown(KeyCode.T))
            {
                gameManager.nodeCoordinator.CreateNode(0,
                    playerManager.transform.position + playerManager.transform.forward,
                    playerManager.transform.rotation);
            }
        }
    }
}