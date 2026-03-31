using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using Animancer;

namespace Tewi.Game.Player.Body
{
    public class AnimationIKFollowLookAt : NetworkBehaviour
    {
        [Header("References")]
        public BodyManager bodyManager;
        public AnimancerComponent animancerComponent;
        public Animator animator;

        [Header("Settings")]
        public float lerpSpeed = 15f;
        public float lookDistance = 20f;
        public float sendThreshold = 1.0f;


        private readonly SyncVar<float> _syncedPitch = new();

        // 用于显示的平滑角度
        private float _currentDisplayPitch;
        private float _lastSentPitch;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            // 开启 Animancer 的 IK
            animancerComponent.Layers[0].ApplyAnimatorIK = true;
        }

        private void Update()
        {
            if (IsOwner)
            {
                HandleOwnerLogic();
            }
            else
            {
                HandleObserverLogic();
            }
        }

        private void HandleOwnerLogic()
        {
            // 1. 获取本地相机的 Pitch (上下角度)
            // 假设你的相机挂在 head 下面，或者你有办法获取相机的 x 轴旋转
            // 这里通常需要根据你的相机脚本来获取。
            // 简单的方法是读取相机的 localEulerAngles.x，并将其转换为 -180 到 180 的角度
            float rawPitch = bodyManager.playerManager.head.eulerAngles.x;

            // 规范化角度到 -180 ~ 180 (处理 360 度回绕)
            if (rawPitch > 180) rawPitch -= 360;

            // Owner 直接设置显示值，无需插值，保证本地响应最快
            _currentDisplayPitch = rawPitch;

            // 2. 只有变化足够大时才发送给服务器 (带宽优化)
            if (Mathf.Abs(rawPitch - _lastSentPitch) > sendThreshold)
            {
                ServerSetPitch(rawPitch);
                _lastSentPitch = rawPitch;
            }
        }

        private void HandleObserverLogic()
        {
            // 其他玩家：将当前显示角度平滑过渡到 SyncVar 接收到的角度
            _currentDisplayPitch = Mathf.Lerp(_currentDisplayPitch, _syncedPitch.Value, Time.deltaTime * lerpSpeed);
        }

        [ServerRpc]
        private void ServerSetPitch(float pitch)
        {
            // 服务器更新 SyncVar，会自动分发给其他客户端
            _syncedPitch.Value = pitch;
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (!animator) return;

            // 设置权重
            animator.SetLookAtWeight(0.6f, 0.2f, 0.8f, 0f);

            // -----------------------------------------------------------
            // 核心计算逻辑：
            // 我们已知：
            // 1. transform.forward (身体朝向，由 NetworkTransform 同步)
            // 2. transform.right (身体右侧，用于作为旋转轴)
            // 3. _currentDisplayPitch (上下看的角度)
            // -----------------------------------------------------------

            // 计算旋转：以身体的右方为轴，上下旋转 Pitch 角度
            // 注意：正 Pitch 通常是向下看还是向上看取决于你的相机设置
            // 通常 Unity 中 x 轴正方向是向下转(Euler)，负是向上。请根据实际情况调整符号 (-_currentDisplayPitch)
            Quaternion pitchRotation = Quaternion.AngleAxis(_currentDisplayPitch, transform.right);

            // 计算最终视线方向：将身体的前方 施加 上下的旋转
            Vector3 finalLookDir = pitchRotation * transform.forward;

            // 计算最终目标点
            Vector3 targetPos = bodyManager.playerManager.head.position + (finalLookDir * lookDistance);

            // 调试用 (可选)
            // Debug.DrawLine(headTransform.position, targetPos, Color.red);

            animator.SetLookAtPosition(targetPos);
        }
    }
}