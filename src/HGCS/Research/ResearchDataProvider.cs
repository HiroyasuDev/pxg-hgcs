// =============================================================================
// PXG // HARD-GRID COORDINATE SYSTEM (HGCS)
// Research Module: Base Data Provider + Factory
// Maps domain-specific research data to HGCS anchor grids.
// Operational Order: PXG-2026-SOMU-0325
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace PXG.HGCS.Research
{
    /// <summary>
    /// Abstract base class for domain-specific research data providers.
    /// Each provider maps research data (columns, metrics, spatial fields)
    /// to HGCS anchor grids for deterministic visualization.
    ///
    /// Poster ref: "This stabilizes interfaces for complex research data
    /// visualization across Astronomy, Health, and Ocean sciences."
    /// </summary>
    public abstract class ResearchDataProvider
    {
        // ── Data Record ─────────────────────────────────────────────────────

        /// <summary>A single data point mapped to the HGCS grid.</summary>
        public class DataPoint
        {
            /// <summary>Unique identifier for this data point.</summary>
            public string Id { get; set; }

            /// <summary>Grid position (snapped to HGCS).</summary>
            public Vector2Int GridPosition { get; set; }

            /// <summary>Display label.</summary>
            public string Label { get; set; }

            /// <summary>Primary value for visualization (size, color, etc.).</summary>
            public float Value { get; set; }

            /// <summary>Secondary value (optional).</summary>
            public float SecondaryValue { get; set; }

            /// <summary>Category tag for filtering/coloring.</summary>
            public string Category { get; set; }

            /// <summary>Metadata key-value pairs.</summary>
            public Dictionary<string, string> Metadata { get; set; } = new();
        }

        // ── Panel Layout ────────────────────────────────────────────────────

        /// <summary>A named panel region within the HGCS grid.</summary>
        public class PanelRegion
        {
            public string Name { get; set; }
            public Vector2Int Origin { get; set; }
            public Vector2Int Size { get; set; }
            public List<AnchorPoint> Anchors { get; set; } = new();

            public Rect ToRect() => new Rect(Origin.x, Origin.y, Size.x, Size.y);
        }

        // ── Abstract Interface ──────────────────────────────────────────────

        /// <summary>Short name for this research domain.</summary>
        public abstract string DomainName { get; }

        /// <summary>Unicode icon for UI display.</summary>
        public abstract string DomainIcon { get; }

        /// <summary>
        /// Generates a set of data points mapped to the given grid.
        /// </summary>
        public abstract List<DataPoint> GenerateDataPoints(DeterministicGrid grid, int count);

        /// <summary>
        /// Creates panel regions for a multi-panel dashboard layout
        /// within the given grid dimensions.
        /// </summary>
        public abstract List<PanelRegion> CreateDashboardLayout(DeterministicGrid grid);

        /// <summary>
        /// Converts data points into anchor points registered on the grid.
        /// </summary>
        public virtual List<AnchorPoint> MapToAnchors(DeterministicGrid grid, List<DataPoint> dataPoints)
        {
            var anchors = new List<AnchorPoint>();
            foreach (var dp in dataPoints)
            {
                var anchor = new AnchorPoint(
                    $"{DomainName}_{dp.Id}",
                    new Vector2(dp.GridPosition.x, dp.GridPosition.y),
                    Mathf.Clamp(dp.Value, 0.1f, 5.0f) // scale by value
                );
                anchor.DisplayIndex = -1;
                anchors.Add(anchor);
            }
            return anchors;
        }

        // ── Factory ─────────────────────────────────────────────────────────

        /// <summary>
        /// Factory method to create a provider by domain name.
        /// </summary>
        public static ResearchDataProvider Create(string domain)
        {
            return domain.ToLower() switch
            {
                "astronomy" or "astro" => new AstronomyDataProvider(),
                "health" or "cancer"   => new HealthDataProvider(),
                "ocean" or "marine"    => new OceanScienceDataProvider(),
                _ => throw new ArgumentException($"Unknown research domain: {domain}")
            };
        }

        /// <summary>Returns all available domain names.</summary>
        public static string[] AvailableDomains => new[] { "Astronomy", "Health", "Ocean" };
    }
}
