using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Tewi.Game.UI
{
    [ExecuteAlways]
    public class SceneColorCapture : MonoBehaviour
    {
        public Material blurMaterial;
        public RenderTexture colorCopy;
        public RenderTexture rtA;
        public RenderTexture rtB;
        public int downsampleScale;

        public Color fallbackColor = new Color(0f, 0f, 0f, 0.4f);
        private Texture2D fallbackTex;

        private void OnValidate()
        {
            CreateFallbackTexture();
        }

        private void OnEnable()
        {
            CreateFallbackTexture();
        }

        private void OnDisable()
        {
            if (fallbackTex != null)
            {
                DestroyImmediate(fallbackTex);
                fallbackTex = null;
            }
        }

        private void LateUpdate()
        {
            if (Camera.main == null || colorCopy == null)
            {
                if (fallbackTex == null) CreateFallbackTexture();
                Shader.SetGlobalTexture("_BlurTex", fallbackTex);
                return;
            }

            if (blurMaterial == null || colorCopy == null || rtA == null || rtB == null) return;
            UpdateBlur(colorCopy);
        }

        void CreateFallbackTexture()
        {
            if (fallbackTex == null)
            {
                fallbackTex = new Texture2D(1, 1);
                fallbackTex.name = "BlurFallbackTex";
            }
            fallbackTex.SetPixel(0, 0, fallbackColor);
            fallbackTex.Apply();
        }

        void UpdateBlur(RenderTexture source)
        {
            float sourceAspect = source.width / (float)source.height;
            float rtAspect = rtA.width / (float)rtA.height;

            if (!Mathf.Approximately(sourceAspect, rtAspect))
            {
                int scale = Mathf.Max(1, downsampleScale);
                int downsampledW = Screen.width / scale;
                int downsampledH = Screen.height / scale;

                rtA.Release();
                rtB.Release();
                rtA.width = downsampledW;
                rtA.height = downsampledH;
                rtB.width = downsampledW;
                rtB.height = downsampledH;
                rtA.Create();
                rtB.Create();
            }

            Graphics.Blit(source, rtA);
            // Horizontal
            Graphics.Blit(rtA, rtB, blurMaterial, 0);
            // Vertical
            Graphics.Blit(rtB, rtA, blurMaterial, 1);

            Shader.SetGlobalTexture("_BlurTex", rtA);
        }
    }
}
