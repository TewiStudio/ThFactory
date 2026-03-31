using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Tewi.Game.Home;
using Tewi.Game.Audio;
using Tewi.Game.Worlds;
using Tewi.Helpers;
using Tewi.Helpers.Extensions;
using PrimeTween;

namespace Tewi.Game
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [ReadOnly] public bool inWorld = false;
        [ReadOnly] public bool inHomeStart = false;
        public float timeScale = 1f;
        public int playerCount = 1;
        public AudioManager audioManager;
        public Transform characterDefaultParent;
        public Transform movingPlayerGroundsParent;
        public Volume defaultVolume;

        [Space(10)]
        public HomeManager homeManager;
        public Vector3 finalHomePosition = new(0, 900, 0);

        [Space(10)]
        [ReadOnly] public WorldManager nowWorld;
        public string goWorld = "TestWorld";

        private Sequence homeAnimate;

        public Vector3 HomePositionInWorld => nowWorld.HomePosition
            .SetY(nowWorld.HomePosition.y + 100) +
            (Vector3.forward * 100).RotateY(nowWorld.HomeRotation.eulerAngles.y);

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
        }

        void Update()
        {
            Time.timeScale = timeScale;
        }

        public void StartWorld()
        {
            if (inHomeStart) return;
            inHomeStart = true;
            if (!inWorld) // 进入世界
            {
                //homeRoot.position = Vector3.zero;
                StartCoroutine(LoadScene());
            }
            else // 离开世界
            {
                //homeRoot.position = new Vector3(0, 100, 100);
                StartCoroutine(UnloadScene());
            }
        }

        /// <summary>
        /// 进入世界时调用
        /// </summary>
        private void OnEnterWorld()
        {/*
            defaultVolume.gameObject.SetActive(true);
            nowWorld = GameObject.Find("World Manager").GetComponent<WorldManager>();
            nowWorld.gameManager = this;
            homeManager.HomeLand(HomePositionInWorld, nowWorld.HomePosition, nowWorld.HomeRotation.eulerAngles, () =>
            {
                inHomeStart = false;
            });

            nowWorld.OnWorldPrepared();*/
        }

        /// <summary>
        /// 正在离开世界时调用
        /// </summary>
        private void OnLeavingWorld()
        {/*
            var temp = nowWorld.gameObject.scene.name;
            nowWorld.OnWorldLeaving();
            homeManager.HomeLeave(finalHomePosition, HomePositionInWorld, nowWorld.HomeRotation.eulerAngles, () =>
            {
                inWorld = false;
                inHomeStart = false;
                unloadWorld = SceneManager.UnloadSceneAsync(temp);
            });*/
        }

        /// <summary>
        /// 已离开世界时调用
        /// </summary>
        private void OnLeavedWorld()
        {
            nowWorld = null;
            defaultVolume.gameObject.SetActive(true);
        }

        /// <summary>
        /// 加载场景时调用，加载完成时会调用 <see cref="OnEnterWorld"/>
        /// </summary>
        /// <returns>协程</returns>
        private IEnumerator LoadScene()
        {
            inWorld = true;
            if (!Application.CanStreamedLevelBeLoaded(goWorld))
            {
                inWorld = false;
                inHomeStart = false;
                Debug.LogWarning($"Not found scene by: {goWorld}");
                yield break;
            }
            var asyncLoad = SceneManager.LoadSceneAsync(goWorld, LoadSceneMode.Additive);
            asyncLoad.allowSceneActivation = false;

            // 显示加载进度（0~0.9）
            while (!asyncLoad.isDone)
            {
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                Debug.Log("Loading progress: " + (progress * 100) + "%");

                if (asyncLoad.progress >= 0.9f)
                {
                    // 完成加载后激活场景
                    asyncLoad.allowSceneActivation = true;
                }
                yield return null;
            }
            //yield return new WaitForSeconds(1.5f); // 等待场景稳定
            OnEnterWorld();
        }

        private Sequence leavingAnimation;
        private AsyncOperation unloadWorld;
        /// <summary>
        /// 卸载场景时调用，卸载时会调用 <see cref="OnLeavingWorld"/> 以加载飞船启动动画。动画完成后会卸载场景，卸载完成后调用 <see cref="OnLeavedWorld"/>
        /// </summary>
        /// <returns>协程</returns>
        private IEnumerator UnloadScene()
        {
            OnLeavingWorld();

            yield return new WaitUntil(() => !leavingAnimation.isAlive);
            while (!unloadWorld.isDone) yield return null;

            OnLeavedWorld();
        }
    }
}