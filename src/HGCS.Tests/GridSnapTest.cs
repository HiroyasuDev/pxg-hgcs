// =============================================================================
// PXG // HARD-GRID COORDINATE SYSTEM (HGCS)
// Test: Grid Snap
// Corresponds to DETERMINISTIC_GRID_SNAP_v2.1 from poster diagram.
// Operational Order: PXG-2026-SOMU-0325
// =============================================================================

using NUnit.Framework;
using UnityEngine;

namespace PXG.HGCS.Tests
{
    /// <summary>
    /// Validates the deterministic grid-snapping algorithm (version 2.1).
    /// Ensures that arbitrary floating-point positions always resolve to
    /// the same integer grid vertex, regardless of input precision.
    ///
    /// Poster diagram ref: DETERMINISTIC_GRID_SNAP_v2.1
    /// (appears twice — one per monitor panel, confirming cross-display
    /// determinism)
    /// </summary>
    [TestFixture]
    public class GridSnapTest
    {
        private DeterministicGrid _grid;

        [SetUp]
        public void SetUp()
        {
            // 8px cell grid, 240 columns × 135 rows = 1920×1080 logical space
            _grid = new DeterministicGrid(cellSize: 8, columns: 240, rows: 135);
        }

        // ── Determinism Tests ───────────────────────────────────────────────

        [Test]
        public void Snap_IsDeterministic_SameInputSameOutput()
        {
            Vector2 pos = new Vector2(123.456f, 789.012f);
            Vector2Int result1 = _grid.Snap(pos);
            Vector2Int result2 = _grid.Snap(pos);

            Assert.AreEqual(result1, result2,
                "Identical inputs must produce identical snap results.");
        }

        [Test]
        public void Snap_AlwaysProducesIntegerCoordinates()
        {
            // Test a range of floating-point inputs
            for (float x = -100f; x <= 2020f; x += 3.7f)
            {
                for (float y = -50f; y <= 1130f; y += 5.3f)
                {
                    Vector2Int snapped = _grid.Snap(new Vector2(x, y));
                    Assert.AreEqual(0, snapped.x % _grid.CellSize,
                        $"X coordinate {snapped.x} is not grid-aligned (cell={_grid.CellSize})");
                    Assert.AreEqual(0, snapped.y % _grid.CellSize,
                        $"Y coordinate {snapped.y} is not grid-aligned (cell={_grid.CellSize})");
                }
            }
        }

        // ── Boundary Tests ──────────────────────────────────────────────────

        [Test]
        public void Snap_ExactGridVertex_ReturnsIdentity()
        {
            Vector2 exact = new Vector2(96, 64); // exactly 12*8, 8*8
            Vector2Int snapped = _grid.Snap(exact);

            Assert.AreEqual(96, snapped.x);
            Assert.AreEqual(64, snapped.y);
        }

        [Test]
        public void Snap_Midpoint_RoundsToNearest()
        {
            // Midpoint between cells: 4px from both 0 and 8
            Vector2 mid = new Vector2(4f, 4f);
            Vector2Int snapped = _grid.Snap(mid);

            // RoundToInt rounds 0.5 to nearest even — 4/8 = 0.5 → snaps to 0 or 8
            Assert.IsTrue(snapped.x == 0 || snapped.x == 8,
                $"Midpoint snap should go to 0 or 8, got {snapped.x}");
        }

        [Test]
        public void Snap_NearVertex_SnapsCorrectly()
        {
            // 1 pixel away from grid vertex 96
            Assert.AreEqual(96, _grid.Snap(new Vector2(95f, 0)).x,
                "95 should snap to 96 (closer than 92)");
            Assert.AreEqual(96, _grid.Snap(new Vector2(97f, 0)).x,
                "97 should snap to 96 (closer than 100)");
        }

        // ── Cross-Display Determinism ───────────────────────────────────────

        [Test]
        public void CrossDisplay_SameGrid_SameResults()
        {
            // Poster shows DETERMINISTIC_GRID_SNAP_v2.1 on BOTH monitors:
            // 18×32" panel and 20×32" panel must produce identical snaps
            var grid18 = new DeterministicGrid(8, 240, 135);
            var grid20 = new DeterministicGrid(8, 240, 135);

            Vector2 testPos = new Vector2(777.77f, 333.33f);

            Assert.AreEqual(
                grid18.Snap(testPos),
                grid20.Snap(testPos),
                "Cross-display snap must be identical for same grid config.");
        }

        // ── Origin Rebasing ─────────────────────────────────────────────────

        [Test]
        public void Snap_WithRebasedOrigin_StillAligns()
        {
            _grid.SetOrigin(new Vector2Int(10, 10));
            Vector2Int snapped = _grid.Snap(new Vector2(25f, 25f));

            int relX = snapped.x - 10;
            int relY = snapped.y - 10;
            Assert.AreEqual(0, relX % _grid.CellSize,
                "Rebased snap must align relative to new origin.");
            Assert.AreEqual(0, relY % _grid.CellSize,
                "Rebased snap must align relative to new origin.");
        }

        // ── IsAligned Validation ────────────────────────────────────────────

        [Test]
        public void IsAligned_TrueForGridVertex()
        {
            Assert.IsTrue(_grid.IsAligned(new Vector2(0, 0)));
            Assert.IsTrue(_grid.IsAligned(new Vector2(8, 16)));
            Assert.IsTrue(_grid.IsAligned(new Vector2(1920, 1080)));
        }

        [Test]
        public void IsAligned_FalseForDriftedPosition()
        {
            Assert.IsFalse(_grid.IsAligned(new Vector2(1.5f, 0)));
            Assert.IsFalse(_grid.IsAligned(new Vector2(0, 3.7f)));
            Assert.IsFalse(_grid.IsAligned(new Vector2(99.9f, 99.9f)));
        }
    }
}
