// =============================================================================
// PXG // HARD-GRID COORDINATE SYSTEM (HGCS)
// Data Structure: AnchorPoint
// Represents ANCHOR_PT references visible in the poster architecture diagram.
// Operational Order: PXG-2026-SOMU-0325
// =============================================================================

using UnityEngine;

namespace PXG.HGCS
{
    /// <summary>
    /// Represents a named anchor point within the HGCS grid. Anchor points
    /// are the fundamental positioning primitives—all UI elements reference
    /// one or more anchors for deterministic placement.
    ///
    /// Poster diagram refs: ANCHOR_PT_04 (appears 4 times across both
    /// monitor panels, indicating shared anchor registration across displays).
    /// </summary>
    public class AnchorPoint
    {
        // ── Identity ────────────────────────────────────────────────────────

        /// <summary>
        /// Unique identifier for this anchor (e.g. "ANCHOR_PT_04").
        /// Convention: ANCHOR_PT_{index} for system anchors.
        /// </summary>
        public string Id { get; }

        // ── Spatial ─────────────────────────────────────────────────────────

        /// <summary>
        /// Current position in grid-space (integer-aligned after solve).
        /// Mutable during constraint solving; read-only after solve completes.
        /// </summary>
        public Vector2 Position { get; set; }

        /// <summary>
        /// Scale factor at this anchor (1.0 = reference scale).
        /// Used by <see cref="MultiResolutionScaler"/> when adapting layouts.
        /// </summary>
        public float Scale { get; set; }

        // ── Metadata ────────────────────────────────────────────────────────

        /// <summary>
        /// Whether this anchor is locked (cannot be moved by solver or AI).
        /// Locked anchors act as fixed reference points for constraint solving.
        /// </summary>
        public bool IsLocked { get; set; }

        /// <summary>
        /// Optional display index for the multi-monitor setup.
        /// Poster shows anchors shared across 18×32" and 20×32" panels.
        /// -1 = shared across all displays.
        /// </summary>
        public int DisplayIndex { get; set; } = -1;

        // ── Constructor ─────────────────────────────────────────────────────

        /// <summary>Creates a new anchor point.</summary>
        /// <param name="id">Unique identifier.</param>
        /// <param name="position">Initial position in grid-space.</param>
        /// <param name="scale">Scale factor (default 1.0).</param>
        public AnchorPoint(string id, Vector2 position, float scale = 1.0f)
        {
            Id = id;
            Position = position;
            Scale = scale;
            IsLocked = false;
        }

        // ── Factory Methods ─────────────────────────────────────────────────

        /// <summary>
        /// Creates a system anchor matching the poster's ANCHOR_PT_{index}
        /// naming convention.
        /// </summary>
        public static AnchorPoint CreateSystemAnchor(int index, Vector2 position)
        {
            return new AnchorPoint($"ANCHOR_PT_{index:D2}", position)
            {
                IsLocked = true,
                DisplayIndex = -1  // shared across displays
            };
        }

        // ── Debug ───────────────────────────────────────────────────────────

        public override string ToString()
        {
            string lockTag = IsLocked ? " [LOCKED]" : "";
            return $"{Id} @ ({Position.x:F0}, {Position.y:F0}) ×{Scale:F2}{lockTag}";
        }
    }
}
