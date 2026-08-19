using PrimeTween;
using System;
using Tewi.Game.Network;
using Tewi.Game.Player;
using UnityEngine;

namespace Tewi.Game.UI
{
    public interface IUIBase
    {
        Type UIType { get; }
        bool IsOpen { get; }
        KeyCode ModalHotKey { get; }
        bool IsModal { get; }
        bool CanClose { get; }

        void Close();
        void Open(object context);
        void SetActive(bool active, bool animation);
        bool HandleInput(KeyCode key);

        void Init();
        void Deinit();
        void PlayerAwake();
        void PlayerDestory();
    }

    [RequireComponent(typeof(CanvasGroup), typeof(RectTransform))]
    public abstract class UIBase<T> : MonoBehaviour, IUIBase
    {
        [SerializeField] protected UIManager uiManager;
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] private bool openOnStart = false;
        [NonSerialized] public RectTransform rectTransform;

        public Type UIType => GetType();
        protected PlayerManager playerManager => uiManager.playerManager;
        protected NetworkGameManager gameManager => uiManager.gameManager;

        public virtual bool IsModal => true;
        public virtual KeyCode ModalHotKey => KeyCode.None;
        public virtual bool DefaultActiveSwitchAnimation => true;

        public bool IsPlayerReady => uiManager.IsPlayerReady;
        public bool IsOpen => gameObject.activeSelf;
        public virtual bool CanClose => true;

        internal virtual void OnOpen(T context) => SetActive(true, DefaultActiveSwitchAnimation);
        internal virtual void OnClose() => SetActive(false, DefaultActiveSwitchAnimation);

        private bool _targetActive;
        public virtual void SetActive(bool active, bool animation = true)
        {
            _targetActive = active;
            float duration = animation ? (active ? .25f : .15f) : 0f;

            if (active)
            {
                canvasGroup.interactable = true;
                gameObject.SetActive(true);

                Sequence.Create()
                    .Group(Tween.Scale(transform, Vector3.one, duration))
                    .Group(Tween.Custom(canvasGroup.alpha, 1f, duration, newVal => canvasGroup.alpha = newVal));
            }
            else
            {
                canvasGroup.interactable = false;
                Sequence.Create()
                    .Group(Tween.Scale(transform, new Vector3(1.15f, 1.15f, 1.15f), duration))
                    .Group(Tween.Custom(canvasGroup.alpha, 0f, duration, newVal => canvasGroup.alpha = newVal)).OnComplete(() =>
                    {
                        if (!_targetActive)
                        {
                            canvasGroup.interactable = false;
                            gameObject.SetActive(false);
                        }
                    });
            }
        }

        void IUIBase.Open(object context)
        {
            OnOpen((T)context);
        }

        public void Close()
        {
            if (CanClose)
            {
                OnClose();
                uiManager.NotifyClosed(this);
            }
        }

        public virtual bool HandleInput(KeyCode key) => false;

        public void SetTemporaryModal()
        {
            uiManager.AddToModalStack(this);
            playerManager.isModalUIOpened = true;
        }

        private void OnValidate()
        {
            GetStartComponents();
        }

        public virtual void Init()
        {
            GetStartComponents();
            SetActive(false, false);
        }

        public virtual void Deinit()
        {
            SetActive(false, false);
        }

        public virtual void PlayerAwake() => SetActive(openOnStart, false);
        
        public virtual void PlayerDestory() => SetActive(false, false);

        protected virtual void GetStartComponents()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (uiManager == null)
                uiManager = GetComponentInParent<UIManager>();
        }
    }
}
