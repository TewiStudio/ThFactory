using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tewi.MainMenu
{
    public class MainMenuManager : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        public void OnSinglePlayerClick()
        {
            SceneManager.LoadSceneAsync(1);
        }
    }
}