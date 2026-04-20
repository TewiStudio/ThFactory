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


        private readonly SyncVar<float> _syncedPitch = new(new(channel: FishNet.Transporting.Channel.Unreliable));

        // 用于显示的平滑角度
        private float _currentDisplayPitch;
        private float _lastSentPitch;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();

            _syncedPitch.UpdateSendRate(1f / TimeManager.TickRate);

            // 开启 Animancer 的 IK
            animancerComponent.Layers[0].ApplyAnimatorIK = true;
        }

        private void Update()
        {
            if (!IsOwner)
            {
                HandleObserverLogic();
            }
            else
            {
                HandleOwnerLogic();
            }
        }

        private void HandleOwnerLogic()
        {
            // 获取本地相机的 Pitch (上下角度)
            // 简单的方法是读取相机的 localEulerAngles.x，并将其转换为 -180 到 180 的角度
            float rawPitch = bodyManager.playerManager.head.eulerAngles.x;

            // 规范化角度到 -180 ~ 180
            if (rawPitch > 180) rawPitch -= 360;

            // Owner 直接设置显示值，无需插值，保证本地响应最快
            _currentDisplayPitch = rawPitch;

            // 只有变化足够大时才发送给服务器 (带宽优化)
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
            //_currentDisplayPitch = pitch;
        }

        [ServerRpc]
        private void ServerSetPitch(float pitch)
        {
            _syncedPitch.Value = pitch;
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (!animator) return;

            animator.SetLookAtWeight(0.6f, 0.2f, 0.8f, 0f);

            Quaternion pitchRotation = Quaternion.AngleAxis(_currentDisplayPitch, transform.right);

            Vector3 finalLookDir = pitchRotation * transform.forward;

            Vector3 targetPos = bodyManager.playerManager.head.position + (finalLookDir * lookDistance);

            // Debug.DrawLine(headTransform.position, targetPos, Color.red);

            animator.SetLookAtPosition(targetPos);
        }
    }
}