// =============================================================================
// PXG // HARD-GRID COORDINATE SYSTEM (HGCS)
// Platform Module: Platform Profile Manager
// Auto-detect runtime platform and select optimal configuration.
// Operational Order: PXG-2026-SOMU-0325
// =============================================================================

using System;
using UnityEngine;

namespace PXG.HGCS.Platform
{
    /// <summary>
    /// Unified platform manager that auto-detects the runtime environment
    /// and configures the optimal resolution profile, grid cell size, and
    /// display adapter for the current hardware.
    ///
    /// Supports: Desktop (8K/4K/FHD), XR (Quest/Vision Pro), Mobile WebGPU.
    /// </summary>
    public class PlatformProfileManager
    {
        // ── Platform Category ───────────────────────────────────────────────

        /// <summary>High-level platform categories.</summary>
        public enum PlatformCategory
        {
            Desktop,
            XR,
            MobileWeb,
            Tablet,
            Unknown
        }

        // ── Active Configuration ────────────────────────────────────────────

        /// <summary>Detected platform category.</summary>
        public PlatformCategory Category { get; private set; }

        /// <summary>Active grid for the current platform.</summary>
        public DeterministicGrid ActiveGrid { get; private set; }

        /// <summary>Active multi-resolution scaler.</summary>
        public MultiResolutionScaler Scaler { get; private set; }

        /// <summary>XR adapter (null if not XR).</summary>
        public XRDisplayAdapter XRAdapter { get; private set; }

        /// <summary>WebGPU bridge (null if not mobile web).</summary>
        public WebGPUBridge WebBridge { get; private set; }

        /// <summary>Human-readable platform description.</summary>
        public string PlatformDescription { get; private set; }

        // ── Configuration ───────────────────────────────────────────────────

        /// <summary>Base grid cell size (adjusted per platform).</summary>
        public int BaseCellSize { get; set; } = 8;

        // ── Constructor ─────────────────────────────────────────────────────

        public PlatformProfileManager(int baseCellSize = 8)
        {
            BaseCellSize = baseCellSize;
            Scaler = new MultiResolutionScaler();
        }

        // ── Auto-Detection ──────────────────────────────────────────────────

        /// <summary>
        /// Auto-detects the runtime platform and configures all subsystems.
        /// Call once at startup (e.g., in Awake or Start).
        /// </summary>
        public void AutoDetect()
        {
            // Check XR first (highest priority)
            if (IsXRPresent())
            {
                ConfigureXR();
            }
            // Check for mobile web
            else if (IsMobileWeb())
            {
                ConfigureMobileWeb();
            }
            // Desktop fallback
            else
            {
                ConfigureDesktop();
            }

            Debug.Log($"[PXG.HGCS.Platform] {PlatformDescription}");
        }

        // ── XR Configuration ────────────────────────────────────────────────

        private void ConfigureXR()
        {
            Category = PlatformCategory.XR;

            // Auto-detect specific XR device
            var device = DetectXRDevice();
            XRAdapter = new XRDisplayAdapter(device, BaseCellSize);
            ActiveGrid = XRAdapter.CreatePerEyeGrid();

            Scaler.SetProfile(MultiResolutionScaler.Profile.XR_HMD);

            PlatformDescription = $"[XR] {XRAdapter}";
        }

        /// <summary>Detects the active XR device (stub — expand with XR SDK).</summary>
        private XRDisplayAdapter.XRDevice DetectXRDevice()
        {
            // In production: query UnityEngine.XR.InputDevices or XRSettings
            // Stub: default to Quest 3 as most common dev target
            string deviceName = SystemInfo.deviceModel?.ToLower() ?? "";

            if (deviceName.Contains("quest"))
                return XRDisplayAdapter.XRDevice.MetaQuest3;
            if (deviceName.Contains("vision"))
                return XRDisplayAdapter.XRDevice.AppleVisionPro;

            return XRDisplayAdapter.XRDevice.MetaQuest3; // default
        }

        // ── Mobile Web Configuration ────────────────────────────────────────

        private void ConfigureMobileWeb()
        {
            Category = PlatformCategory.MobileWeb;

            var profile = DetectMobileProfile();
            WebBridge = new WebGPUBridge(profile, BaseCellSize);
            ActiveGrid = WebBridge.CreateMobileGrid();

            Scaler.SetProfile(MultiResolutionScaler.Profile.MobileWebGPU);

            PlatformDescription = $"[Web] {WebBridge}";
        }

        /// <summary>Detects the mobile browser/device profile.</summary>
        private WebGPUBridge.MobileProfile DetectMobileProfile()
        {
            int screenW = Screen.width;
            int screenH = Screen.height;

            // Heuristic: classify by screen dimensions
            if (screenW >= 1024 && screenH >= 1024)
                return WebGPUBridge.MobileProfile.iPadPro;

            if (screenW >= 400 || screenH >= 900)
                return WebGPUBridge.MobileProfile.Pixel8;

            return WebGPUBridge.MobileProfile.GenericMobile;
        }

        // ── Desktop Configuration ───────────────────────────────────────────

        private void ConfigureDesktop()
        {
            Category = PlatformCategory.Desktop;

            int screenW = Screen.currentResolution.width;

            // Classify by monitor resolution
            if (screenW >= 7000)
            {
                Scaler.SetProfile(MultiResolutionScaler.Profile.UHD_8K);
                PlatformDescription = "[Desktop] 8K Research Display";
            }
            else if (screenW >= 3800)
            {
                Scaler.SetProfile(MultiResolutionScaler.Profile.UHD_4K);
                PlatformDescription = "[Desktop] 4K Display";
            }
            else
            {
                Scaler.SetProfile(MultiResolutionScaler.Profile.FHD_1080p);
                PlatformDescription = "[Desktop] Full HD Display";
            }

            // Create grid at detected resolution
            int cols = Scaler.TargetResolution.x / BaseCellSize;
            int rows = Scaler.TargetResolution.y / BaseCellSize;
            ActiveGrid = new DeterministicGrid(BaseCellSize, cols, rows);
        }

        // ── Platform Detection Helpers ──────────────────────────────────────

        private bool IsXRPresent()
        {
            // Stub — in production, use UnityEngine.XR.XRSettings.isDeviceActive
            // or check UnityEngine.XR.XRGeneralSettings
            return Application.platform == RuntimePlatform.Android
                && SystemInfo.deviceModel?.ToLower().Contains("quest") == true;
        }

        private bool IsMobileWeb()
        {
            return Application.platform == RuntimePlatform.WebGLPlayer;
        }

        // ── Manual Override ─────────────────────────────────────────────────

        /// <summary>
        /// Manually set platform for demo/testing purposes.
        /// </summary>
        public void ForceXR(XRDisplayAdapter.XRDevice device)
        {
            XRAdapter = new XRDisplayAdapter(device, BaseCellSize);
            ActiveGrid = XRAdapter.CreatePerEyeGrid();
            Scaler.SetProfile(MultiResolutionScaler.Profile.XR_HMD);
            Category = PlatformCategory.XR;
            PlatformDescription = $"[XR-FORCED] {XRAdapter}";
        }

        /// <summary>
        /// Manually set mobile web profile for demo/testing.
        /// </summary>
        public void ForceMobileWeb(WebGPUBridge.MobileProfile profile)
        {
            WebBridge = new WebGPUBridge(profile, BaseCellSize);
            ActiveGrid = WebBridge.CreateMobileGrid();
            Scaler.SetProfile(MultiResolutionScaler.Profile.MobileWebGPU);
            Category = PlatformCategory.MobileWeb;
            PlatformDescription = $"[Web-FORCED] {WebBridge}";
        }
    }
}
