// =============================================================================
// PXG // HARD-GRID COORDINATE SYSTEM (HGCS)
// Platform Module: XR Display Adapter
// Per-eye stereo grid, passthrough display bounds, Quest/Vision Pro profiles.
// Operational Order: PXG-2026-SOMU-0325
// =============================================================================

using System;
using UnityEngine;

namespace PXG.HGCS.Platform
{
    /// <summary>
    /// XR-specific display adapter for head-mounted displays. Manages
    /// per-eye resolution, stereo grid offsets, and passthrough display
    /// bounds for deterministic HGCS layout in immersive environments.
    /// </summary>
    public class XRDisplayAdapter
    {
        // ── XR Device Profiles ──────────────────────────────────────────────

        /// <summary>Known XR device profiles with hardware specs.</summary>
        public enum XRDevice
        {
            /// <summary>Generic XR fallback.</summary>
            Generic,

            /// <summary>Meta Quest 3 — 2064×2208 per eye, 120Hz.</summary>
            MetaQuest3,

            /// <summary>Meta Quest Pro — 1920×1800 per eye, 90Hz.</summary>
            MetaQuestPro,

            /// <summary>Apple Vision Pro — 3660×3200 per eye, 100Hz.</summary>
            AppleVisionPro,

            /// <summary>HTC Vive XR Elite — 1920×1920 per eye, 90Hz.</summary>
            ViveXRElite,

            /// <summary>PlayStation VR2 — 2000×2040 per eye, 120Hz.</summary>
            PSVR2
        }

        /// <summary>Per-eye resolution and refresh rate for an XR device.</summary>
        public readonly struct DeviceSpec
        {
            public readonly Vector2Int PerEyeResolution;
            public readonly int RefreshRateHz;
            public readonly float IPDRangeMm;  // inter-pupillary distance range
            public readonly bool HasPassthrough;
            public readonly string DisplayName;

            public DeviceSpec(int w, int h, int hz, float ipd, bool passthrough, string name)
            {
                PerEyeResolution = new Vector2Int(w, h);
                RefreshRateHz = hz;
                IPDRangeMm = ipd;
                HasPassthrough = passthrough;
                DisplayName = name;
            }
        }

        // ── Device Specs Database ───────────────────────────────────────────

        /// <summary>Returns hardware specs for a device profile.</summary>
        public static DeviceSpec GetDeviceSpec(XRDevice device) => device switch
        {
            XRDevice.MetaQuest3     => new DeviceSpec(2064, 2208, 120, 68f, true,  "Meta Quest 3"),
            XRDevice.MetaQuestPro   => new DeviceSpec(1920, 1800, 90,  64f, true,  "Meta Quest Pro"),
            XRDevice.AppleVisionPro => new DeviceSpec(3660, 3200, 100, 63f, true,  "Apple Vision Pro"),
            XRDevice.ViveXRElite    => new DeviceSpec(1920, 1920, 90,  62f, true,  "Vive XR Elite"),
            XRDevice.PSVR2          => new DeviceSpec(2000, 2040, 120, 63f, false, "PlayStation VR2"),
            _                       => new DeviceSpec(1920, 1080, 90,  64f, false, "Generic XR")
        };

        // ── State ───────────────────────────────────────────────────────────

        /// <summary>Active XR device.</summary>
        public XRDevice ActiveDevice { get; private set; }

        /// <summary>Active device specifications.</summary>
        public DeviceSpec ActiveSpec { get; private set; }

        /// <summary>HGCS grid cell size adapted for XR (may differ from desktop).</summary>
        public int XRCellSize { get; private set; }

        // ── Stereo Grid ─────────────────────────────────────────────────────

        /// <summary>Left-eye grid origin offset (half-IPD shift).</summary>
        public Vector2Int LeftEyeOffset { get; private set; }

        /// <summary>Right-eye grid origin offset.</summary>
        public Vector2Int RightEyeOffset { get; private set; }

        // ── Constructor ─────────────────────────────────────────────────────

        public XRDisplayAdapter(XRDevice device = XRDevice.Generic, int baseCellSize = 8)
        {
            SetDevice(device, baseCellSize);
        }

        // ── Device Switching ────────────────────────────────────────────────

        /// <summary>
        /// Switches to a new XR device profile, recalculating grid parameters.
        /// </summary>
        public void SetDevice(XRDevice device, int baseCellSize = 8)
        {
            ActiveDevice = device;
            ActiveSpec = GetDeviceSpec(device);

            // Scale cell size based on per-eye pixel density
            // Higher resolution = smaller cells for finer precision
            float densityFactor = ActiveSpec.PerEyeResolution.x / 1920f;
            XRCellSize = Mathf.Max(4, Mathf.RoundToInt(baseCellSize * densityFactor));

            // Calculate stereo offsets based on IPD
            // IPD in mm → convert to pixel offset at assumed 12px/mm screen density
            int ipdPixels = Mathf.RoundToInt(ActiveSpec.IPDRangeMm * 0.5f * 12f);
            LeftEyeOffset = new Vector2Int(-ipdPixels / 2, 0);
            RightEyeOffset = new Vector2Int(ipdPixels / 2, 0);
        }

        // ── Grid Factory ────────────────────────────────────────────────────

        /// <summary>
        /// Creates a DeterministicGrid configured for the active XR device's
        /// per-eye resolution.
        /// </summary>
        public DeterministicGrid CreatePerEyeGrid()
        {
            int cols = ActiveSpec.PerEyeResolution.x / XRCellSize;
            int rows = ActiveSpec.PerEyeResolution.y / XRCellSize;
            return new DeterministicGrid(XRCellSize, cols, rows);
        }

        /// <summary>
        /// Creates stereo grid pair (left + right eye) with IPD offset.
        /// </summary>
        public (DeterministicGrid left, DeterministicGrid right) CreateStereoGrids()
        {
            var left = CreatePerEyeGrid();
            left.SetOrigin(LeftEyeOffset);

            var right = CreatePerEyeGrid();
            right.SetOrigin(RightEyeOffset);

            return (left, right);
        }

        // ── Passthrough Bounds ──────────────────────────────────────────────

        /// <summary>
        /// Returns the safe rendering area for passthrough AR overlays.
        /// UI elements outside these bounds may be clipped by passthrough edges.
        /// </summary>
        public Rect GetPassthroughSafeArea()
        {
            if (!ActiveSpec.HasPassthrough)
                return new Rect(0, 0, ActiveSpec.PerEyeResolution.x, ActiveSpec.PerEyeResolution.y);

            // 10% inset for passthrough edge distortion
            float insetX = ActiveSpec.PerEyeResolution.x * 0.1f;
            float insetY = ActiveSpec.PerEyeResolution.y * 0.1f;

            return new Rect(
                insetX, insetY,
                ActiveSpec.PerEyeResolution.x - insetX * 2,
                ActiveSpec.PerEyeResolution.y - insetY * 2);
        }

        // ── Frame Budget ────────────────────────────────────────────────────

        /// <summary>
        /// Returns the per-frame time budget in milliseconds for the
        /// active device's refresh rate. Layout operations must complete
        /// within this budget to avoid dropped frames.
        /// </summary>
        public float FrameBudgetMs => 1000f / ActiveSpec.RefreshRateHz;

        // ── Debug ───────────────────────────────────────────────────────────

        public override string ToString() =>
            $"[XR] {ActiveSpec.DisplayName}: {ActiveSpec.PerEyeResolution.x}×{ActiveSpec.PerEyeResolution.y}/eye " +
            $"@ {ActiveSpec.RefreshRateHz}Hz | Cell: {XRCellSize}px | " +
            $"Budget: {FrameBudgetMs:F1}ms | Passthrough: {ActiveSpec.HasPassthrough}";
    }
}
