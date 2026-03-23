// =============================================================================
// PXG // HARD-GRID COORDINATE SYSTEM (HGCS)
// Test: Resolution Stress Test
// Corresponds to RESOLUTION_STRESS_TEST_FAILSAFE_100% from poster diagram.
// Operational Order: PXG-2026-SOMU-0325
// =============================================================================

using NUnit.Framework;
using UnityEngine;

namespace PXG.HGCS.Tests
{
    /// <summary>
    /// Stress-tests the HGCS pipeline at extreme resolutions to verify the
    /// failsafe condition: 100% of anchors must remain grid-aligned after
    /// scaling across the full resolution range (Mobile WebGPU → 8K).
    ///
    /// Poster diagram ref: RESOLUTION_STRESS_TEST_FAILSAFE_100%
    /// </summary>
    [TestFixture]
    public class ResolutionStressTest
    {
        private DeterministicGrid _grid;
        private MultiResolutionScaler _scaler;
        private SanityCheck _checker;

        [SetUp]
        public void SetUp()
        {
            _grid = new DeterministicGrid(cellSize: 8, columns: 240, rows: 135);
            _scaler = new MultiResolutionScaler(1920, 1080);
            _checker = new SanityCheck(_grid);
        }

        [Test]
        public void AllProfiles_MaintainGridAlignment()
        {
            // Create reference anchors at known grid positions
            var anchors = new[]
            {
                AnchorPoint.CreateSystemAnchor(1, new Vector2(0, 0)),
                AnchorPoint.CreateSystemAnchor(2, new Vector2(960, 540)),
                AnchorPoint.CreateSystemAnchor(3, new Vector2(1912, 1072)),
                AnchorPoint.CreateSystemAnchor(4, new Vector2(480, 270)),
            };

            foreach (var profile in System.Enum.GetValues(typeof(MultiResolutionScaler.Profile)))
            {
                _scaler.SetProfile((MultiResolutionScaler.Profile)profile);

                foreach (var anchor in anchors)
                {
                    Vector2Int scaled = _scaler.ScalePosition(anchor.Position);
                    Vector2Int snapped = _grid.Snap(scaled);

                    Assert.AreEqual(scaled, snapped,
                        $"Resolution drift detected at profile {profile} for {anchor.Id}: " +
                        $"scaled=({scaled.x},{scaled.y}) snapped=({snapped.x},{snapped.y})");
                }
            }
        }

        [Test]
        public void Failsafe_100Percent_NoAnchorDrift()
        {
            // Simulate 1000 random anchors and verify 100% grid alignment
            var anchors = new AnchorPoint[1000];
            for (int i = 0; i < anchors.Length; i++)
            {
                int x = Random.Range(0, 240) * 8;  // pre-snapped
                int y = Random.Range(0, 135) * 8;
                anchors[i] = new AnchorPoint($"STRESS_{i:D4}", new Vector2(x, y));
            }

            var results = _checker.RunAll(anchors);
            int failCount = 0;
            foreach (var r in results)
            {
                if (r.Severity == SanityCheck.Severity.Fail)
                    failCount++;
            }

            Assert.AreEqual(0, failCount,
                $"FAILSAFE VIOLATED: {failCount} anchors drifted from grid alignment.");
        }

        [Test]
        public void Extreme8K_GridSnap_Preserves_Integrity()
        {
            _scaler.SetProfile(MultiResolutionScaler.Profile.UHD_8K);

            // At 8K (7680×4320), grid cell size scales to 8 * 4 = 32px
            Vector2Int pos = _scaler.ScalePosition(new Vector2(100, 100));
            Vector2Int snapped = _grid.Snap(pos);

            Assert.AreEqual(snapped.x, pos.x, 8,
                "8K scaled position should be within 1 cell of snapped position.");
        }

        [Test]
        public void MobileWebGPU_MinimumSize_Preserved()
        {
            _scaler.SetProfile(MultiResolutionScaler.Profile.MobileWebGPU);

            // Ensure no element collapses to zero size
            Vector2Int size = _scaler.ScaleSize(new Vector2(8, 8));
            Assert.GreaterOrEqual(size.x, 1, "Width must be at least 1px on mobile.");
            Assert.GreaterOrEqual(size.y, 1, "Height must be at least 1px on mobile.");
        }
    }
}
