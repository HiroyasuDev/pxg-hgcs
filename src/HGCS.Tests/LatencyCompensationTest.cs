// =============================================================================
// PXG // HARD-GRID COORDINATE SYSTEM (HGCS)
// Test: Latency Compensation Protocol
// Corresponds to LATENCY_COMPENSATION_PROTOCOL_04 from poster diagram.
// Operational Order: PXG-2026-SOMU-0325
// =============================================================================

using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace PXG.HGCS.Tests
{
    /// <summary>
    /// Validates that the HGCS pipeline meets latency targets under load.
    /// "Protocol 04" implies the 4th iteration of the latency compensation
    /// algorithm, optimized for CoreCLR's 4x performance boost.
    ///
    /// Poster diagram ref: LATENCY_COMPENSATION_PROTOCOL_04
    /// (appears twice — once per monitor panel, indicating per-display
    /// latency budgeting)
    /// </summary>
    [TestFixture]
    public class LatencyCompensationTest
    {
        private DeterministicGrid _grid;
        private ConstraintSolver _solver;

        /// <summary>Maximum acceptable solve time in milliseconds.</summary>
        private const double MaxSolveTimeMs = 16.67; // one 60fps frame budget

        [SetUp]
        public void SetUp()
        {
            _grid = new DeterministicGrid(cellSize: 8, columns: 240, rows: 135);
            _solver = new ConstraintSolver(_grid);
        }

        [Test]
        public void ConstraintSolve_UnderFrameBudget_SmallLayout()
        {
            // 20 anchors with chained distance constraints
            for (int i = 0; i < 20; i++)
            {
                _solver.AddAnchor(new AnchorPoint($"LAT_{i:D2}", new Vector2(i * 96, i * 54)));
            }

            for (int i = 0; i < 19; i++)
            {
                _solver.AddConstraint(new ConstraintSolver.Constraint(
                    $"LAT_{i:D2}", $"LAT_{i + 1:D2}",
                    ConstraintSolver.ConstraintType.FixedDistance, 110f
                ));
            }

            var sw = Stopwatch.StartNew();
            bool converged = _solver.Solve();
            sw.Stop();

            Assert.IsTrue(converged, "Solver should converge for a 20-anchor chain.");
            Assert.Less(sw.Elapsed.TotalMilliseconds, MaxSolveTimeMs,
                $"Solve took {sw.Elapsed.TotalMilliseconds:F2}ms, exceeds {MaxSolveTimeMs}ms frame budget.");
        }

        [Test]
        public void ConstraintSolve_UnderFrameBudget_LargeLayout()
        {
            // 200 anchors — stress test for CoreCLR optimization
            for (int i = 0; i < 200; i++)
            {
                _solver.AddAnchor(new AnchorPoint($"LAT_{i:D3}", new Vector2(
                    (i % 20) * 96,
                    (i / 20) * 108
                )));
            }

            // Horizontal alignment constraints per row
            for (int row = 0; row < 10; row++)
            {
                for (int col = 0; col < 19; col++)
                {
                    int a = row * 20 + col;
                    int b = row * 20 + col + 1;
                    _solver.AddConstraint(new ConstraintSolver.Constraint(
                        $"LAT_{a:D3}", $"LAT_{b:D3}",
                        ConstraintSolver.ConstraintType.AlignHorizontal
                    ));
                }
            }

            var sw = Stopwatch.StartNew();
            bool converged = _solver.Solve();
            sw.Stop();

            Assert.IsTrue(converged, "Solver should converge for a 200-anchor grid.");
            // Allow 2x frame budget for large layouts
            Assert.Less(sw.Elapsed.TotalMilliseconds, MaxSolveTimeMs * 2,
                $"Large layout solve took {sw.Elapsed.TotalMilliseconds:F2}ms.");
        }

        [Test]
        public void GridSnap_Latency_Negligible()
        {
            // 10,000 snap operations should be well under 1ms
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 10_000; i++)
            {
                _grid.Snap(new Vector2(i * 0.7f, i * 0.3f));
            }
            sw.Stop();

            Assert.Less(sw.Elapsed.TotalMilliseconds, 1.0,
                $"10K snap operations took {sw.Elapsed.TotalMilliseconds:F3}ms, expected < 1ms.");
        }
    }
}
