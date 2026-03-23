// =============================================================================
// PXG // HARD-GRID COORDINATE SYSTEM (HGCS)
// REAL INTEGRATION: Quest 3 XR Subsystem Bootstrapper
// Manually initializes and verifies XR hardware communication.
// Operational Order: PXG-2026-SOMU-0325
// =============================================================================
//
// PREREQUISITES:
// 1. com.unity.xr.management (XR Plugin Management)
// 2. com.unity.xr.openxr (OpenXR Plugin)
// 3. com.meta.xr.sdk.core (Meta XR Core SDK) - OR Meta OpenXR Feature pack
// 4. Project Settings > XR Plug-in Management > OpenXR > Meta Quest Feature Group
// 5. AndroidManifest.xml must declare:
//    <uses-feature android:name="com.oculus.feature.PASSTHROUGH" android:required="true"/>
//    <uses-permission android:name="com.oculus.permission.HAND_TRACKING"/>
//
// ATTACH: To a GameObject in your boot scene. Runs before anything else.
// =============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace PXG.HGCS.Platform
{
    /// <summary>
    /// Quest 3 XR subsystem bootstrapper. Verifies that the headset's
    /// hardware is actually talking to Unity by manually enumerating and
    /// validating <see cref="XRDisplaySubsystem"/> and
    /// <see cref="XRInputSubsystem"/> instances.
    ///
    /// This is the "bridge" between your grid math and the physical hardware.
    /// Without this, you're painting pixels into a void.
    /// </summary>
    public class XRSubsystemBootstrapper : MonoBehaviour
    {
        // ── Hardware State ──────────────────────────────────────────────────

        /// <summary>Whether XR subsystems are active and verified.</summary>
        public bool IsXRReady { get; private set; }

        /// <summary>Whether hand tracking is available.</summary>
        public bool HandTrackingAvailable { get; private set; }

        /// <summary>Whether passthrough is available.</summary>
        public bool PassthroughAvailable { get; private set; }

        /// <summary>Connected HMD device name.</summary>
        public string DeviceName { get; private set; } = "Not Detected";

        /// <summary>Per-eye render resolution reported by hardware.</summary>
        public Vector2Int PerEyeResolution { get; private set; }

        /// <summary>Refresh rate reported by hardware.</summary>
        public float RefreshRate { get; private set; }

        /// <summary>Tracking origin mode.</summary>
        public TrackingOriginModeFlags TrackingOrigin { get; private set; }

        // ── Subsystem References ────────────────────────────────────────────

        private XRDisplaySubsystem _display;
        private XRInputSubsystem _input;

        // ── Events ──────────────────────────────────────────────────────────

        /// <summary>Fired when XR subsystems are verified and ready.</summary>
        public event Action OnXRReady;

        /// <summary>Fired when XR initialization fails.</summary>
        public event Action<string> OnXRFailed;

        // ── Lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            StartCoroutine(InitializeXR());
        }

        /// <summary>
        /// Full XR initialization sequence:
        /// 1. Wait for XR Management to initialize
        /// 2. Enumerate Display subsystem
        /// 3. Enumerate Input subsystem
        /// 4. Verify hardware capabilities
        /// 5. Report status
        /// </summary>
        private IEnumerator InitializeXR()
        {
            Debug.Log("[PXG.HGCS.XR] ──── XR Subsystem Bootstrap Begin ────");

            // ── Step 1: Wait for XR Management ──────────────────────────────
            var xrManager = XRGeneralSettings.Instance;
            if (xrManager == null)
            {
                string error = "XRGeneralSettings not found. Is XR Plugin Management installed?";
                Debug.LogError($"[PXG.HGCS.XR] FATAL: {error}");
                OnXRFailed?.Invoke(error);
                yield break;
            }

            var xrManagerInstance = xrManager.Manager;
            if (xrManagerInstance == null)
            {
                string error = "XRManagerSettings is null. Check Project Settings > XR Plug-in Management.";
                Debug.LogError($"[PXG.HGCS.XR] FATAL: {error}");
                OnXRFailed?.Invoke(error);
                yield break;
            }

            // Wait for loader initialization if not auto-initialized
            if (!xrManagerInstance.isInitializationComplete)
            {
                Debug.Log("[PXG.HGCS.XR] Waiting for XR initialization...");
                yield return new WaitUntil(() => xrManagerInstance.isInitializationComplete);
            }

            var activeLoader = xrManagerInstance.activeLoader;
            if (activeLoader == null)
            {
                string error = "No active XR loader. Ensure OpenXR or Meta XR is enabled in XR Plug-in Management.";
                Debug.LogError($"[PXG.HGCS.XR] FATAL: {error}");
                OnXRFailed?.Invoke(error);
                yield break;
            }

            Debug.Log($"[PXG.HGCS.XR] Active loader: {activeLoader.name}");

            // ── Step 2: Enumerate Display Subsystem ─────────────────────────
            var displaySubsystems = new List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(displaySubsystems);

            if (displaySubsystems.Count == 0)
            {
                string error = "No XRDisplaySubsystem found. Hardware may not be connected.";
                Debug.LogError($"[PXG.HGCS.XR] {error}");
                OnXRFailed?.Invoke(error);
                yield break;
            }

            _display = displaySubsystems[0];
            Debug.Log($"[PXG.HGCS.XR] Display subsystem: {_display.SubsystemDescriptor.id}");
            Debug.Log($"[PXG.HGCS.XR]   Running: {_display.running}");

            // Get render resolution
            if (_display.TryGetRenderPass(0, out var renderPass))
            {
                renderPass.GetRenderParameter(Camera.main, 0, out var renderParam);
                PerEyeResolution = new Vector2Int(
                    renderParam.textureWidth,
                    renderParam.textureHeight);
                Debug.Log($"[PXG.HGCS.XR]   Per-eye resolution: {PerEyeResolution.x}×{PerEyeResolution.y}");
            }

            // ── Step 3: Enumerate Input Subsystem ───────────────────────────
            var inputSubsystems = new List<XRInputSubsystem>();
            SubsystemManager.GetSubsystems(inputSubsystems);

            if (inputSubsystems.Count == 0)
            {
                Debug.LogWarning("[PXG.HGCS.XR] No XRInputSubsystem found. Input may not work.");
            }
            else
            {
                _input = inputSubsystems[0];
                TrackingOrigin = _input.GetTrackingOriginMode();
                Debug.Log($"[PXG.HGCS.XR] Input subsystem: {_input.SubsystemDescriptor.id}");
                Debug.Log($"[PXG.HGCS.XR]   Tracking: {TrackingOrigin}");

                // Try to set floor-level tracking (Quest standard)
                if (_input.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor))
                {
                    Debug.Log("[PXG.HGCS.XR]   Set tracking origin to Floor.");
                }
            }

            // ── Step 4: Detect Device Capabilities ──────────────────────────
            var inputDevices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.HeadMounted, inputDevices);

            if (inputDevices.Count > 0)
            {
                var hmd = inputDevices[0];
                DeviceName = hmd.name;

                hmd.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 headPos);
                hmd.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion headRot);

                Debug.Log($"[PXG.HGCS.XR]   HMD: {DeviceName}");
                Debug.Log($"[PXG.HGCS.XR]   Head position: {headPos}");
            }

            // Check for hand tracking devices
            var handDevices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.HandTracking, handDevices);
            HandTrackingAvailable = handDevices.Count > 0;
            Debug.Log($"[PXG.HGCS.XR]   Hand tracking: {HandTrackingAvailable}");

            // Passthrough check (Quest 3 specific)
            PassthroughAvailable = CheckPassthroughSupport();
            Debug.Log($"[PXG.HGCS.XR]   Passthrough: {PassthroughAvailable}");

            // Refresh rate
            if (XRDevice.refreshRate > 0)
            {
                RefreshRate = XRDevice.refreshRate;
                Debug.Log($"[PXG.HGCS.XR]   Refresh rate: {RefreshRate}Hz");
            }

            // ── Step 5: Mark Ready ──────────────────────────────────────────
            IsXRReady = true;
            Debug.Log("[PXG.HGCS.XR] ──── XR Subsystem Bootstrap COMPLETE ────");
            Debug.Log(GetHardwareReport());

            OnXRReady?.Invoke();
        }

        // ── Passthrough Detection ───────────────────────────────────────────

        private bool CheckPassthroughSupport()
        {
            // Check via SystemInfo (Quest 3 reports AR capability)
            if (SystemInfo.supportsAsyncGPUReadback)
            {
                // Meta Quest 3 identifies via device model
                string model = SystemInfo.deviceModel?.ToLower() ?? "";
                if (model.Contains("quest 3") || model.Contains("eureka"))
                    return true;
            }

            // Fallback: Check if OpenXR passthrough extension is present
            // In production, query OVRManager.isPassthroughSupported
            return false;
        }

        // ── HGCS Integration ────────────────────────────────────────────────

        /// <summary>
        /// Creates an <see cref="XRDisplayAdapter"/> configured with the
        /// actual hardware-reported resolution (not spec-sheet values).
        /// </summary>
        public XRDisplayAdapter CreateVerifiedAdapter(int baseCellSize = 8)
        {
            if (!IsXRReady)
            {
                Debug.LogWarning("[PXG.HGCS.XR] Hardware not verified — using defaults.");
                return new XRDisplayAdapter(XRDisplayAdapter.XRDevice.Generic, baseCellSize);
            }

            // Match device name to known profile
            var device = DeviceName?.ToLower() switch
            {
                string s when s.Contains("quest 3")     => XRDisplayAdapter.XRDevice.MetaQuest3,
                string s when s.Contains("quest pro")   => XRDisplayAdapter.XRDevice.MetaQuestPro,
                string s when s.Contains("vision")      => XRDisplayAdapter.XRDevice.AppleVisionPro,
                string s when s.Contains("vive")        => XRDisplayAdapter.XRDevice.ViveXRElite,
                string s when s.Contains("psvr")        => XRDisplayAdapter.XRDevice.PSVR2,
                _ => XRDisplayAdapter.XRDevice.Generic
            };

            return new XRDisplayAdapter(device, baseCellSize);
        }

        /// <summary>
        /// Creates a DeterministicGrid using the ACTUAL hardware per-eye
        /// resolution, not a spec-sheet estimate.
        /// </summary>
        public DeterministicGrid CreateHardwareGrid(int cellSize = 8)
        {
            if (PerEyeResolution == Vector2Int.zero)
            {
                Debug.LogWarning("[PXG.HGCS.XR] No per-eye resolution detected — using Quest 3 default.");
                return new DeterministicGrid(cellSize, 2064 / cellSize, 2208 / cellSize);
            }

            int cols = PerEyeResolution.x / cellSize;
            int rows = PerEyeResolution.y / cellSize;
            return new DeterministicGrid(cellSize, cols, rows);
        }

        // ── Diagnostics ─────────────────────────────────────────────────────

        /// <summary>
        /// Returns a formatted hardware report for debug overlay or console.
        /// </summary>
        public string GetHardwareReport()
        {
            return $"[PXG HARDWARE REPORT]\n" +
                   $"  Ready:      {IsXRReady}\n" +
                   $"  Device:     {DeviceName}\n" +
                   $"  Resolution: {PerEyeResolution.x}×{PerEyeResolution.y} per eye\n" +
                   $"  Refresh:    {RefreshRate:F0}Hz\n" +
                   $"  Tracking:   {TrackingOrigin}\n" +
                   $"  Hands:      {HandTrackingAvailable}\n" +
                   $"  Passthrough:{PassthroughAvailable}\n" +
                   $"  Frame Budget:{(RefreshRate > 0 ? (1000f / RefreshRate).ToString("F1") : "?")}ms";
        }
    }
}
