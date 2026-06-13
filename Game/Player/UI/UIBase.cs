using PrimeTween;
using Tewi.Game.Network;
using UnityEngine;

namespace Tewi.Game.Player.UI
{
    public interface IUIBase
    {
        bool IsOpen { get; }
        bool IsModal { get; }
        bool CanClose { get; }

        void Close();
        void SetActive(bool active, bool animation);
    }

    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIBase<T> : MonoBehaviour, IUIBase
    {
        [SerializeField] protected UIManager uiManager;
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] private bool openOnStart = false;

        protected PlayerManager playerManager => uiManager.playerManager;
        protected NetworkGameManager gameManager => playerManager.gameManager;

        public virtual bool IsModal => true;
        public virtual bool DefaultAnimateActiveSwitch => true;

        public bool IsOpen => gameObject.activeSelf;
        public virtual bool CanClose => true;

        internal virtual void OnOpen(T context) => SetActive(true, DefaultAnimateActiveSwitch);
        internal virtual void OnClose() => SetActive(false, DefaultAnimateActiveSwitch);

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

        public void Close()
        {
            if (CanClose)
            {
                OnClose();
                uiManager.NotifyClosed(this);
            }
        }

        private void OnValidate()
        {
            GetStartComponents();
        }

        protected virtual void Awake()
        {
            GetStartComponents();
            SetActive(openOnStart, false);
        }

        protected virtual void GetStartComponents()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (uiManager == null)
                uiManager = GetComponentInParent<UIManager>();
        }
    }
}
