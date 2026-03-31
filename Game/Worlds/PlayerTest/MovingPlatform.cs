using PrimeTween;
using Tewi.Game.Network;
using UnityEngine;

namespace Tewi.Game.Worlds.PlayerTest
{
    public class MovingPlatform : NetworkMovingPlatform
    {
        public enum AnimationType
        {
            rotation,
            rotation2,
            linear,
            fastLinear,
        }
        public AnimationType _animationType;

        protected override void CreateAnimation()
        {
            switch (_animationType)
            {
                case AnimationType.rotation:
                    _tween =
                        Tween.RigidbodyMoveRotation(_rigidbody, new TweenSettings<Vector3>(
                            new Vector3(0, 0, 0),
                            new Vector3(0, 180, 0),
                            2, Ease.Linear,
                            -1, CycleMode.Incremental,
                            updateType: UpdateType.FixedUpdate));
                    break;
                case AnimationType.rotation2:
                    Vector3 startRot = new Vector3(0, 0, 0);
                    Vector3 endRot = new Vector3(0, 360, 0);

                    _tween = Tween.Custom(
                        new TweenSettings<Vector3>(startRot, endRot, 1, Ease.Linear, -1, CycleMode.Rewind, updateType: UpdateType.FixedUpdate),
                        onValueChange: (currentEuler) =>
                        {
                            _rigidbody.MoveRotation(Quaternion.Euler(currentEuler));
                        }
                    );
                    break;
                case AnimationType.linear:
                    _sequence = Sequence.Create(-1, Sequence.SequenceCycleMode.Rewind, updateType: UpdateType.FixedUpdate)
                        .ChainDelay(2)
                        .Chain(Tween.RigidbodyMovePosition(_rigidbody, transform.position, transform.position + -Vector3.right * 14, 4, Ease.Linear));
                    break;
                case AnimationType.fastLinear:
                    _sequence = Sequence.Create(-1, Sequence.SequenceCycleMode.Rewind, updateType: UpdateType.FixedUpdate)
                        .ChainDelay(2)
                        .Chain(Tween.RigidbodyMovePosition(_rigidbody, transform.position, transform.position + -Vector3.right * 14, .3f, Ease.Linear));
                    break;
            }
        }
    }
}
