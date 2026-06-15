using FishNet.Object;
using System.Collections;
using UnityEngine;

namespace Tewi.Game.Player.Cameras
{
    public class SmoothCharacterGrahpics : NetworkBehaviour
    {
        public PlayerManager playerManager;
        public Transform positionTarget;
        public Transform smoothTarget;
        public bool enableRotationSmoothing = true;

        // 当前的参考空间。站立在移动平台上时为平台Transform，在普通地面时为 null，代表世界空间
        private Transform _currentReference;
        private Transform _designParent;

        // 在参考空间下的上一物理帧和当前物理帧的相对位置与旋转
        private Vector3 _prevRelativePos;
        private Vector3 _currentRelativePos;
        private Quaternion _prevRelativeRot;
        private Quaternion _currentRelativeRot;

        private void Start()
        {
            if (positionTarget != null)
            {
                _currentReference = null;
                ResetState(positionTarget.position, positionTarget.rotation);
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (smoothTarget.parent != null)
            {
                _designParent = smoothTarget.parent;
                smoothTarget.transform.SetParent(null);
            }
            StopAllCoroutines();
            StartCoroutine(PostPhysicsSyncLoop());
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            if (smoothTarget.parent == null)
            {
                smoothTarget.transform.SetParent(_designParent);
            }
            StopAllCoroutines();
        }

        private void ResetState(Vector3 pos, Quaternion rot)
        {
            _prevRelativePos = pos;
            _currentRelativePos = pos;
            _prevRelativeRot = rot;
            _currentRelativeRot = rot;
        }

        private IEnumerator PostPhysicsSyncLoop()
        {
            WaitForFixedUpdate waitForFixedUpdate = new();
            while (true)
            {
                yield return waitForFixedUpdate;

                if (IsClientStarted)
                {
                    Transform currentPlatform = null;

                    // 获取移动平台的 Transform
                    Rigidbody groundRigidbody = null;
                    if (IsOwner) groundRigidbody = playerManager.groundDetect._lastGroundRigidbody;
                    else groundRigidbody = playerManager.groundDetect.currentGroundRigidbody;
                    if (groundRigidbody != null)
                    {
                        currentPlatform = groundRigidbody.transform;
                    }

                    UpdatePhysicsState(currentPlatform);
                }
            }
        }

        public void UpdatePhysicsState(Transform newPlatform)
        {
            if (positionTarget == null) return;

            // 如果平台发生了切换，需要对上一帧和当前帧的记录进行“重锚定”，防止插值产生1帧的闪烁
            if (newPlatform != _currentReference)
            {
                ReAnchor(_currentReference, newPlatform);
                _currentReference = newPlatform;
            }

            // 记录上一物理帧的相对状态
            _prevRelativePos = _currentRelativePos;
            _prevRelativeRot = _currentRelativeRot;

            // 记录当前物理帧的相对状态
            if (_currentReference != null)
            {
                _currentRelativePos = _currentReference.InverseTransformPoint(positionTarget.position);
                _currentRelativeRot = Quaternion.Inverse(_currentReference.rotation) * positionTarget.rotation;
            }
            else
            {
                _currentRelativePos = positionTarget.position;
                _currentRelativeRot = positionTarget.rotation;
            }
        }

        // 重锚定：将存储的相对数据平滑转换到新的参考空间
        private void ReAnchor(Transform oldRef, Transform newRef)
        {
            Vector3 prevWorldPos = (oldRef != null) ? oldRef.TransformPoint(_prevRelativePos) : _prevRelativePos;
            _prevRelativePos = (newRef != null) ? newRef.InverseTransformPoint(prevWorldPos) : prevWorldPos;

            Vector3 currWorldPos = (oldRef != null) ? oldRef.TransformPoint(_currentRelativePos) : _currentRelativePos;
            _currentRelativePos = (newRef != null) ? newRef.InverseTransformPoint(currWorldPos) : currWorldPos;

            Quaternion prevWorldRot = (oldRef != null) ? oldRef.rotation * _prevRelativeRot : _prevRelativeRot;
            _prevRelativeRot = (newRef != null) ? Quaternion.Inverse(newRef.rotation) * prevWorldRot : prevWorldRot;

            Quaternion currWorldRot = (oldRef != null) ? oldRef.rotation * _currentRelativeRot : _currentRelativeRot;
            _currentRelativeRot = (newRef != null) ? Quaternion.Inverse(newRef.rotation) * currWorldRot : currWorldRot;
        }

        private void LateUpdate()
        {
            if (positionTarget == null || smoothTarget == null || !enabled) return;

            // 计算当前渲染帧处于两个物理 Tick 之间的时间比例
            float t = (Time.time - Time.fixedTime) / Time.fixedDeltaTime;
            t = Mathf.Clamp01(t);

            // 平滑相对插值
            Vector3 interpolatedRelativePos = Vector3.Lerp(_prevRelativePos, _currentRelativePos, t);
            Quaternion interpolatedRelativeRot = Quaternion.Slerp(_prevRelativeRot, _currentRelativeRot, t);

            // 转换回世界空间并应用
            // Rotation is changed by PlayerManager's head rotation, for low latency
            if (_currentReference != null)
            {
                smoothTarget.position = _currentReference.TransformPoint(interpolatedRelativePos);
                if (enableRotationSmoothing)
                    smoothTarget.rotation = _currentReference.rotation * interpolatedRelativeRot;
            }
            else
            {
                smoothTarget.position = interpolatedRelativePos;
                if (enableRotationSmoothing)
                    smoothTarget.rotation = interpolatedRelativeRot;
            }
        }
    }
}
