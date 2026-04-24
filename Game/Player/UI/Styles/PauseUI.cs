using UnityEngine;
using UnityEngine.SceneManagement;
using PrimeTween;

namespace Tewi.Game.Player.UI.Styles
{
    internal class PauseUI : UIBase<object>
    {
        public override bool IsModal => true;

        internal override void OnOpen(object context)
        {
            SetPause(true);
        }

        internal override void OnClose()
        {
            SetPause(false);
        }

        private bool inPauseAnimation = false;
        public void SetPause(bool isPause, bool animate = true)
        {
            inPauseAnimation = true;

            float duration = animate ? (isPause ? .25f : .15f) : 0f;

            if (isPause)
            {
                canvasGroup.interactable = true;
                SetActive(true);

                transform.localScale = new Vector3(1.15f, 1.15f, 1.15f);
                Sequence.Create()
                    .Group(Tween.Scale(transform, Vector3.one, duration))
                    .Group(Tween.Custom(0f, 1f, duration, newVal => canvasGroup.alpha = newVal)).OnComplete(() =>
                    {
                        inPauseAnimation = false;
                    });
            }
            else
            {
                canvasGroup.interactable = false;
                transform.localScale = Vector3.one;
                Sequence.Create()
                    .Group(Tween.Scale(transform, new Vector3(1.15f, 1.15f, 1.15f), duration))
                    .Group(Tween.Custom(1f, 0f, duration, newVal => canvasGroup.alpha = newVal)).OnComplete(() =>
                    {
                        inPauseAnimation = false;
                        canvasGroup.interactable = false;
                        SetActive(false);
                    });
            }
        }

        public void OnContinueButtonClock()
        {
            Close();
        }

        public void OnExitButtonClick()
        {
            SceneManager.LoadSceneAsync(0);
        }
    }
}