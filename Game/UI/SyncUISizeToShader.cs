using UnityEngine;
using UnityEngine.UI;

namespace Tewi.Game.UI
{
    [ExecuteAlways]
    [RequireComponent(typeof(RawImage))]
    public class SyncUISizeToShader : MonoBehaviour, IMaterialModifier
    {
        [Header("Liquid Glass Settings")]
        [Range(0f, 150f)] public float cornerRadius = 32f;          // 对应 _Radius
        [Range(1f, 100f)] public float bevelWidth = 27f;            // 对应 _BevelWidth
        [Range(0f, 5f)] public float bevelHeight = 4.05f;            // 对应 _BevelHeight
        [Range(-0.1f, 0.1f)] public float refractionStrength = -.1f; // 对应 _Refraction

        private RawImage m_Image;
        private Material m_BaseMaterialUsed;
        private Material m_InstancedMaterial;

        // Shader 属性 ID 缓存（避免每帧进行字符串寻址，提升性能）
        private static readonly int SizeID = Shader.PropertyToID("_Size");
        private static readonly int RadiusID = Shader.PropertyToID("_Radius");
        private static readonly int BevelWidthID = Shader.PropertyToID("_BevelWidth");
        private static readonly int BevelHeightID = Shader.PropertyToID("_BevelHeight");
        private static readonly int RefractionID = Shader.PropertyToID("_Refraction");

        private void OnEnable()
        {
            m_Image = GetComponent<RawImage>();
            if (m_Image != null) m_Image.SetMaterialDirty();
        }

        private void OnDisable()
        {
            CleanUpMaterial();
            if (m_Image != null) m_Image.SetMaterialDirty();
        }

        private void OnDestroy()
        {
            CleanUpMaterial();
        }

        private void OnValidate()
        {
            // 在 Inspector 调节任意滑条时，立即通知 UI 重新获取并刷新材质
            if (m_Image != null) m_Image.SetMaterialDirty();
        }

        private void Update()
        {
            if (IsAsset()) return;

            // 保持每一帧在 UI 大小自适应改变时，动态同步参数给材质
            if (m_Image != null) m_Image.SetMaterialDirty();
        }

        public Material GetModifiedMaterial(Material baseMaterial)
        {
            if (IsAsset() || baseMaterial == null) return baseMaterial;

            // 如果没有私有克隆体，或者面板挂载的材质改变了，则进行克隆
            if (m_InstancedMaterial == null || m_BaseMaterialUsed != baseMaterial)
            {
                CleanUpMaterial();
                m_BaseMaterialUsed = baseMaterial;
                m_InstancedMaterial = Instantiate(baseMaterial);
                m_InstancedMaterial.hideFlags = HideFlags.HideAndDontSave; // 阻止存盘，彻底避免 Undo 丢失 Bug
            }

            // 动态向私有材质同步 C# 暴露出来的所有参数
            UpdateShaderProperties(m_InstancedMaterial);

            return m_InstancedMaterial;
        }

        void UpdateShaderProperties(Material mat)
        {
            if (mat == null) return;

            RectTransform rectTransform = m_Image.rectTransform;
            if (rectTransform != null)
            {
                // 1. 同步物理大小（宽高）
                Vector2 size = rectTransform.rect.size;
                if (mat.HasProperty(SizeID))
                {
                    mat.SetVector(SizeID, new Vector4(size.x, size.y, 0, 0));
                }

                // 2. 同步圆角半径
                if (mat.HasProperty(RadiusID))
                {
                    mat.SetFloat(RadiusID, cornerRadius);
                }

                // 3. 同步边缘高度/宽度参数
                if (mat.HasProperty(BevelWidthID))
                {
                    mat.SetFloat(BevelWidthID, bevelWidth);
                }

                if (mat.HasProperty(BevelHeightID))
                {
                    mat.SetFloat(BevelHeightID, bevelHeight);
                }

                // 4. 同步折射强度
                if (mat.HasProperty(RefractionID))
                {
                    mat.SetFloat(RefractionID, refractionStrength);
                }
            }
        }

        private void CleanUpMaterial()
        {
            if (m_InstancedMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(m_InstancedMaterial);
                }
                else
                {
                    DestroyImmediate(m_InstancedMaterial);
                }
                m_InstancedMaterial = null;
                m_BaseMaterialUsed = null;
            }
        }

        private bool IsAsset()
        {
#if UNITY_EDITOR
            return UnityEditor.EditorUtility.IsPersistent(gameObject);
#else
            return false;
#endif
        }
    }
}