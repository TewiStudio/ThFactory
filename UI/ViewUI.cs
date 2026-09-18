using System;
using Tewi.Game.Network;
using Tewi.Game.Player;
using Tewi.Game.UI.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tewi.Game.UI
{
    public abstract class ViewUI<T> : MonoBehaviour, IViewUI
        where T : VisualElement
    {
        public bool openOnStart = false;
        public bool isModal = false;
        public bool canClose = true;

        public virtual bool CanClose => canClose;
        public virtual bool IsModal => isModal;
        public bool OpenOnStart => openOnStart;
        public ViewState State { get; private set; } = ViewState.Closed;

        public abstract string ElementName { get; }
        public T Element { get; private set; }

        public bool IsOpen =>
            State == ViewState.Open ||
            State == ViewState.Opening;

        public bool IsTransitioning =>
            State == ViewState.Opening ||
            State == ViewState.Closing;

        public event Action<IViewUI> Opened;
        public event Action<IViewUI> Closed;

        protected VisualElement Root { get; private set; }
        protected UIToolkitManager uiToolkitManager;
        protected ModalManager ModalManager => uiToolkitManager.modalManager;
        protected NetworkGameManager GameManager => uiToolkitManager.gameManager;
        protected PlayerManager PlayerManager => GameManager.localPlayer;
        protected bool IsPlayerReady => GameManager && GameManager.localPlayer;

        protected virtual void OnEnable()
        {
            uiToolkitManager = GetComponentInParent<UIToolkitManager>();

            if (uiToolkitManager == null)
            {
                Debug.LogError(
                    $"{GetType().Name} requires a UIToolkitManager in its parent hierarchy.",
                    this);

                return;
            }

            uiToolkitManager.Register(this);
        }

        protected virtual void OnDisable()
        {
            if (uiToolkitManager != null)
                uiToolkitManager.Unregister(this);

            Unbind();

            uiToolkitManager = null;
        }

        private void Bind()
        {
            Element = Root.Q<T>(ElementName);
            if (Element is null)
            {
                Debug.LogError($"Element not found: {ElementName}");
                return;
            }

            Element.RegisterCallback<TransitionEndEvent>(OnTransitionEnd);

            Element.RemoveFromClassList("is-open");
            Element.RemoveFromClassList("is-visible");

            State = ViewState.Closed;

            OnBind();
        }

        private void Unbind()
        {
            OnUnbind();

            if (Element != null)
            {
                Element.UnregisterCallback<TransitionEndEvent>(OnTransitionEnd);
            }

            Element = null;
            Root = null;
        }

        public void Reload(VisualElement root)
        {
            bool wasOpen = IsOpen;

            Unbind();

            Root = root;

            if (Root == null)
                return;

            Bind();

            if (wasOpen)
            {
                RestoreOpenState();
            }
        }

        private void RestoreOpenState()
        {
            Element.AddToClassList("is-visible");
            Element.AddToClassList("is-open");
        }

        public virtual void Open()
        {
            if (Element == null)
                return;

            if (State is ViewState.Open or ViewState.Opening)
                return;

            State = ViewState.Opening;
            // 先让元素参与布局/接收事件
            Element.AddToClassList("is-visible");
            OnOpening();

            // 下一帧再添加 is-open，
            // 让 USS transition 真正产生起始状态 -> 目标状态
            Element.schedule.Execute(() =>
            {
                if (State != ViewState.Opening)
                    return;

                Element.AddToClassList("is-open");
            });
        }

        public virtual void Close()
        {
            if (Element == null)
                return;

            if (State is ViewState.Closed or ViewState.Closing)
                return;

            if (!CanClose)
                return;

            State = ViewState.Closing;
            Element.RemoveFromClassList("is-open");
            OnClosing();
        }

        public virtual void ForceClose()
        {
            if (Element == null)
            {
                State = ViewState.Closed;
                return;
            }

            Element.RemoveFromClassList("is-open");
            Element.RemoveFromClassList("is-visible");

            State = ViewState.Closed;
            OnClosing();
            OnClosed();
            Closed?.Invoke(this);
        }

        private void OnTransitionEnd(TransitionEndEvent evt)
        {
            // 防止子元素的 transition 冒泡影响 Modal 状态
            if (evt.target != Element)
                return;
            /*
            if (evt.stylePropertyNames == null)
                return;
            */
            if (!evt.stylePropertyNames.Contains("opacity"))
                return;

            switch (State)
            {
                case ViewState.Opening:
                    FinishOpen();
                    break;

                case ViewState.Closing:
                    FinishClose();
                    break;
            }
        }

        private void FinishOpen()
        {
            State = ViewState.Open;

            OnOpened();
            Opened?.Invoke(this);
        }

        private void FinishClose()
        {
            Element.RemoveFromClassList("is-visible");

            State = ViewState.Closed;

            OnClosed();
            Closed?.Invoke(this);
        }

        protected virtual void OnBind() { }

        protected virtual void OnUnbind() { }

        protected virtual void OnOpened() { }
        protected virtual void OnOpening() { }
        protected virtual void OnClosed() { }
        protected virtual void OnClosing() { }

        protected TElement Q<TElement>(string name)
            where TElement : VisualElement
        {
            return Root?.Q<TElement>(name);
        }
    }

    public abstract class ViewUI<TElement, TData> : ViewUI<TElement>, IViewUI<TData>
        where TElement : VisualElement
    {
        public abstract void SetData(TData data);
    }
}