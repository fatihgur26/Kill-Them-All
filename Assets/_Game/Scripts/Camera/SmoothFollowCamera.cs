using UnityEngine;

namespace KillThemAll.CameraSystem
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class SmoothFollowCamera : MonoBehaviour
    {
        [Header("Target & Positioning")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 16f, -12f);
        [SerializeField] private float smoothSpeed = 10f;

        [Header("Camera Tilt & Rotation")]
        [SerializeField] private float pitchAngle = 55f;
        [SerializeField] private float yawAngle = 0f;

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 targetPosition = target.position + offset;

            if (Application.isPlaying)
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
            }
            else
            {
                transform.position = targetPosition;
            }

            transform.rotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void SnapToTarget()
        {
            if (target != null)
            {
                transform.position = target.position + offset;
                transform.rotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);
            }
        }
    }
}
