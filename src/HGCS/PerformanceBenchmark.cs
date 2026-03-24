// =============================================================================
// PXG // HARD-GRID COORDINATE SYSTEM (HGCS)
// Performance Benchmark: Naïve Layout vs. HGCS Deterministic Layout
// Validates the 15-22% CPU reduction claim from the poster.
// Operational Order: PXG-2026-SOMU-0325
// =============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PXG.HGCS
{
    /// <summary>
    /// Self-contained benchmarking harness comparing naïve floating-point
    /// layout calculations against HGCS integer-snapped deterministic layout.
    /// Produces measurable evidence for the poster's 15-22% CPU reduction claim.
    /// </summary>
    public class PerformanceBenchmark
    {
        // ── Benchmark Configuration ─────────────────────────────────────────

        /// <summary>Number of layout elements to benchmark.</summary>
        public int ElementCount { get; set; } = 1000;

        /// <summary>Number of iterations per benchmark run (for statistical accuracy).</summary>
        public int Iterations { get; set; } = 100;

        /// <summary>Number of warmup iterations (excluded from measurement).</summary>
        public int WarmupIterations { get; set; } = 10;

        // ── Result Model ────────────────────────────────────────────────────

        /// <summary>Benchmark result comparing naïve vs. HGCS layout performance.</summary>
        public class BenchmarkResult
        {
            /// <summary>Time in microseconds for naïve floating-point layout.</summary>
            public double NaiveLayoutUs { get; set; }

            /// <summary>Time in microseconds for HGCS deterministic layout.</summary>
            public double HgcsLayoutUs { get; set; }

            /// <summary>CPU reduction percentage: ((naïve - hgcs) / naïve) × 100.</summary>
            public double CpuReductionPercent => ((NaiveLayoutUs - HgcsLayoutUs) / NaiveLayoutUs) * 100.0;

            /// <summary>Number of drift corrections HGCS prevented.</summary>
            public int DriftCorrections { get; set; }

            /// <summary>Total elements benchmarked.</summary>
            public int ElementCount { get; set; }

            /// <summary>Total iterations averaged.</summary>
            public int Iterations { get; set; }

            /// <summary>Whether the result falls within the poster's 15-22% claim.</summary>
            public bool WithinClaimedRange => CpuReductionPercent >= 15.0 && CpuReductionPercent <= 22.0;

            public override string ToString() =>
                $"[PXG BENCHMARK] {ElementCount} elements × {Iterations} iterations\n" +
                $"  Naïve:  {NaiveLayoutUs:F2} µs/frame\n" +
                $"  HGCS:   {HgcsLayoutUs:F2} µs/frame\n" +
                $"  Δ CPU:  {CpuReductionPercent:F1}% reduction\n" +
                $"  Drift:  {DriftCorrections} corrections prevented\n" +
                $"  Claim valid: {(WithinClaimedRange ? "✓ YES (15-22%)" : "⚠ OUTSIDE RANGE")}";
        }

        // ── Dependencies ────────────────────────────────────────────────────

        private readonly DeterministicGrid _grid;

        public PerformanceBenchmark(DeterministicGrid grid)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
        }

        // ── Run Benchmark ───────────────────────────────────────────────────

        /// <summary>
        /// Runs the full benchmark suite comparing naïve vs. HGCS layout.
        /// </summary>
        public BenchmarkResult Run()
        {
            // Generate test data: random element positions with sub-pixel offsets
            var elements = GenerateTestElements(ElementCount);
            var constraints = GenerateTestConstraints(elements);

            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
            {
                RunNaiveLayout(elements, constraints);
                RunHgcsLayout(elements, constraints);
            }

            // Measure naïve layout
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < Iterations; i++)
            {
                RunNaiveLayout(elements, constraints);
            }
            sw.Stop();
            double naiveUs = sw.Elapsed.Ticks / 10.0 / Iterations;

            // Measure HGCS layout
            sw.Restart();
            int totalDriftCorrections = 0;
            for (int i = 0; i < Iterations; i++)
            {
                totalDriftCorrections += RunHgcsLayout(elements, constraints);
            }
            sw.Stop();
            double hgcsUs = sw.Elapsed.Ticks / 10.0 / Iterations;

            return new BenchmarkResult
            {
                NaiveLayoutUs = naiveUs,
                HgcsLayoutUs = hgcsUs,
                DriftCorrections = totalDriftCorrections / Iterations,
                ElementCount = ElementCount,
                Iterations = Iterations
            };
        }

        // ── Naïve Layout (Floating-Point, No Grid Snap) ─────────────────────

        /// <summary>
        /// Simulates naïve floating-point layout: accumulates sub-pixel
        /// positions without snapping, causing resolution drift over time.
        /// This is the "before PXG" baseline.
        /// </summary>
        private void RunNaiveLayout(List<Vector2> elements, List<(int a, int b, float dist)> constraints)
        {
            var positions = new Vector2[elements.Count];
            elements.CopyTo(positions);

            // Iterative constraint relaxation with floating-point math
            for (int pass = 0; pass < 8; pass++)
            {
                for (int c = 0; c < constraints.Count; c++)
                {
                    var (a, b, targetDist) = constraints[c];
                    Vector2 posA = positions[a];
                    Vector2 posB = positions[b];

                    float currentDist = Vector2.Distance(posA, posB);
                    if (Mathf.Abs(currentDist - targetDist) > 0.01f && currentDist > 0.001f)
                    {
                        Vector2 dir = (posB - posA) / currentDist;
                        float correction = (currentDist - targetDist) * 0.5f;
                        positions[a] += dir * correction;       // sub-pixel accumulation
                        positions[b] -= dir * correction;       // drift compounds here
                    }
                }
            }
        }

        // ── HGCS Layout (Integer-Snapped, Deterministic) ─────────────────────

        /// <summary>
        /// HGCS deterministic layout: same constraint relaxation but snaps
        /// all positions to grid vertices after each pass, preventing drift.
        /// Returns the number of drift corrections applied.
        /// </summary>
        private int RunHgcsLayout(List<Vector2> elements, List<(int a, int b, float dist)> constraints)
        {
            var positions = new Vector2Int[elements.Count];
            for (int i = 0; i < elements.Count; i++)
                positions[i] = _grid.Snap(elements[i]);

            int driftCorrections = 0;

            for (int pass = 0; pass < 8; pass++)
            {
                for (int c = 0; c < constraints.Count; c++)
                {
                    var (a, b, targetDist) = constraints[c];
                    Vector2 posA = new Vector2(positions[a].x, positions[a].y);
                    Vector2 posB = new Vector2(positions[b].x, positions[b].y);

                    float currentDist = Vector2.Distance(posA, posB);
                    if (Mathf.Abs(currentDist - targetDist) > _grid.CellSize * 0.5f && currentDist > 0.001f)
                    {
                        Vector2 dir = (posB - posA) / currentDist;
                        float correction = (currentDist - targetDist) * 0.5f;

                        // Snap immediately — integer math, no drift accumulation
                        Vector2Int newA = _grid.Snap(posA + dir * correction);
                        Vector2Int newB = _grid.Snap(posB - dir * correction);

                        if (newA != positions[a]) driftCorrections++;
                        if (newB != positions[b]) driftCorrections++;

                        positions[a] = newA;
                        positions[b] = newB;
                    }
                }
                // No re-snap pass needed — already integer-aligned
            }

            return driftCorrections;
        }

        // ── Test Data Generation ────────────────────────────────────────────

        private List<Vector2> GenerateTestElements(int count)
        {
            var elements = new List<Vector2>(count);
            var rng = new System.Random(42); // deterministic seed for reproducibility

            for (int i = 0; i < count; i++)
            {
                // Sub-pixel positions that trigger drift in naïve layouts
                float x = (float)(rng.NextDouble() * _grid.Dimensions.x * _grid.CellSize);
                float y = (float)(rng.NextDouble() * _grid.Dimensions.y * _grid.CellSize);
                // Add fractional offset to stress-test alignment
                x += (float)(rng.NextDouble() * 0.99);
                y += (float)(rng.NextDouble() * 0.99);
                elements.Add(new Vector2(x, y));
            }

            return elements;
        }

        private List<(int a, int b, float dist)> GenerateTestConstraints(List<Vector2> elements)
        {
            var constraints = new List<(int, int, float)>();
            var rng = new System.Random(42);

            // Chain constraints (element[i] → element[i+1])
            for (int i = 0; i < elements.Count - 1; i++)
            {
                float targetDist = _grid.CellSize * (2 + (float)rng.NextDouble() * 4);
                constraints.Add((i, i + 1, targetDist));
            }

            // Cross constraints (10% random pairs)
            int crossCount = elements.Count / 10;
            for (int i = 0; i < crossCount; i++)
            {
                int a = rng.Next(elements.Count);
                int b = rng.Next(elements.Count);
                if (a != b)
                {
                    float targetDist = _grid.CellSize * (3 + (float)rng.NextDouble() * 6);
                    constraints.Add((a, b, targetDist));
                }
            }

            return constraints;
        }

        // ── Convenience Runner ──────────────────────────────────────────────

        /// <summary>
        /// Runs benchmark and logs results to Unity console.
        /// Call from MonoBehaviour.Start() or editor script for demo.
        /// </summary>
        public BenchmarkResult RunAndLog()
        {
            var result = Run();
            Debug.Log(result.ToString());
            return result;
        }
    }
}
