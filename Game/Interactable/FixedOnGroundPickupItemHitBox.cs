using Tewi.Helpers;
using UnityEngine;

namespace Tewi.Game.Interactable
{
    [RequireComponent(typeof(Collider))]
    public class FixedOnGroundPickupItemHitBox : MonoBehaviour
    {
        public Collider hitbox;
        public Rigidbody parentRigidbody;
        [ReadOnly] public Collider hitOther;
        //public PickupItem pickupItem;
        [SerializeField] private bool fixedOnGround = false;
        //[SerializeField] private bool isMovingGround = false;
        public bool testOnTheGround { get; private set; } = true;
        public Vector3 fixedOnGroundPosition = Vector3.zero;

        public void ValidateData()
        {
            if (!hitbox) hitbox = GetComponent<Collider>();
            if (!parentRigidbody && transform.parent) parentRigidbody = transform.parent.GetComponent<Rigidbody>();
        }

        private void Reset()
        {
            ValidateData();
        }

        private void OnValidate()
        {
            ValidateData();
        }

        void Start()
        {
            ValidateData();
            hitbox.excludeLayers = LayerMask.GetMask("Ignore Raycast", "Interactable", "Damageable", "HitBox");
        }

        private void OnTriggerEnter(Collider other)
        {
            hitOther = other;

            if (testOnTheGround)
            {
                testOnTheGround = false;
                fixedOnGround = true;
                parentRigidbody.isKinematic = true;
                parentRigidbody.interpolation = RigidbodyInterpolation.None;
                transform.parent.SetParent(other.transform);
                transform.parent.localRotation = new Quaternion(0, transform.parent.localRotation.y, 0, transform.parent.localRotation.w);
                //fixedOnGroundPosition = transform.parent.localPosition;
                //UpdatePositionAndRotation();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.tag == "Volume") return;
            hitOther = other;
        }

        public void StartTestGround()
        {
            testOnTheGround = true;
            fixedOnGround = false;
            parentRigidbody.isKinematic = false;
            //if (pickupItem.isOnGroundParent && false) parentRigidbody.interpolation = RigidbodyInterpolation.None;
            //else parentRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            parentRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            //isMovingGround = false;
        }

        public void StopTestGround()
        {
            testOnTheGround = false;
        }

        /*private void Update()
        {
            if (testOnTheGround)
            {
                fixedOnGround = false;
                parentRigidbody.isKinematic = false;
                //if (pickupItem.isOnGroundParent && false) parentRigidbody.interpolation = RigidbodyInterpolation.None;
                //else parentRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                parentRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                hitbox.excludeLayers = LayerMask.GetMask("Ignore Player Raycast interactable", "interactable", "Damageable", "HitBox");
                isMovingGround = false;
                *//*var c = Physics.OverlapBox(transform.position, new Vector3(testSize, testDistance, testSize), transform.rotation, ~layer);
                if (c.Length > 0)
                {
                    if (c[0] is Collider hit)
                    {
                        testOnTheGround = false;
                        fixedOnGround = true;
                        Rigidbody.isKinematic = true;
                        transform.SetParent(hit.transform);
                        fixedOnGroundPosition = transform.localPosition;
                        UpdatePositionAndRotation();*//*
                        if (hit.transform.GetComponent<IsMovingTransform>())
                        {
                            // isMovingGround = true;
                            Rigidbody.interpolation = RigidbodyInterpolation.None;
                        }*//*
                        if (hit.transform.GetComponent<Rigidbody>())
                        {
                            Rigidbody.interpolation = RigidbodyInterpolation.None;
                        }
                    }
                }*//*
            }

        }*/
        /*
                void LateUpdate()
                {
                    if (Rigidbody)
                    {
                        if (fixedOnGround && isMovingGround)
                        {
                            UpdatePositionAndRotation();
                        }
                    }
                }*/

        void UpdatePositionAndRotation()
        {
            transform.parent.SetLocalPositionAndRotation(fixedOnGroundPosition, new Quaternion(0, transform.parent.localRotation.y, 0, transform.parent.localRotation.w));
        }
    }
}