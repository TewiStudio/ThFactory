using System;
using System.Collections.Generic;
using Tewi.Game.Network;
using Tewi.Game.Player;
using Tewi.Game.UI.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tewi.Game.UI
{
    public class UIToolkitManager : MonoBehaviour
    {
        public NetworkGameManager gameManager;
        public PanelRenderer panelRenderer;
        public ModalManager modalManager;
        public PlayerManager playerManager => gameManager.localPlayer;
        public bool IsPlayerReady => playerManager;

        private readonly Dictionary<Type, IViewUI> _views = new();

        private void OnEnable()
        {
            panelRenderer.RegisterUIReloadCallback(OnUIReload);
        }

        private void OnDisable()
        {
            panelRenderer.UnregisterUIReloadCallback(OnUIReload);
        }

        private void OnUIReload(PanelRenderer panelRenderer, VisualElement root, int version)
        {
            foreach (var view in _views.Values)
            {
                view.Reload(root);

                if (view.OpenOnStart)
                {
                    Open(view);
                }
            }

            Debug.Log($"UIToolkitManager: OnUIReload {_views.Count} views, version: {version}");
        }

        public void Register(IViewUI view)
        {
            var type = view.GetType();

            if (!_views.TryAdd(type, view))
            {
                Debug.LogError(
                    $"View already registered: {type.Name}",
                    this);
            }
        }

        public void Unregister(IViewUI view)
        {
            _views.Remove(view.GetType());
        }

        public T Get<T>()
            where T : class, IViewUI
        {
            if (_views.TryGetValue(typeof(T), out var view))
                return view as T;

            Debug.LogError(
                $"View not registered: {typeof(T).Name}",
                this);

            return null;
        }

        public void Open(IViewUI view)
        {
            if (view == null) return;
            if (view.IsModal) modalManager.Push(view);
            view.Open();
        }

        public TView Open<TView>()
            where TView : class, IViewUI
        {
            var view = Get<TView>();
            Open(view);
            return view;
        }

        public TView Open<TView, TData>(TData data)
            where TView : class, IViewUI<TData>
        {
            var view = Get<TView>();

            if (view == null)
                return null;

            view.SetData(data);
            Open(view);

            return view;
        }

        public void Close<T>()
            where T : class, IViewUI
        {
            var view = Get<T>();

            if (view == null)
                return;

            view.Close();
        }

        public void Close(IViewUI view)
        {
            if (view == null)
                return;

            view.Close();
        }

        public void ForceClose<T>()
            where T : class, IViewUI
        {
            var view = Get<T>();

            if (view == null)
                return;

            view.ForceClose();
        }

        public void ForceClose(IViewUI view)
        {
            if (view == null)
                return;

            view.ForceClose();
        }
    }
}