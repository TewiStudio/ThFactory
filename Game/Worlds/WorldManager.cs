using System;
using System.Collections;
using Tewi.Helpers;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace Tewi.Game.Worlds
{
    public class WorldManager : MonoBehaviour
    {
        public GameManager gameManager;
        public WorldTimeManager worldTimeManager;

        [Header("World Settings")]
        public string worldName = "Test world";
        public string worldRank = "Test";
        public string worldDescribe = "A test world";
        public Transform worldPositionTransform;
        public Transform DungeonStartPosition;

        public DungeonGenerator DungeonGenerator { get; private set; }

        public Vector3 HomePosition => worldPositionTransform ? worldPositionTransform.position : Vector3.zero;
        public Quaternion HomeRotation => worldPositionTransform ? worldPositionTransform.rotation : Quaternion.identity;

        public virtual void OnWorldStart()
        {
            Debug.Log($"{worldName} start.");

            worldTimeManager.OnWorldStart();
            DungeonGenerator = new DungeonGenerator() { WorldManager = this };
            DungeonGenerator.StartGenerateDungeon();
        }

        public virtual void OnWorldPrepared()
        {
            Debug.Log($"{worldName} prepared.");
        }

        public virtual void OnWorldLeaving()
        {
            Debug.Log($"{worldName} leaving.");
        }

        public virtual void OnWorldDestroy()
        {
            Debug.Log($"{worldName} destroy.");
        }

        private void Start()
        {
            OnWorldStart();
        }

        private void OnDestroy()
        {
            OnWorldDestroy();
        }

        private void FixedUpdate()
        {
        }

/*
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void AutoBootstrap()
        {
            var active = SceneManager.GetActiveScene();
            if (active.name is "TestRoom")
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
                SceneManager.LoadScene("GameScene", LoadSceneMode.Additive);
            }
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "GameScene") return;

            SceneManager.sceneLoaded -= OnSceneLoaded;

            var gameManager = scene.GetRootGameObjects()[0].GetComponent<GameManager>();
            gameManager.nowWorld = GameObject.Find("World Manager").GetComponent<WorldManager>();
            gameManager.nowWorld.gameManager = gameManager;
            gameManager.nowWorld.StartCoroutine(gameManager.nowWorld.WaitHomeManagerLoaded());
            gameManager.inWorld = true;
        }

        public IEnumerator WaitHomeManagerLoaded()
        {
            yield return new WaitUntil(() => gameManager.homeManager != null && gameManager.homeManager.isActiveAndEnabled);
            gameManager.homeManager.SetDoorOpen(false);
            gameManager.homeManager.HomeLand(gameManager.HomePositionInWorld, gameManager.nowWorld.HomePosition, gameManager.nowWorld.HomeRotation.eulerAngles, () => { }, true);
        }*/
    }

    public class DungeonGenerator
    {
        public WorldManager WorldManager;

        public void StartGenerateDungeon()
        {
            //WorldManager.DungeonStartPosition
        }
    }
}