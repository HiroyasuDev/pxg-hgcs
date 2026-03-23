// =============================================================================
// PXG // HARD-GRID COORDINATE SYSTEM (HGCS)
// AI Module: Layout Optimization Model I/O Contract
// Defines the neural network input/output tensor schema.
// Operational Order: PXG-2026-SOMU-0325
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace PXG.HGCS.AI
{
    /// <summary>
    /// Defines the neural network I/O contract for the HGCS layout
    /// optimization model. Handles serialization of anchor layouts into
    /// input tensors and deserialization of output tensors into actionable
    /// <see cref="SentisIntegration.LayoutSuggestion"/> instances.
    ///
    /// Input tensor:  [batch=1, anchors=N, features=5]
    ///   Features: x, y, scale, isLocked(0/1), displayIndex
    ///
    /// Output tensor: [batch=1, anchors=N, deltas=3]
    ///   Deltas: dx, dy, confidence
    /// </summary>
    public class LayoutOptimizationModel
    {
        // ── Constants ───────────────────────────────────────────────────────

        /// <summary>Features per anchor in the input tensor.</summary>
        public const int FeaturesPerAnchor = 5;

        /// <summary>Output values per anchor in the output tensor.</summary>
        public const int DeltasPerAnchor = 3;

        /// <summary>Maximum supported anchor count.</summary>
        public int MaxAnchors { get; }

        // ── Constructor ─────────────────────────────────────────────────────

        public LayoutOptimizationModel(int maxAnchors = 256)
        {
            MaxAnchors = maxAnchors;
        }

        // ── Input Serialization ─────────────────────────────────────────────

        /// <summary>
        /// Serializes a set of anchor points into a flat float array for
        /// Sentis tensor input.
        ///
        /// Layout per anchor (5 floats):
        ///   [0] x position (normalized to viewport width)
        ///   [1] y position (normalized to viewport height)
        ///   [2] scale factor
        ///   [3] isLocked (0.0 or 1.0)
        ///   [4] displayIndex (normalized)
        /// </summary>
        /// <param name="anchors">Anchor points to serialize.</param>
        /// <param name="viewportSize">Viewport dimensions for normalization.</param>
        /// <returns>Flattened float array [MaxAnchors × 5].</returns>
        public float[] SerializeInput(IReadOnlyList<AnchorPoint> anchors, Vector2Int viewportSize)
        {
            float[] input = new float[MaxAnchors * FeaturesPerAnchor];
            float invW = viewportSize.x > 0 ? 1f / viewportSize.x : 1f;
            float invH = viewportSize.y > 0 ? 1f / viewportSize.y : 1f;

            int count = Math.Min(anchors.Count, MaxAnchors);
            for (int i = 0; i < count; i++)
            {
                int baseIdx = i * FeaturesPerAnchor;
                var anchor = anchors[i];

                input[baseIdx + 0] = anchor.Position.x * invW;  // normalized x
                input[baseIdx + 1] = anchor.Position.y * invH;  // normalized y
                input[baseIdx + 2] = anchor.Scale;                // scale
                input[baseIdx + 3] = anchor.IsLocked ? 1f : 0f;  // locked flag
                input[baseIdx + 4] = anchor.DisplayIndex >= 0
                    ? anchor.DisplayIndex / 4f : 0f;              // display (max 4)
            }

            // Remaining slots are zero-padded (masked out in attention)
            return input;
        }

        // ── Output Deserialization ──────────────────────────────────────────

        /// <summary>
        /// Deserializes model output tensor into layout suggestions.
        /// Only produces suggestions for anchors that actually exist
        /// (skips zero-padded slots).
        ///
        /// Output per anchor (3 floats):
        ///   [0] dx (normalized delta, multiply by viewport width)
        ///   [1] dy (normalized delta, multiply by viewport height)
        ///   [2] confidence [0.0 – 1.0]
        /// </summary>
        /// <param name="outputData">Raw model output [MaxAnchors × 3].</param>
        /// <param name="anchors">Original anchor list for ID lookup.</param>
        /// <param name="viewportSize">Viewport for denormalization.</param>
        /// <param name="minConfidence">Skip suggestions below this threshold.</param>
        /// <returns>List of actionable layout suggestions.</returns>
        public List<SentisIntegration.LayoutSuggestion> DeserializeOutput(
            float[] outputData,
            IReadOnlyList<AnchorPoint> anchors,
            Vector2Int viewportSize,
            float minConfidence = 0.1f)
        {
            var suggestions = new List<SentisIntegration.LayoutSuggestion>();
            int count = Math.Min(anchors.Count, MaxAnchors);

            for (int i = 0; i < count; i++)
            {
                int baseIdx = i * DeltasPerAnchor;
                float dx = outputData[baseIdx + 0] * viewportSize.x;
                float dy = outputData[baseIdx + 1] * viewportSize.y;
                float confidence = Mathf.Clamp01(outputData[baseIdx + 2]);

                // Skip low-confidence or zero-magnitude suggestions
                if (confidence < minConfidence)
                    continue;

                if (Mathf.Abs(dx) < 0.01f && Mathf.Abs(dy) < 0.01f)
                    continue;

                // Skip locked anchors — model shouldn't suggest moving them
                if (anchors[i].IsLocked)
                    continue;

                suggestions.Add(new SentisIntegration.LayoutSuggestion(
                    anchors[i].Id,
                    new Vector2(dx, dy),
                    confidence,
                    ClassifyReason(dx, dy, confidence)
                ));
            }

            return suggestions;
        }

        // ── Reason Classification ───────────────────────────────────────────

        /// <summary>
        /// Classifies the AI suggestion into a human-readable reason tag
        /// based on delta characteristics.
        /// </summary>
        private string ClassifyReason(float dx, float dy, float confidence)
        {
            float magnitude = Mathf.Sqrt(dx * dx + dy * dy);

            if (magnitude < 4f)
                return "micro_alignment";
            else if (confidence > 0.9f && magnitude > 16f)
                return "overlap_reduction";
            else if (Mathf.Abs(dx) > Mathf.Abs(dy) * 3f)
                return "horizontal_balance";
            else if (Mathf.Abs(dy) > Mathf.Abs(dx) * 3f)
                return "vertical_balance";
            else
                return "layout_optimization";
        }

        // ── Constraint Graph Encoding ───────────────────────────────────────

        /// <summary>
        /// Encodes the constraint graph as an adjacency feature for advanced
        /// graph neural network models. Returns [MaxAnchors × MaxAnchors]
        /// adjacency matrix (1.0 = connected, 0.0 = not connected).
        /// </summary>
        public float[] EncodeConstraintGraph(
            IReadOnlyList<ConstraintSolver.Constraint> constraints,
            IReadOnlyList<AnchorPoint> anchors)
        {
            float[] adjacency = new float[MaxAnchors * MaxAnchors];
            var idToIndex = new Dictionary<string, int>();

            for (int i = 0; i < Math.Min(anchors.Count, MaxAnchors); i++)
                idToIndex[anchors[i].Id] = i;

            foreach (var constraint in constraints)
            {
                if (idToIndex.TryGetValue(constraint.AnchorA, out int a) &&
                    idToIndex.TryGetValue(constraint.AnchorB, out int b))
                {
                    adjacency[a * MaxAnchors + b] = 1f;
                    adjacency[b * MaxAnchors + a] = 1f; // undirected
                }
            }

            return adjacency;
        }
    }
}
