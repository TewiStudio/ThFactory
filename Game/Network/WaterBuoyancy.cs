using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using FishNet.Object;

namespace Tewi.Game.Network
{
    [RequireComponent(typeof(Rigidbody))]
    public class HDRPWaterBuoyancy : NetworkBehaviour
    {
        [Header("水面引用")]
        public WaterSurface waterSurface;

        [Header("浮力设置")]
        [Tooltip("漂浮力作用点（建议在物体的底部四周创建 4 个子物体并拖入此处）")]
        public Transform[] floaters;

        [Tooltip("完全浸没时的深度阈值。值越大，过渡越平缓，越不容易抖动。推荐设在 1.0 ~ 2.0 之间")]
        public float depthBeforeSubmerged = 1.5f;

        [Tooltip("浮力强度系数（通常设为 1.2 到 2.0 左右）")]
        public float displacementAmount = 1.8f;

        [Header("阻尼设置（使用刚体内置属性，极其稳定）")]
        [Tooltip("在水中的空气阻力（数值越大，上下弹跳平息得越快，推荐 2 ~ 5）")]
        public float waterDrag = 3.0f;
        [Tooltip("在水中的旋转阻力（防止物体在水面疯狂旋转，推荐 2 ~ 4）")]
        public float waterAngularDrag = 2.0f;

        private Rigidbody rb;
        private WaterSearchParameters searchParameters = new WaterSearchParameters();
        private WaterSearchResult searchResult = new WaterSearchResult();

        // 记录物体原本在空气中的阻尼
        private float originalDrag;
        private float originalAngularDrag;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = true;

            // 1. 强制开启插值，消除由于渲染与物理帧不同步导致的“视觉抖动”
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // 2. 记录物体的初始阻尼
            originalDrag = rb.linearDamping;
            originalAngularDrag = rb.angularDamping;
        }

        void FixedUpdate()
        {
            if (!IsServerStarted || waterSurface == null || floaters == null || floaters.Length == 0) return;

            int submergedPointsCount = 0;
            float totalDisplacement = 0f;

            foreach (Transform floater in floaters)
            {
                if (floater == null) continue;

                // 设置 HDRP 水高度查询参数
                searchParameters.startPositionWS = searchResult.candidateLocationWS;
                searchParameters.targetPositionWS = floater.position;
                searchParameters.error = 0.01f;
                searchParameters.maxIterations = 8;

                if (waterSurface.ProjectPointOnWaterSurface(searchParameters, out searchResult))
                {
                    float waterHeight = searchResult.projectedPositionWS.y;

                    if (floater.position.y < waterHeight)
                    {
                        submergedPointsCount++;

                        // 计算该点的浸没比例 (0 到 1 之间)
                        float displacementMultiplier = Mathf.Clamp01((waterHeight - floater.position.y) / depthBeforeSubmerged);
                        totalDisplacement += displacementMultiplier;

                        // 计算该浮点应施加的浮力（使用 ForceMode.Acceleration 忽略质量影响）
                        float buoyancyForceY = Mathf.Abs(Physics.gravity.y) * displacementMultiplier * displacementAmount / floaters.Length;
                        Vector3 buoyancyForce = new Vector3(0f, buoyancyForceY, 0f);

                        // 在该浮点位置施加向上的力（这会自动产生自然的物理旋转力矩）
                        rb.AddForceAtPosition(buoyancyForce, floater.position, ForceMode.Acceleration);
                    }
                }
            }

            // 3. 根据所有浮点的平均浸没程度，平滑调节 Rigidbody 自身的内置阻尼
            // 这种方式利用了物理引擎底层的阻尼计算，完全避免了手动受力过冲产生的物理抖动
            if (submergedPointsCount > 0)
            {
                float averageDisplacement = totalDisplacement / floaters.Length;

                rb.linearDamping = Mathf.Lerp(originalDrag, waterDrag, averageDisplacement);
                rb.angularDamping = Mathf.Lerp(originalAngularDrag, waterAngularDrag, averageDisplacement);
            }
            else
            {
                // 完全离开水面时，平滑恢复空气中的初始阻尼
                rb.linearDamping = originalDrag;
                rb.angularDamping = originalAngularDrag;
            }
        }
    }
}
