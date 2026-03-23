// =============================================================================
// PXG // HARD-GRID COORDINATE SYSTEM (HGCS)
// Spec 4: Sentis Integration
// Hook points for 2026-era AI-suggested auto-layout refinement.
// Operational Order: PXG-2026-SOMU-0325
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using PXG.HGCS.AI;

namespace PXG.HGCS
{
    /// <summary>
    /// Integration layer between Unity Sentis (neural-network inference) and
    /// the HGCS constraint solver. Provides hook points for AI-suggested
    /// auto-layout refinement at runtime.
    ///
    /// Poster ref: "79% of large teams see major efficiency gains (optimized
    /// for PXG Sentis integration)."
    ///
    /// The AI model suggests layout adjustments (anchor repositioning,
    /// constraint relaxation) which are then validated and grid-snapped by
    /// the deterministic pipeline before application.
    /// </summary>
    public class SentisIntegration
    {
        // ── Suggestion Model ────────────────────────────────────────────────

        /// <summary>An AI-generated layout suggestion.</summary>
        public readonly struct LayoutSuggestion
        {
            /// <summary>Target anchor to reposition.</summary>
            public readonly string AnchorId;

            /// <summary>Suggested delta in logical pixels.</summary>
            public readonly Vector2 SuggestedDelta;

            /// <summary>Confidence score [0.0 – 1.0] from the Sentis model.</summary>
            public readonly float Confidence;

            /// <summary>Reason tag for telemetry (e.g. "overlap_reduction").</summary>
            public readonly string Reason;

            public LayoutSuggestion(string anchorId, Vector2 delta, float confidence, string reason)
            {
                AnchorId = anchorId;
                SuggestedDelta = delta;
                Confidence = Mathf.Clamp01(confidence);
                Reason = reason;
            }
        }

        // ── Configuration ───────────────────────────────────────────────────

        /// <summary>Minimum confidence threshold to accept a suggestion.</summary>
        public float ConfidenceThreshold { get; set; } = 0.75f;

        /// <summary>Maximum delta magnitude (pixels) the AI may suggest.</summary>
        public float MaxDeltaMagnitude { get; set; } = 64f;

        /// <summary>Whether Sentis inference is currently enabled.</summary>
        public bool Enabled { get; set; } = true;

        // ── Dependencies ────────────────────────────────────────────────────

        private readonly ConstraintSolver _solver;
        private readonly DeterministicGrid _grid;
        private SentisModelLoader _modelLoader;
        private LayoutOptimizationModel _modelContract;

        // ── Telemetry ───────────────────────────────────────────────────────

        /// <summary>Cumulative suggestions accepted across all cycles.</summary>
        public int TotalAccepted { get; private set; }

        /// <summary>Cumulative suggestions rejected across all cycles.</summary>
        public int TotalRejected { get; private set; }

        /// <summary>Total inference cycles executed.</summary>
        public int InferenceCycles { get; private set; }

        // ── Events ──────────────────────────────────────────────────────────

        /// <summary>Fired when a suggestion is accepted and applied.</summary>
        public event Action<LayoutSuggestion> OnSuggestionApplied;

        /// <summary>Fired when a suggestion is rejected (low confidence or out of bounds).</summary>
        public event Action<LayoutSuggestion, string> OnSuggestionRejected;

        // ── Constructor ─────────────────────────────────────────────────────

        public SentisIntegration(ConstraintSolver solver, DeterministicGrid grid)
        {
            _solver = solver ?? throw new ArgumentNullException(nameof(solver));
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
        }

        /// <summary>Attaches a loaded Sentis model for real inference.</summary>
        public void AttachModel(SentisModelLoader loader, LayoutOptimizationModel contract)
        {
            _modelLoader = loader;
            _modelContract = contract;
        }

        // ── Suggestion Pipeline ─────────────────────────────────────────────

        /// <summary>
        /// Processes a batch of AI-generated layout suggestions.
        /// Each suggestion is validated, grid-snapped, and applied only if
        /// it passes confidence and magnitude checks.
        /// </summary>
        /// <returns>Number of suggestions accepted.</returns>
        public int ProcessSuggestions(IReadOnlyList<LayoutSuggestion> suggestions)
        {
            if (!Enabled) return 0;

            int accepted = 0;

            foreach (var suggestion in suggestions)
            {
                if (suggestion.Confidence < ConfidenceThreshold)
                {
                    OnSuggestionRejected?.Invoke(suggestion, "below_confidence_threshold");
                    continue;
                }

                if (suggestion.SuggestedDelta.magnitude > MaxDeltaMagnitude)
                {
                    OnSuggestionRejected?.Invoke(suggestion, "exceeds_max_delta");
                    continue;
                }

                // Apply delta, then re-snap to grid for determinism
                try
                {
                    Vector2Int currentPos = _solver.GetAnchorPosition(suggestion.AnchorId);
                    Vector2 newPos = new Vector2(currentPos.x, currentPos.y) + suggestion.SuggestedDelta;
                    Vector2Int snapped = _grid.Snap(newPos);

                    // Apply snapped position back through solver
                    _solver.SetAnchorPosition(suggestion.AnchorId, snapped);

                    accepted++;
                    TotalAccepted++;
                    OnSuggestionApplied?.Invoke(suggestion);
                }
                catch (KeyNotFoundException)
                {
                    TotalRejected++;
                    OnSuggestionRejected?.Invoke(suggestion, "anchor_not_found");
                }
            }

            // Re-solve constraints after all accepted suggestions
            if (accepted > 0)
            {
                _solver.Solve();
            }

            return accepted;
        }

        // ── Inference Delegation ─────────────────────────────────────────────

        /// <summary>
        /// Runs inference via the attached Sentis model. If no model is
        /// attached, returns an empty list.
        /// </summary>
        /// <param name="anchors">Current layout anchors.</param>
        /// <param name="viewportSize">Viewport for normalization.</param>
        /// <returns>List of AI-generated layout suggestions.</returns>
        public List<LayoutSuggestion> RunInference(
            IReadOnlyList<AnchorPoint> anchors = null,
            Vector2Int viewportSize = default)
        {
            InferenceCycles++;

            if (_modelLoader == null || !_modelLoader.IsLoaded || _modelContract == null)
            {
                Debug.Log("[PXG.HGCS] SentisIntegration: No model attached — returning empty.");
                return new List<LayoutSuggestion>();
            }

            if (anchors == null || anchors.Count == 0)
            {
                Debug.LogWarning("[PXG.HGCS] SentisIntegration: No anchors provided.");
                return new List<LayoutSuggestion>();
            }

            if (viewportSize == default)
                viewportSize = new Vector2Int(1920, 1080);

            // Serialize → Infer → Deserialize
            float[] input = _modelContract.SerializeInput(anchors, viewportSize);
            float[] output = _modelLoader.Execute(input);
            return _modelContract.DeserializeOutput(output, anchors, viewportSize);
        }

        // ── Telemetry ───────────────────────────────────────────────────────

        /// <summary>Returns a formatted telemetry string for demo dashboards.</summary>
        public string GetTelemetry() =>
            $"[PXG AI] Cycles: {InferenceCycles} | " +
            $"Accepted: {TotalAccepted} | Rejected: {TotalRejected} | " +
            $"Rate: {(TotalAccepted + TotalRejected > 0 ? TotalAccepted / (float)(TotalAccepted + TotalRejected) * 100f : 0f):F1}%";
    }
}
