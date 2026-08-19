using UnityEngine;
using UnityEngine.SceneManagement;
using PrimeTween;

namespace Tewi.Game.UI.Styles
{
    internal class PauseUI : UIBase<object>
    {
        public override bool IsModal => true;

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