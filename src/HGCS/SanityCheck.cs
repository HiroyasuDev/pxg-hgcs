// =============================================================================
// PXG // HARD-GRID COORDINATE SYSTEM (HGCS)
// Automated UI "Sanity Checks"
// Operational Order: PXG-2026-SOMU-0325
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace PXG.HGCS
{
    /// <summary>
    /// Automated UI validation system. Runs deterministic "Sanity Checks"
    /// against the solved layout to detect resolution drift, alignment
    /// violations, and constraint breakage before rendering.
    ///
    /// Poster ref: "By leveraging Unity 6's CoreCLR performance boost (a 4x
    /// increase), PXG runs automated UI 'Sanity Checks.' This stabilizes
    /// interfaces for complex research data visualization across Astronomy,
    /// Health, and Ocean sciences."
    /// </summary>
    public class SanityCheck
    {
        // ── Result Types ────────────────────────────────────────────────────

        /// <summary>Severity level for a sanity check finding.</summary>
        public enum Severity
        {
            Pass,
            Warning,
            Fail
        }

        /// <summary>A single sanity check result.</summary>
        public readonly struct CheckResult
        {
            public readonly string CheckName;
            public readonly Severity Severity;
            public readonly string Message;
            public readonly string AnchorId;

            public CheckResult(string checkName, Severity severity, string message, string anchorId = null)
            {
                CheckName = checkName;
                Severity = severity;
                Message = message;
                AnchorId = anchorId;
            }

            public override string ToString() =>
                $"[{Severity}] {CheckName}: {Message}" + (AnchorId != null ? $" (anchor: {AnchorId})" : "");
        }

        // ── Dependencies ────────────────────────────────────────────────────

        private readonly DeterministicGrid _grid;

        // ── Constructor ─────────────────────────────────────────────────────

        public SanityCheck(DeterministicGrid grid)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
        }

        // ── Full Check Suite ────────────────────────────────────────────────

        /// <summary>
        /// Runs the complete sanity check suite against a set of anchors.
        /// Returns all findings (pass, warning, and fail).
        /// Corresponds to RESOLUTION_STRESS_TEST_FAILSAFE_100% from poster.
        /// </summary>
        public List<CheckResult> RunAll(IReadOnlyList<AnchorPoint> anchors)
        {
            var results = new List<CheckResult>();

            results.AddRange(CheckGridAlignment(anchors));
            results.AddRange(CheckBoundsViolations(anchors));
            results.AddRange(CheckOverlaps(anchors));
            results.AddRange(CheckScaleConsistency(anchors));

            return results;
        }

        // ── Individual Checks ───────────────────────────────────────────────

        /// <summary>
        /// Verifies all anchors lie exactly on grid vertices (no resolution drift).
        /// </summary>
        public List<CheckResult> CheckGridAlignment(IReadOnlyList<AnchorPoint> anchors)
        {
            var results = new List<CheckResult>();
            foreach (var anchor in anchors)
            {
                bool aligned = _grid.IsAligned(anchor.Position);
                results.Add(new CheckResult(
                    "GridAlignment",
                    aligned ? Severity.Pass : Severity.Fail,
                    aligned
                        ? $"Anchor is grid-aligned at ({anchor.Position.x}, {anchor.Position.y})"
                        : $"DRIFT DETECTED — position ({anchor.Position.x:F2}, {anchor.Position.y:F2}) is not grid-aligned",
                    anchor.Id
                ));
            }
            return results;
        }

        /// <summary>
        /// Checks whether any anchor falls outside the grid bounds.
        /// </summary>
        public List<CheckResult> CheckBoundsViolations(IReadOnlyList<AnchorPoint> anchors)
        {
            var results = new List<CheckResult>();
            int maxX = _grid.Dimensions.x * _grid.CellSize + _grid.Origin.x;
            int maxY = _grid.Dimensions.y * _grid.CellSize + _grid.Origin.y;

            foreach (var anchor in anchors)
            {
                bool inBounds = anchor.Position.x >= _grid.Origin.x
                             && anchor.Position.y >= _grid.Origin.y
                             && anchor.Position.x <= maxX
                             && anchor.Position.y <= maxY;

                results.Add(new CheckResult(
                    "BoundsCheck",
                    inBounds ? Severity.Pass : Severity.Warning,
                    inBounds
                        ? "Within grid bounds"
                        : $"Out of bounds at ({anchor.Position.x}, {anchor.Position.y})",
                    anchor.Id
                ));
            }
            return results;
        }

        /// <summary>
        /// Detects overlapping anchors (same grid cell, different IDs).
        /// </summary>
        public List<CheckResult> CheckOverlaps(IReadOnlyList<AnchorPoint> anchors)
        {
            var results = new List<CheckResult>();
            var occupied = new Dictionary<Vector2Int, string>();

            foreach (var anchor in anchors)
            {
                Vector2Int cell = _grid.WorldToCell(anchor.Position);
                if (occupied.TryGetValue(cell, out string existingId))
                {
                    results.Add(new CheckResult(
                        "OverlapDetection",
                        Severity.Warning,
                        $"Overlaps with '{existingId}' at cell ({cell.x}, {cell.y})",
                        anchor.Id
                    ));
                }
                else
                {
                    occupied[cell] = anchor.Id;
                    results.Add(new CheckResult(
                        "OverlapDetection",
                        Severity.Pass,
                        $"No overlap at cell ({cell.x}, {cell.y})",
                        anchor.Id
                    ));
                }
            }
            return results;
        }

        /// <summary>
        /// Validates that scale factors are within acceptable range.
        /// </summary>
        public List<CheckResult> CheckScaleConsistency(IReadOnlyList<AnchorPoint> anchors)
        {
            var results = new List<CheckResult>();
            const float minScale = 0.1f;
            const float maxScale = 10.0f;

            foreach (var anchor in anchors)
            {
                bool valid = anchor.Scale >= minScale && anchor.Scale <= maxScale;
                results.Add(new CheckResult(
                    "ScaleConsistency",
                    valid ? Severity.Pass : Severity.Fail,
                    valid
                        ? $"Scale {anchor.Scale:F2} within range [{minScale}–{maxScale}]"
                        : $"Scale {anchor.Scale:F2} OUT OF RANGE [{minScale}–{maxScale}]",
                    anchor.Id
                ));
            }
            return results;
        }
    }
}
