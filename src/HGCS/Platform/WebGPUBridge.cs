// =============================================================================
// PXG // HARD-GRID COORDINATE SYSTEM (HGCS)
// Platform Module: WebGPU Runtime Bridge
// Mobile canvas detection, DPR compensation, touch-grid alignment.
// Operational Order: PXG-2026-SOMU-0325
// =============================================================================

using System;
using UnityEngine;

namespace PXG.HGCS.Platform
{
    /// <summary>
    /// Mobile WebGPU runtime bridge for browser-based deployment.
    /// Handles canvas size detection, Device Pixel Ratio (DPR) compensation,
    /// touch-input grid alignment, and WebGL/WebGPU fallback logic.
    ///
    /// Poster ref: "Cross-compatible from 8K simulation to Mobile WebGPU runtimes."
    /// </summary>
    public class WebGPUBridge
    {
        // ── Mobile Viewport Profiles ────────────────────────────────────────

        /// <summary>Common mobile viewport/DPR profiles.</summary>
        public enum MobileProfile
        {
            /// <summary>Generic mobile — 360×800 @ DPR 2.0.</summary>
            GenericMobile,

            /// <summary>Google Pixel 8 — 412×915 @ DPR 2.625.</summary>
            Pixel8,

            /// <summary>Samsung Galaxy S24 — 360×780 @ DPR 3.0.</summary>
            GalaxyS24,

            /// <summary>iPhone 15 Pro — 393×852 @ DPR 3.0.</summary>
            iPhone15Pro,

            /// <summary>iPad Pro 12.9" — 1024×1366 @ DPR 2.0.</summary>
            iPadPro,

            /// <summary>Desktop Chrome — 1920×1080 @ DPR 1.0.</summary>
            DesktopChrome
        }

        /// <summary>Viewport specification for a mobile profile.</summary>
        public readonly struct ViewportSpec
        {
            public readonly Vector2Int CSSPixels;       // logical viewport
            public readonly float DevicePixelRatio;     // DPR
            public readonly Vector2Int PhysicalPixels;  // CSS × DPR
            public readonly bool IsPortrait;
            public readonly bool SupportsWebGPU;
            public readonly string DisplayName;

            public ViewportSpec(int cssW, int cssH, float dpr, bool webgpu, string name)
            {
                CSSPixels = new Vector2Int(cssW, cssH);
                DevicePixelRatio = dpr;
                PhysicalPixels = new Vector2Int(
                    Mathf.RoundToInt(cssW * dpr),
                    Mathf.RoundToInt(cssH * dpr));
                IsPortrait = cssH > cssW;
                SupportsWebGPU = webgpu;
                DisplayName = name;
            }
        }

        // ── Profile Database ────────────────────────────────────────────────

        public static ViewportSpec GetProfile(MobileProfile profile) => profile switch
        {
            MobileProfile.Pixel8        => new ViewportSpec(412, 915, 2.625f, true,  "Pixel 8"),
            MobileProfile.GalaxyS24     => new ViewportSpec(360, 780, 3.0f,   true,  "Galaxy S24"),
            MobileProfile.iPhone15Pro   => new ViewportSpec(393, 852, 3.0f,   true,  "iPhone 15 Pro"),
            MobileProfile.iPadPro       => new ViewportSpec(1024, 1366, 2.0f, true,  "iPad Pro 12.9\""),
            MobileProfile.DesktopChrome => new ViewportSpec(1920, 1080, 1.0f, true,  "Desktop Chrome"),
            _                           => new ViewportSpec(360, 800, 2.0f,   false, "Generic Mobile")
        };

        // ── State ───────────────────────────────────────────────────────────

        /// <summary>Active mobile profile.</summary>
        public MobileProfile ActiveProfile { get; private set; }

        /// <summary>Active viewport spec.</summary>
        public ViewportSpec ActiveSpec { get; private set; }

        /// <summary>Grid cell size adjusted for DPR.</summary>
        public int AdjustedCellSize { get; private set; }

        /// <summary>Whether WebGPU is available (vs WebGL fallback).</summary>
        public bool WebGPUAvailable { get; private set; }

        // ── Constructor ─────────────────────────────────────────────────────

        public WebGPUBridge(MobileProfile profile = MobileProfile.GenericMobile, int baseCellSize = 8)
        {
            SetProfile(profile, baseCellSize);
        }

        // ── Profile Configuration ───────────────────────────────────────────

        public void SetProfile(MobileProfile profile, int baseCellSize = 8)
        {
            ActiveProfile = profile;
            ActiveSpec = GetProfile(profile);
            WebGPUAvailable = ActiveSpec.SupportsWebGPU;

            // DPR compensation: scale cell size to maintain perceived visual size
            // At DPR 3.0, a CSS 8px cell is 24 physical pixels — too large
            // We use the CSS pixel grid, but validate against physical bounds
            AdjustedCellSize = Mathf.Max(4, Mathf.RoundToInt(baseCellSize / ActiveSpec.DevicePixelRatio));

            // Ensure cell size stays power-of-2 aligned for optimal rendering
            AdjustedCellSize = NearestPowerOfTwo(AdjustedCellSize);
        }

        // ── Grid Factory ────────────────────────────────────────────────────

        /// <summary>
        /// Creates a DeterministicGrid sized for the mobile viewport.
        /// Uses CSS pixels (logical) — the rendering pipeline handles DPR scaling.
        /// </summary>
        public DeterministicGrid CreateMobileGrid()
        {
            int cols = ActiveSpec.CSSPixels.x / AdjustedCellSize;
            int rows = ActiveSpec.CSSPixels.y / AdjustedCellSize;
            return new DeterministicGrid(AdjustedCellSize, cols, rows);
        }

        // ── Touch-Grid Alignment ────────────────────────────────────────────

        /// <summary>
        /// Converts a touch event position (CSS pixels) to the nearest
        /// grid-aligned anchor point. Accounts for touch target minimum
        /// size (48px CSS = Apple/Google accessibility guideline).
        /// </summary>
        /// <param name="touchCSS">Touch position in CSS pixels.</param>
        /// <param name="grid">Grid to snap to.</param>
        /// <returns>Grid-aligned position suitable for anchor placement.</returns>
        public Vector2Int TouchToGrid(Vector2 touchCSS, DeterministicGrid grid)
        {
            // Clamp to viewport
            float x = Mathf.Clamp(touchCSS.x, 0, ActiveSpec.CSSPixels.x);
            float y = Mathf.Clamp(touchCSS.y, 0, ActiveSpec.CSSPixels.y);

            return grid.Snap(new Vector2(x, y));
        }

        /// <summary>
        /// Returns the minimum touch target size in grid cells.
        /// Based on 48dp (CSS px) accessibility minimum.
        /// </summary>
        public int MinTouchTargetCells =>
            Mathf.Max(1, Mathf.CeilToInt(48f / AdjustedCellSize));

        // ── Canvas Resolution ───────────────────────────────────────────────

        /// <summary>
        /// Returns the physical pixel resolution to set on the WebGPU canvas.
        /// This ensures crisp rendering on high-DPR screens.
        /// </summary>
        public Vector2Int GetCanvasResolution() => ActiveSpec.PhysicalPixels;

        /// <summary>
        /// Returns the CSS pixel viewport (for layout calculations).
        /// </summary>
        public Vector2Int GetLogicalViewport() => ActiveSpec.CSSPixels;

        // ── WebGL Fallback ──────────────────────────────────────────────────

        /// <summary>
        /// Returns recommended render scale for WebGL fallback
        /// (reduced resolution for older browsers without WebGPU).
        /// </summary>
        public float GetWebGLFallbackScale()
        {
            if (WebGPUAvailable) return 1.0f;

            // Scale down based on physical pixel count to maintain framerate
            long totalPixels = (long)ActiveSpec.PhysicalPixels.x * ActiveSpec.PhysicalPixels.y;
            if (totalPixels > 4_000_000) return 0.5f;  // 4MP+ → half res
            if (totalPixels > 2_000_000) return 0.75f;  // 2-4MP → 75%
            return 1.0f;
        }

        // ── Utility ─────────────────────────────────────────────────────────

        private static int NearestPowerOfTwo(int value)
        {
            int power = 1;
            while (power < value) power <<= 1;
            // Return nearest: either power or power/2
            return (power - value) < (value - power / 2) ? power : power / 2;
        }

        public override string ToString() =>
            $"[WebGPU] {ActiveSpec.DisplayName}: {ActiveSpec.CSSPixels.x}×{ActiveSpec.CSSPixels.y} CSS " +
            $"({ActiveSpec.PhysicalPixels.x}×{ActiveSpec.PhysicalPixels.y} physical) | " +
            $"DPR: {ActiveSpec.DevicePixelRatio} | Cell: {AdjustedCellSize}px | " +
            $"WebGPU: {WebGPUAvailable} | Portrait: {ActiveSpec.IsPortrait}";
    }
}
