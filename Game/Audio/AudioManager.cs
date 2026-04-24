using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Tewi.Game.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public AudioMixer audioMixer;

        [Header("Pooling")]
        public GameObject sfxPrefab;
        public int poolSize = 20;
        private Queue<AudioSource> sfxPool;

        [Header("Ambient")]
        public AudioSource ambientSource;

        [Header("Music")]
        public AudioSource musicSource;

        private void Awake()
        {
            InitializePool();
        }

        public void PlaySFX(SoundData data, Vector3 position = default)
        {
            if (data == null) return;

            AudioSource source = GetFromPool();

            // 配置 Source
            source.transform.position = position;
            source.clip = data.GetClip();
            source.outputAudioMixerGroup = data.outputGroup;
            source.volume = data.volume;
            source.pitch = data.pitch * Random.Range(0.95f, 1.05f); // 增加一点微小的随机音调，听起来更自然
            source.loop = data.loop;
            source.spatialBlend = data.spatialBlend;

            source.gameObject.SetActive(true);
            source.Play();

            // 如果不是循环音效，播放完自动回收
            if (!data.loop)
            {
                StartCoroutine(ReturnToPool(source, source.clip.length));
            }
        }

        private void InitializePool()
        {
            sfxPool = new Queue<AudioSource>();
            GameObject poolRoot = new GameObject("SFX_Pool");
            poolRoot.transform.SetParent(this.transform);

            for (int i = 0; i < poolSize; i++)
            {
                GameObject obj = Instantiate(sfxPrefab, poolRoot.transform);
                AudioSource source = obj.GetComponent<AudioSource>();
                obj.SetActive(false);
                sfxPool.Enqueue(source);
            }
        }

        private AudioSource GetFromPool()
        {
            if (sfxPool.Count > 0)
            {
                return sfxPool.Dequeue();
            }
            // 池子空了，临时创建一个（或者动态扩容）
            GameObject obj = Instantiate(sfxPrefab, transform);
            return obj.GetComponent<AudioSource>();
        }

        private IEnumerator ReturnToPool(AudioSource source, float delay)
        {
            yield return new WaitForSeconds(delay);
            source.Stop();
            source.gameObject.SetActive(false);
            sfxPool.Enqueue(source);
        }
    }
}