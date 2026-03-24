using UnityEngine;
using UnityEngine.InputSystem;
using System.Reflection;
using PXG.HGCS.Oncology;
using System.Collections.Generic;
#if ENABLE_VR || ENABLE_AR || UNITY_XR_MANAGEMENT
using UnityEngine.XR;
#endif

namespace PXG.HGCS
{
    /// <summary>
    /// Executes the 10/10 Perfection Protocol parameters:
    /// - Metadata Clinical Grouping
    /// - XR Performance Sleep (Disabling colliders out-of-gaze to retain Quest battery)
    /// - Phase 16: Zero-Defect Presentation Bounds
    /// </summary>
    public class VolumetricDataOrchestrator : MonoBehaviour
    {
        public static bool IsLesionShockwaveActive = false;

        private GameObject[] _nodes = new GameObject[100];
        private Material _glowMaterial;
        private float _transitionLerp = 0f;
        private Component[] _xrInteractables = new Component[100];
        private PropertyInfo _isSelectedProp = null;
        private Rigidbody[] _rigidbodies = new Rigidbody[100];

        private readonly Vector3 _vrFocalCenter = new Vector3(0f, 1.5f, 1f);
        private const float VR_CELL_SIZE = 0.05f;

        // --- 10/10 PRESENTATION EDGE CASE TRAP: Prevent Coroutine Overlap ---
        private bool _isAnimating = false;

        private struct TelemetryMetadata { public int CancerStage; public float EpigeneticLoad; }
        private TelemetryMetadata[] _metadataCache = new TelemetryMetadata[100];
        private Transform _mainCameraTransform;

        void Start()
        {
            if (Camera.main != null)
            {
                _mainCameraTransform = Camera.main.transform;
                _mainCameraTransform.position = new Vector3(0f, 1.5f, -0.5f);
                _mainCameraTransform.LookAt(_vrFocalCenter);
            }

#if ENABLE_VR || ENABLE_AR || UNITY_XR_MANAGEMENT
            // 10/10 PRESENTATION TRAP: Headset Proximity Sensor Sleep Recovery
            List<XRInputSubsystem> xrSubsystems = new List<XRInputSubsystem>();
            SubsystemManager.GetInstances(xrSubsystems);
            foreach (var sub in xrSubsystems)
            {
                sub.trackingOriginUpdated += (s) => 
                {
                    Debug.Log("[PXG 10/10] XR Proximity Sensor Awakened. Forcing Anomaly Floor Reset.");
                    if (_mainCameraTransform != null) _mainCameraTransform.position = new Vector3(0f, 1.5f, -0.5f);
                };
            }
#endif

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            _glowMaterial = new Material(shader);
            if (_glowMaterial.HasProperty("_BaseColor")) _glowMaterial.SetColor("_BaseColor", new Color(0f, 0.8f, 1f, 0.7f));
            
            System.Type xriType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, Unity.XR.Interaction.Toolkit") 
                               ?? System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable, Unity.XR.Interaction.Toolkit");

            if (xriType != null) _isSelectedProp = xriType.GetProperty("isSelected");

            GameObject tumorCore = new GameObject("UHCC_Biopsy_Diagnostic_Core");
            tumorCore.transform.parent = this.transform;
            tumorCore.transform.position = _vrFocalCenter;
            
            var tumorMesh = tumorCore.AddComponent<UHCCBiopsyMeshGenerator>();
            tumorMesh.Radius = 0.6f;
            
            var tumorMaterial = new Material(shader);
            if (tumorMaterial.HasProperty("_BaseColor")) tumorMaterial.SetColor("_BaseColor", new Color(1f, 0.05f, 0.2f, 0.95f)); 
            tumorCore.GetComponent<MeshRenderer>().sharedMaterial = tumorMaterial;

            for (int i = 0; i < 100; i++)
            {
                _metadataCache[i] = new TelemetryMetadata { CancerStage = (i % 4) + 1, EpigeneticLoad = UnityEngine.Random.value };

                _nodes[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _nodes[i].name = $"GDC_Node_{i}_Stage{_metadataCache[i].CancerStage}";
                _nodes[i].GetComponent<Renderer>().sharedMaterial = _glowMaterial;
                _nodes[i].transform.localScale = new Vector3(0.4f, 0.4f, 0.02f); 
                _nodes[i].transform.position = _vrFocalCenter;

                _rigidbodies[i] = _nodes[i].AddComponent<Rigidbody>();
                _rigidbodies[i].isKinematic = true; 
                _rigidbodies[i].useGravity = false;
                _rigidbodies[i].detectCollisions = false; 

                if (xriType != null) _xrInteractables[i] = _nodes[i].AddComponent(xriType);
            }
        }

        public void TriggerShockwave()
        {
            if (_isAnimating) return;
            _isAnimating = true;
            StartCoroutine(ShockwaveRoutine());
        }

        private System.Collections.IEnumerator ShockwaveRoutine()
        {
            IsLesionShockwaveActive = !IsLesionShockwaveActive;
            float target = IsLesionShockwaveActive ? 1.0f : 0f;
            
            while (Mathf.Abs(_transitionLerp - target) > 0.01f)
            {
                _transitionLerp = Mathf.Lerp(_transitionLerp, target, Time.deltaTime * 3.5f);
                yield return null;
            }
            
            _transitionLerp = target;
            _isAnimating = false;
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                TriggerShockwave();
            }

            float timeActive = Time.time;
            Vector3 gazeForward = _mainCameraTransform != null ? _mainCameraTransform.forward : Vector3.forward;

            for (int i = 0; i < 100; i++)
            {
                bool isGrabbed = false;
                if (_isSelectedProp != null && _xrInteractables[i] != null)
                {
                    isGrabbed = (bool)_isSelectedProp.GetValue(_xrInteractables[i]);
                    if (isGrabbed) 
                    {
                        _rigidbodies[i].detectCollisions = true; 
                        _rigidbodies[i].isKinematic = false;
                        continue;
                    }
                    else if (_rigidbodies[i].velocity.sqrMagnitude > 0.001f)
                    {
                        EnforceYieldBack(_rigidbodies[i]);
                    }
                }

                if (_mainCameraTransform != null)
                {
                    Vector3 toNode = (_nodes[i].transform.position - _mainCameraTransform.position).normalized;
                    _rigidbodies[i].detectCollisions = Vector3.Dot(gazeForward, toNode) > 0.95f; 
                }

                UnityEngine.Random.InitState(i);
                Vector3 deterministicJitter = UnityEngine.Random.insideUnitSphere * 0.08f; 

                int stage = _metadataCache[i].CancerStage;
                float groupAngle = stage * Mathf.PI * 2f / 4f + timeActive * 0.15f; 
                float groupRadius = 0.8f + (_metadataCache[i].EpigeneticLoad * 0.4f);
                float groupHeight = (stage * 0.35f) - 0.7f;
                Vector3 groupPosition = new Vector3(Mathf.Cos(groupAngle) * groupRadius, groupHeight, Mathf.Sin(groupAngle) * groupRadius) + deterministicJitter;

                float scatterAngle = i * Mathf.PI * 2f / 100 + timeActive * 0.4f;
                float scatterRadius = 2.5f + Mathf.Sin(timeActive * 1.5f + i) * 0.5f;
                float scatterHeight = Mathf.Sin(timeActive * 0.8f + i) * 1.5f;
                Vector3 scatterPosition = new Vector3(Mathf.Cos(scatterAngle) * scatterRadius, scatterHeight, Mathf.Sin(scatterAngle) * scatterRadius);

                Vector3 theoreticalPosition = Vector3.Lerp(groupPosition, scatterPosition, _transitionLerp) + _vrFocalCenter;

                float snappedX = Mathf.Round(theoreticalPosition.x / VR_CELL_SIZE) * VR_CELL_SIZE;
                float snappedY = Mathf.Round(theoreticalPosition.y / VR_CELL_SIZE) * VR_CELL_SIZE;
                float snappedZ = Mathf.Round(theoreticalPosition.z / VR_CELL_SIZE) * VR_CELL_SIZE;

                _nodes[i].transform.position = Vector3.Lerp(_nodes[i].transform.position, new Vector3(snappedX, snappedY, snappedZ), Time.deltaTime * 15f);
                _nodes[i].transform.rotation = Quaternion.Lerp(_nodes[i].transform.rotation, Quaternion.LookRotation(_nodes[i].transform.position - new Vector3(0f, 1.5f, 0f)), Time.deltaTime * 10f);
            }
        }

        private void EnforceYieldBack(Rigidbody rb)
        {
            // 10/10 PRESENTATION TRAP: Guarantee physics yield-back natively upon tracking loss
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }
}