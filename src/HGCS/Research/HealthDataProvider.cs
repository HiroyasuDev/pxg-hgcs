// =============================================================================
// PXG // HARD-GRID COORDINATE SYSTEM (HGCS)
// Research Module: Health / Cancer Data Provider
// Cohort data grids, biomarker heatmaps, clinical trial timelines.
// Operational Order: PXG-2026-SOMU-0325
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace PXG.HGCS.Research
{
    /// <summary>
    /// Health and cancer research data visualization provider aligned with
    /// UH Cancer Center workflows. Maps cohort demographics, biomarker
    /// intensity data, and clinical trial timelines to HGCS grids.
    ///
    /// Relevant UH programs: Population Sciences in the Pacific,
    /// UH Cancer Center (UHCC), Multiethnic Cohort Study.
    /// </summary>
    public class HealthDataProvider : ResearchDataProvider
    {
        public override string DomainName => "Health";
        public override string DomainIcon => "🏥";

        // ── Cancer Types ────────────────────────────────────────────────────

        private static readonly string[] CancerTypes = {
            "Breast", "Lung", "Colorectal", "Prostate", "Liver",
            "Stomach", "Thyroid", "Pancreatic", "Melanoma", "Lymphoma"
        };

        private static readonly string[] Ethnicities = {
            "Native Hawaiian", "Filipino", "Japanese", "Chinese",
            "Korean", "Samoan", "Other Pacific Islander", "White"
        };

        private static readonly string[] Biomarkers = {
            "CA-125", "PSA", "CEA", "AFP", "CA 19-9",
            "HER2", "BRCA1", "Ki-67", "PD-L1", "TP53"
        };

        // ── Cohort Data Generation ──────────────────────────────────────────

        /// <summary>
        /// Generates simulated cohort data points for cancer research
        /// visualization. Each point represents a patient record with
        /// demographics and biomarker data mapped to the grid.
        /// </summary>
        public override List<DataPoint> GenerateDataPoints(DeterministicGrid grid, int count)
        {
            var points = new List<DataPoint>();
            var rng = new System.Random(42);

            int gridW = grid.Dimensions.x * grid.CellSize;
            int gridH = grid.Dimensions.y * grid.CellSize;

            for (int i = 0; i < count; i++)
            {
                string cancerType = CancerTypes[rng.Next(CancerTypes.Length)];
                string ethnicity = Ethnicities[rng.Next(Ethnicities.Length)];
                string biomarker = Biomarkers[rng.Next(Biomarkers.Length)];

                int age = rng.Next(30, 85);
                float biomarkerLevel = (float)(rng.NextDouble() * 100.0);
                int survivalMonths = rng.Next(1, 120);

                // Map: X = age range (30–85 → grid width), Y = biomarker level (0–100 → grid height)
                int x = Mathf.RoundToInt(((age - 30f) / 55f) * gridW);
                int y = Mathf.RoundToInt((biomarkerLevel / 100f) * gridH);

                var dp = new DataPoint
                {
                    Id = $"PT_{i:D5}",
                    GridPosition = grid.Snap(new Vector2(x, y)),
                    Label = $"{cancerType} | {ethnicity} | Age {age}",
                    Value = Mathf.Clamp(biomarkerLevel / 25f, 0.5f, 4f), // scale by severity
                    SecondaryValue = survivalMonths,
                    Category = cancerType,
                    Metadata =
                    {
                        ["cancer_type"] = cancerType,
                        ["ethnicity"] = ethnicity,
                        ["age"] = age.ToString(),
                        ["biomarker"] = biomarker,
                        ["biomarker_level"] = $"{biomarkerLevel:F1}",
                        ["survival_months"] = survivalMonths.ToString(),
                        ["cohort"] = "Multiethnic Cohort"
                    }
                };

                points.Add(dp);
            }

            return points;
        }

        // ── Dashboard Layout ────────────────────────────────────────────────

        /// <summary>
        /// Creates a multi-panel health research dashboard:
        /// - Cohort Scatter (main, top-left 50%)
        /// - Biomarker Heatmap (top-right 50%)
        /// - Survival Curve Timeline (bottom, full width)
        /// - Demographics Summary Bar (right strip)
        /// </summary>
        public override List<PanelRegion> CreateDashboardLayout(DeterministicGrid grid)
        {
            int totalW = grid.Dimensions.x * grid.CellSize;
            int totalH = grid.Dimensions.y * grid.CellSize;

            int halfW = totalW / 2;
            int topH = Mathf.RoundToInt(totalH * 0.55f);
            int bottomH = Mathf.RoundToInt(totalH * 0.3f);
            int barH = totalH - topH - bottomH;

            return new List<PanelRegion>
            {
                new PanelRegion
                {
                    Name = "Cohort Scatter Plot",
                    Origin = grid.Snap(Vector2.zero),
                    Size = new Vector2Int(halfW, topH)
                },
                new PanelRegion
                {
                    Name = "Biomarker Heatmap",
                    Origin = grid.Snap(new Vector2(halfW, 0)),
                    Size = new Vector2Int(halfW, topH)
                },
                new PanelRegion
                {
                    Name = "Survival Curve",
                    Origin = grid.Snap(new Vector2(0, topH)),
                    Size = new Vector2Int(totalW, bottomH)
                },
                new PanelRegion
                {
                    Name = "Demographics Summary",
                    Origin = grid.Snap(new Vector2(0, topH + bottomH)),
                    Size = new Vector2Int(totalW, barH)
                }
            };
        }

        // ── Heatmap Grid ────────────────────────────────────────────────────

        /// <summary>
        /// Creates a biomarker × cancer-type heatmap grid with intensity values.
        /// Returns a 2D array [biomarker_index, cancer_type_index] of normalized intensities.
        /// </summary>
        public float[,] GenerateBiomarkerHeatmap()
        {
            var heatmap = new float[Biomarkers.Length, CancerTypes.Length];
            var rng = new System.Random(42);

            for (int b = 0; b < Biomarkers.Length; b++)
            {
                for (int c = 0; c < CancerTypes.Length; c++)
                {
                    // Simulated correlation strengths
                    heatmap[b, c] = (float)rng.NextDouble();
                }
            }

            return heatmap;
        }

        /// <summary>Returns the biomarker names for heatmap row labels.</summary>
        public string[] GetBiomarkerLabels() => Biomarkers;

        /// <summary>Returns the cancer type names for heatmap column labels.</summary>
        public string[] GetCancerTypeLabels() => CancerTypes;

        /// <summary>Returns the ethnicity categories for demographic breakdowns.</summary>
        public string[] GetEthnicityLabels() => Ethnicities;
    }
}
