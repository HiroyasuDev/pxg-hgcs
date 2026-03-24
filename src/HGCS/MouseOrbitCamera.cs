using UnityEngine;
using UnityEngine.InputSystem;

namespace PXG.HGCS
{
    /// <summary>
    /// Grants the user live XR physical inspection capabilities during the presentation.
    /// Supports orbital rotation around the procedural Biological mass using the Right Mouse Button,
    /// and spatial zooming using the Mouse ScrollWheel. Compliant with modern XR Input System.
    /// </summary>
    public class MouseOrbitCamera : MonoBehaviour
    {
        [Header("Orbit Targets")]
        public Transform target;
        public float distance = 1200.0f;
        
        [Header("Telemetry Speeds")]
        public float xSpeed = 12.0f;
        public float ySpeed = 12.0f;
        public float zoomSpeed = 8.0f;

        [Header("Orbit Constraints")]
        public float yMinLimit = -60f;
        public float yMaxLimit = 60f;
        public float minDistance = 300f;
        public float maxDistance = 4000f;

        private float x = 0.0f;
        private float y = 0.0f;

        void Start()
        {
            Vector3 angles = transform.eulerAngles;
            x = angles.y;
            y = angles.x;

            if (GetComponent<Rigidbody>())
            {
                GetComponent<Rigidbody>().freezeRotation = true;
            }
        }

        void LateUpdate()
        {
            if (target && Mouse.current != null)
            {
                // Scroll wheel natively zooms the spatial distance
                float scrollDelta = Mouse.current.scroll.y.ReadValue() / 120f;
                distance -= scrollDelta * zoomSpeed * 10f;
                distance = Mathf.Clamp(distance, minDistance, maxDistance);

                // Check for Right Click or Left Alt
                bool isOrbiting = Mouse.current.rightButton.isPressed;
                if (Keyboard.current != null && Keyboard.current.leftAltKey.isPressed) isOrbiting = true;

                if (isOrbiting)
                {
                    Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                    x += mouseDelta.x * xSpeed * distance * 0.0002f;
                    y -= mouseDelta.y * ySpeed * 0.02f;
                }

                y = ClampAngle(y, yMinLimit, yMaxLimit);

                Quaternion rotation = Quaternion.Euler(y, x, 0);
                Vector3 position = rotation * new Vector3(0.0f, 0.0f, -distance) + target.position;

                transform.rotation = rotation;
                transform.position = position;
            }
        }

        public static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360F) angle += 360F;
            if (angle > 360F) angle -= 360F;
            return Mathf.Clamp(angle, min, max);
        }
    }
}
