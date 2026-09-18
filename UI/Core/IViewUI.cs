using System;
using UnityEngine.UIElements;

namespace Tewi.Game.UI.Core
{
    public interface IViewUI
    {
        ViewState State { get; }

        bool IsModal { get; }
        bool IsOpen { get; }
        bool IsTransitioning { get; }
        bool CanClose { get; }
        bool OpenOnStart { get; }

        event Action<IViewUI> Opened;
        event Action<IViewUI> Closed;

        void Open();
        void Close();
        void ForceClose();

        void Reload(VisualElement root);
    }

    public interface IViewUI<TData> : IViewUI
    {
        public void SetData(TData data);
    }
}