// =============================================================================
// PXG // HARD-GRID COORDINATE SYSTEM (HGCS)
// Research Module: Astronomy Data Provider
// Star catalog plotting, telescope FOV, spectral data panel layout.
// Operational Order: PXG-2026-SOMU-0325
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace PXG.HGCS.Research
{
    /// <summary>
    /// Astronomy data visualization provider for UH research workflows.
    /// Maps stellar catalog data, telescope fields of view, and spectral
    /// analysis panels to the HGCS deterministic grid.
    ///
    /// Relevant UH assets: Subaru Telescope, Pan-STARRS, IfA Haleakalā,
    /// Maunakea Observatories.
    /// </summary>
    public class AstronomyDataProvider : ResearchDataProvider
    {
        public override string DomainName => "Astronomy";
        public override string DomainIcon => "🔭";

        // ── Spectral Classes ────────────────────────────────────────────────

        private static readonly string[] SpectralClasses = { "O", "B", "A", "F", "G", "K", "M" };
        private static readonly float[] ClassTemperatures = { 40000, 20000, 8500, 6500, 5500, 4000, 3000 };

        // ── Star Catalog Data ───────────────────────────────────────────────

        /// <summary>
        /// Generates simulated star catalog data points mapped to the HGCS grid.
        /// Each star is positioned based on RA/Dec converted to grid coordinates.
        /// </summary>
        public override List<DataPoint> GenerateDataPoints(DeterministicGrid grid, int count)
        {
            var points = new List<DataPoint>();
            var rng = new System.Random(42); // reproducible for demo

            int gridW = grid.Dimensions.x * grid.CellSize;
            int gridH = grid.Dimensions.y * grid.CellSize;

            for (int i = 0; i < count; i++)
            {
                // Simulate RA/Dec to grid mapping
                float ra = (float)(rng.NextDouble() * 360.0);     // 0–360°
                float dec = (float)(rng.NextDouble() * 180.0 - 90.0); // -90° to +90°

                // Map to grid coordinates
                int x = Mathf.RoundToInt((ra / 360f) * gridW);
                int y = Mathf.RoundToInt(((dec + 90f) / 180f) * gridH);

                // Assign spectral class
                int classIdx = rng.Next(SpectralClasses.Length);
                float magnitude = (float)(rng.NextDouble() * 10.0 + 1.0);

                var dp = new DataPoint
                {
                    Id = $"STAR_{i:D4}",
                    GridPosition = grid.Snap(new Vector2(x, y)),
                    Label = $"{SpectralClasses[classIdx]}{rng.Next(10)} ({magnitude:F1}m)",
                    Value = Mathf.Clamp(11f - magnitude, 0.5f, 5f), // brighter = larger
                    SecondaryValue = ClassTemperatures[classIdx],
                    Category = SpectralClasses[classIdx],
                    Metadata =
                    {
                        ["ra"] = $"{ra:F4}°",
                        ["dec"] = $"{dec:F4}°",
                        ["magnitude"] = $"{magnitude:F2}",
                        ["temperature_K"] = $"{ClassTemperatures[classIdx]:F0}",
                        ["telescope"] = rng.Next(2) == 0 ? "Subaru" : "Pan-STARRS"
                    }
                };

                points.Add(dp);
            }

            return points;
        }

        // ── Dashboard Layout ────────────────────────────────────────────────

        /// <summary>
        /// Creates a multi-panel astronomy dashboard:
        /// - Sky Map (main, 60% width)
        /// - Spectral Analysis (right, 40% width, top half)
        /// - Photometry Timeline (right, 40% width, bottom half)
        /// - Telescope Status Bar (bottom strip)
        /// </summary>
        public override List<PanelRegion> CreateDashboardLayout(DeterministicGrid grid)
        {
            int totalW = grid.Dimensions.x * grid.CellSize;
            int totalH = grid.Dimensions.y * grid.CellSize;

            int mainW = Mathf.RoundToInt(totalW * 0.6f);
            int sideW = totalW - mainW;
            int halfH = Mathf.RoundToInt(totalH * 0.45f);
            int barH = totalH - halfH * 2;

            return new List<PanelRegion>
            {
                new PanelRegion
                {
                    Name = "Sky Map",
                    Origin = grid.Snap(Vector2.zero),
                    Size = new Vector2Int(mainW, totalH - barH)
                },
                new PanelRegion
                {
                    Name = "Spectral Analysis",
                    Origin = grid.Snap(new Vector2(mainW, 0)),
                    Size = new Vector2Int(sideW, halfH)
                },
                new PanelRegion
                {
                    Name = "Photometry Timeline",
                    Origin = grid.Snap(new Vector2(mainW, halfH)),
                    Size = new Vector2Int(sideW, halfH)
                },
                new PanelRegion
                {
                    Name = "Telescope Status",
                    Origin = grid.Snap(new Vector2(0, totalH - barH)),
                    Size = new Vector2Int(totalW, barH)
                }
            };
        }

        // ── Telescope FOV Mapping ───────────────────────────────────────────

        /// <summary>
        /// Maps a telescope's field of view to a grid region.
        /// </summary>
        /// <param name="grid">HGCS grid.</param>
        /// <param name="centerRA">Center RA in degrees.</param>
        /// <param name="centerDec">Center Dec in degrees.</param>
        /// <param name="fovArcmin">Field of view in arcminutes.</param>
        /// <param name="telescopeName">Instrument name.</param>
        public PanelRegion CreateTelescopeFOV(
            DeterministicGrid grid,
            float centerRA, float centerDec,
            float fovArcmin, string telescopeName)
        {
            int gridW = grid.Dimensions.x * grid.CellSize;
            int gridH = grid.Dimensions.y * grid.CellSize;

            // Convert RA/Dec + FOV to grid region
            float degreesPerPixelX = 360f / gridW;
            float degreesPerPixelY = 180f / gridH;
            float fovDegrees = fovArcmin / 60f;

            int fovPixelsX = Mathf.RoundToInt(fovDegrees / degreesPerPixelX);
            int fovPixelsY = Mathf.RoundToInt(fovDegrees / degreesPerPixelY);

            int cx = Mathf.RoundToInt((centerRA / 360f) * gridW);
            int cy = Mathf.RoundToInt(((centerDec + 90f) / 180f) * gridH);

            return new PanelRegion
            {
                Name = $"{telescopeName} FOV",
                Origin = grid.Snap(new Vector2(cx - fovPixelsX / 2, cy - fovPixelsY / 2)),
                Size = new Vector2Int(
                    Mathf.Max(grid.CellSize, fovPixelsX),
                    Mathf.Max(grid.CellSize, fovPixelsY))
            };
        }
    }
}
