using PrimeTween;
using Tewi.Helpers;
using Tewi.Helpers.Extensions;
using UnityEngine;

namespace Tewi.Game.Worlds.TestWorld
{
    public class MovingPlatformManager: MonoBehaviour
    {
        public int movementType = 0;
        public Transform moveTransform;
        public Transform rotationTransform;

        private void Update()
        {
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            /*TweenMover.DoSequence(Sequence.Create(-1, CycleMode.Yoyo)
                    .Group(Tween.Position(TweenMover.transform, TweenMover.transform.position.SetX(-50), 4, Ease.InOutQuad))
                    .ChainDelay(4).Group(Tween.Position(TweenMover.transform, TweenMover.transform.position.SetX(-26), 4, Ease.InOutQuad)));*/
            var a = moveTransform.GetComponent<Rigidbody>();
            if (movementType == 0)
            {
                var b = rotationTransform.GetComponent<Rigidbody>();
                Sequence.Create(-1, Sequence.SequenceCycleMode.Yoyo, updateType: UpdateType.FixedUpdate)
                        .ChainDelay(5)
                        .Chain(Tween.RigidbodyMovePosition(a, moveTransform.transform.position.SetX(-100).SetY(100), 2, Ease.Linear));

                Sequence.Create(-1, Sequence.SequenceCycleMode.Yoyo)
                        .Group(Tween.LocalRotation(rotationTransform, new Vector3(180, 0, 0), 5, Ease.Linear));
            }
            else
            {
                Sequence.Create(-1, Sequence.SequenceCycleMode.Yoyo, updateType: UpdateType.FixedUpdate)
                        .ChainDelay(5)
                        .Chain(Tween.RigidbodyMovePosition(a, moveTransform.transform.position.SetY(100), 8, Ease.Linear))
                        .Chain(Tween.RigidbodyMovePosition(a, moveTransform.transform.position.SetY(100).SetX(-100), 8, Ease.Linear));
            }

        }
    }
}