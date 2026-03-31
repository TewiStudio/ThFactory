using UnityEngine;

namespace Tewi.Game.Player.Cameras
{
    public class ItemViewLag : MonoBehaviour
    {
        public PlayerCamera playerCamera;

        [Header("Angular Speed (deg/sec)")]
        public float slowSpeed = 20f;  // 慢速转头阈值
        public float fastSpeed = 180f; // 快速转头阈值

        [Header("Follow Speed")]
        public float minFollow = 6f;   // 慢速转头时缓动
        public float maxFollow = 40f;  // 快速转头时跟随速度

        private Quaternion lastCameraRot;
        private Quaternion lagOffset = Quaternion.identity;

        void Start()
        {/*
            lastCameraRot = playerCamera.camera.transform.rotation;
            transform.rotation = playerCamera.camera.transform.rotation;*/
        }

        void LateUpdate()
        {
            if (playerCamera.camera == null) return;
            Quaternion camRot = playerCamera.camera.transform.rotation;

            //计算相机角速度
            Quaternion delta = camRot * Quaternion.Inverse(lastCameraRot);
            delta.ToAngleAxis(out float angle, out _);
            float angularSpeed = angle / Mathf.Max(Time.deltaTime, 0.0001f);

            lastCameraRot = camRot;

            // 将角速度映射到 [0,1]
            float t = Mathf.InverseLerp(slowSpeed, fastSpeed, angularSpeed);

            // 转头越快 → followSpeed 越大
            float followSpeed = Mathf.Lerp(minFollow, maxFollow, t);

            // 指数平滑应用到 lagOffset
            float slerpT = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
            lagOffset = Quaternion.Slerp(lagOffset, Quaternion.identity, slerpT);

            // 最终旋转 = 相机旋转 + 偏移
            transform.rotation = camRot * lagOffset;
        }

        // 如果想在外部主动施加旋转偏移
        public void AddRotationLag(Quaternion cameraDelta)
        {
            lagOffset = Quaternion.Inverse(cameraDelta) * lagOffset;
        }
    }
}
