using UnityEngine;
using PrimeTween;
using FishNet.Object;
using FishNet.Object.Synchronizing;

namespace Tewi.Game.Network
{
    public abstract class NetworkMovingPlatform : NetworkBehaviour
    {
        public Rigidbody _rigidbody;

        protected Vector3 _designLocalPosition;
        protected Quaternion _designLocalRotation;
        protected Vector3 _designLocalScale;

        protected Sequence _sequence;
        protected Tween _tween;
        protected readonly SyncVar<float> _time = new(new(sendRate: 5));

        public bool isSequence => _sequence.isAlive;
        public bool isTween => _tween.isAlive;

        #region Server logic
        public override void OnStartServer()
        {
            base.OnStartServer();
            CreateAnimation();
            TimeManager.OnTick += TimeManager_OnTick;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            TimeManager.OnTick -= TimeManager_OnTick;
        }

        [Server]
        private void TimeManager_OnTick()
        {
            if (_sequence.isAlive)
                _time.Value = _sequence.elapsedTimeTotal;

            if (_tween.isAlive)
                _time.Value = _tween.elapsedTimeTotal;

        }

        /// <summary>
        /// 向服务器请求当前时间以同步给新加入的客户端，触发同步事件以更新客户端的时间。
        /// </summary>
        /// <remarks><seealso cref="ServerRpcAttribute"/></remarks>
        [ServerRpc(RequireOwnership = false)]
        public void RequestTime()
        {
            _time.Value = _time.Value; // 触发同步
        }
        #endregion

        protected void Awake()
        {
            _designLocalPosition = transform.localPosition;
            _designLocalRotation = transform.localRotation;
            _designLocalScale = transform.localScale;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (IsServerStarted) return;
            _tween.Complete();
            _sequence.Complete();

            transform.SetLocalPositionAndRotation(_designLocalPosition, _designLocalRotation);
            transform.localScale = _designLocalScale;

            CreateAnimation();
            _time.OnChange += Time_OnChange;
            if (!IsServerStarted) RequestTime();
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            _time.OnChange -= Time_OnChange;

            if (IsServerStarted) return;
            _tween.Complete();
            _sequence.Complete();

            transform.SetLocalPositionAndRotation(_designLocalPosition, _designLocalRotation);
            transform.localScale = _designLocalScale;
        }

        private void Time_OnChange(float prev, float next, bool asServer)
        {
            if (asServer) return;

            if (_sequence.isAlive)
                _sequence.elapsedTimeTotal = next;

            if (_tween.isAlive)
                _tween.elapsedTimeTotal = next;
        }

        protected abstract void CreateAnimation();
    }
}
