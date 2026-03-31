using UnityEngine;
using UnityEngine.SceneManagement;
using PrimeTween;

namespace Tewi.Game.Player.UI
{
    public class PauseManager : MonoBehaviour
    {
        public UIManager uiManager;
        public CanvasGroup canvasGroup;

        void Start()
        {
            canvasGroup.alpha = 0f;
            SetPause(false, false);
        }

        private bool inPauseAnimation = false;
        public void SetPause(bool isPause, bool animate = true)
        {
            if (inPauseAnimation) return;
            inPauseAnimation = true;

            uiManager.PlayerManager.isPaused = isPause;
            float duration = animate ? (isPause ? .25f : .15f) : 0f;

            if (isPause)
            {
                Cursor.lockState = CursorLockMode.None;
                canvasGroup.interactable = true;
                gameObject.SetActive(true);
                uiManager.PlayerManager.character.SetMovementDirection(Vector3.zero);
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
                Cursor.lockState = CursorLockMode.Locked;
                transform.localScale = Vector3.one;
                Sequence.Create()
                    .Group(Tween.Scale(transform, new Vector3(1.15f, 1.15f, 1.15f), duration))
                    .Group(Tween.Custom(1f, 0f, duration, newVal => canvasGroup.alpha = newVal)).OnComplete(() =>
                    {
                        inPauseAnimation = false;
                        canvasGroup.interactable = false;
                        gameObject.SetActive(false);
                    });
            }
        }

        public void OnContinueButtonClock()
        {
            SetPause(false);
        }

        public void OnExitButtonClick()
        {
            SceneManager.LoadSceneAsync(0);
        }
    }
}