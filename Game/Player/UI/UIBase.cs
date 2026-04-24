using Tewi.Game.Network;
using UnityEngine;

namespace Tewi.Game.Player.UI
{
    public interface IUIBase
    {
        bool IsOpen { get; }
        bool IsModal { get; }
        bool CanClose();
        void Close();
        void SetActive(bool active);
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
        public bool IsOpen => gameObject.activeSelf;

        internal virtual void OnOpen(T context) => SetActive(true);
        internal virtual void OnClose() => SetActive(false);
        public virtual bool CanClose() => true;

        public void SetActive(bool active) => gameObject.SetActive(active);

        public void Close()
        {
            if (CanClose())
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
            SetActive(openOnStart);
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
