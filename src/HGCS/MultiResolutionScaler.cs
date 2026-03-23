// =============================================================================
// PXG // HARD-GRID COORDINATE SYSTEM (HGCS)
// Spec 2: Multi-Resolution
// Cross-compatible from 8K simulation to Mobile WebGPU runtimes.
// Operational Order: PXG-2026-SOMU-0325
// =============================================================================

using System;
using UnityEngine;

namespace PXG.HGCS
{
    /// <summary>
    /// Handles resolution-independent scaling so that a single HGCS layout
    /// renders identically across the full hardware spectrum—from 8K
    /// simulation displays (7680×4320) down to Mobile WebGPU runtimes.
    ///
    /// Poster ref: "Cross-compatible from 8K simulation to Mobile WebGPU
    /// runtimes."
    /// </summary>
    public class MultiResolutionScaler
    {
        // ── Resolution Profiles ─────────────────────────────────────────────

        /// <summary>Known target resolution profiles.</summary>
        public enum Profile
        {
            /// <summary>7680 × 4320 — research simulation displays.</summary>
            UHD_8K,

            /// <summary>3840 × 2160 — standard 4K monitors.</summary>
            UHD_4K,

            /// <summary>1920 × 1080 — baseline desktop.</summary>
            FHD_1080p,

            /// <summary>Variable — mobile WebGPU browser runtimes.</summary>
            MobileWebGPU,

            /// <summary>Variable — XR / head-mounted displays.</summary>
            XR_HMD
        }

        // ── Configuration ───────────────────────────────────────────────────

        /// <summary>Reference resolution the layout was authored at.</summary>
        public Vector2Int ReferenceResolution { get; }

        /// <summary>Current target resolution.</summary>
        public Vector2Int TargetResolution { get; private set; }

        /// <summary>Active scaling profile.</summary>
        public Profile ActiveProfile { get; private set; }

        /// <summary>Computed scale factor (target / reference).</summary>
        public float ScaleFactor => TargetResolution.x / (float)ReferenceResolution.x;

        // ── Constructor ─────────────────────────────────────────────────────

        /// <param name="referenceWidth">Reference design width in pixels.</param>
        /// <param name="referenceHeight">Reference design height in pixels.</param>
        public MultiResolutionScaler(int referenceWidth = 1920, int referenceHeight = 1080)
        {
            ReferenceResolution = new Vector2Int(referenceWidth, referenceHeight);
            TargetResolution = ReferenceResolution;
            ActiveProfile = Profile.FHD_1080p;
        }

        // ── Profile Switching ───────────────────────────────────────────────

        /// <summary>
        /// Switches to a named profile, updating target resolution and scale.
        /// </summary>
        public void SetProfile(Profile profile)
        {
            ActiveProfile = profile;
            TargetResolution = profile switch
            {
                Profile.UHD_8K       => new Vector2Int(7680, 4320),
                Profile.UHD_4K       => new Vector2Int(3840, 2160),
                Profile.FHD_1080p    => new Vector2Int(1920, 1080),
                Profile.MobileWebGPU => new Vector2Int(412, 915),   // Pixel-class baseline
                Profile.XR_HMD      => new Vector2Int(2064, 2208),  // Quest 3 per-eye
                _ => ReferenceResolution
            };
        }

        /// <summary>
        /// Sets a custom XR per-eye resolution for specific headsets.
        /// </summary>
        public void SetXRProfile(int perEyeWidth, int perEyeHeight)
        {
            ActiveProfile = Profile.XR_HMD;
            TargetResolution = new Vector2Int(perEyeWidth, perEyeHeight);
        }

        /// <summary>Shortcut: Apple Vision Pro (3660×3200 per eye).</summary>
        public void SetAppleVisionPro() => SetXRProfile(3660, 3200);

        /// <summary>Shortcut: Meta Quest Pro (1920×1800 per eye).</summary>
        public void SetMetaQuestPro() => SetXRProfile(1920, 1800);

        /// <summary>Shortcut: WebGPU Tablet (iPad Pro 1024×1366 CSS).</summary>
        public void SetWebGPUTablet()
        {
            ActiveProfile = Profile.MobileWebGPU;
            TargetResolution = new Vector2Int(1024, 1366);
        }

        /// <summary>
        /// Sets an explicit target resolution (for non-standard displays).
        /// </summary>
        public void SetCustomResolution(int width, int height)
        {
            TargetResolution = new Vector2Int(width, height);
            ActiveProfile = Profile.FHD_1080p; // custom override
        }

        // ── Coordinate Scaling ──────────────────────────────────────────────

        /// <summary>
        /// Scales a reference-space position to the current target resolution,
        /// snapping to the nearest integer to preserve grid alignment.
        /// </summary>
        public Vector2Int ScalePosition(Vector2 referencePosition)
        {
            float sx = referencePosition.x * ScaleFactor;
            float sy = referencePosition.y * (TargetResolution.y / (float)ReferenceResolution.y);
            return new Vector2Int(Mathf.RoundToInt(sx), Mathf.RoundToInt(sy));
        }

        /// <summary>
        /// Scales a reference-space size to the current target, preserving
        /// minimum 1px to avoid zero-sized elements on small screens.
        /// </summary>
        public Vector2Int ScaleSize(Vector2 referenceSize)
        {
            int w = Mathf.Max(1, Mathf.RoundToInt(referenceSize.x * ScaleFactor));
            int h = Mathf.Max(1, Mathf.RoundToInt(referenceSize.y * (TargetResolution.y / (float)ReferenceResolution.y)));
            return new Vector2Int(w, h);
        }

        // ── Poster Dimension Mapping ────────────────────────────────────────

        /// <summary>
        /// Maps poster physical dimensions (inches) to pixel coordinates.
        /// Poster shows two panels: 18×32" and 20×32".
        /// </summary>
        /// <param name="widthInches">Physical width in inches.</param>
        /// <param name="heightInches">Physical height in inches.</param>
        /// <param name="dpi">Target DPI (default 150 for large-format print).</param>
        public Vector2Int PhysicalToPixel(float widthInches, float heightInches, int dpi = 150)
        {
            return new Vector2Int(
                Mathf.RoundToInt(widthInches * dpi),
                Mathf.RoundToInt(heightInches * dpi)
            );
        }
    }
}
