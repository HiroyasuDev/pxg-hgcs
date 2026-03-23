// =============================================================================
// PXG // HARD-GRID COORDINATE SYSTEM (HGCS)
// Runtime Profiler: Grid-Snap Operation Timing
// Operational Order: PXG-2026-SOMU-0325
// =============================================================================

using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PXG.HGCS
{
    /// <summary>
    /// Runtime profiler that wraps grid-snap operations with precise timing.
    /// Tracks per-frame averages, peaks, and cumulative overhead for live
    /// demo readouts and poster presentation dashboards.
    /// </summary>
    public class GridSnapProfiler
    {
        // ── Timing State ────────────────────────────────────────────────────

        private readonly Stopwatch _stopwatch = new();
        private long _totalTicks;
        private long _frameTicks;
        private int _totalOps;
        private int _frameOps;
        private int _frameCount;
        private long _peakFrameTicks;

        // ── Configuration ───────────────────────────────────────────────────

        /// <summary>Whether profiling is active. Disable for production builds.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Rolling window size for average calculation.</summary>
        public int RollingWindowSize { get; set; } = 60;

        // ── Rolling Window ──────────────────────────────────────────────────

        private readonly long[] _frameTickHistory;
        private readonly int[] _frameOpHistory;
        private int _historyIndex;

        // ── Metrics (Read-Only) ─────────────────────────────────────────────

        /// <summary>Total snap operations since profiler creation.</summary>
        public int TotalOperations => _totalOps;

        /// <summary>Average microseconds per snap operation (all-time).</summary>
        public double AverageSnapUs => _totalOps > 0
            ? (_totalTicks / (double)Stopwatch.Frequency) * 1_000_000.0 / _totalOps
            : 0.0;

        /// <summary>Peak frame time in microseconds.</summary>
        public double PeakFrameUs => (_peakFrameTicks / (double)Stopwatch.Frequency) * 1_000_000.0;

        /// <summary>Rolling average frame time in microseconds.</summary>
        public double RollingAverageFrameUs
        {
            get
            {
                int count = Math.Min(_frameCount, RollingWindowSize);
                if (count == 0) return 0;
                long sum = 0;
                for (int i = 0; i < count; i++)
                    sum += _frameTickHistory[i];
                return (sum / (double)Stopwatch.Frequency) * 1_000_000.0 / count;
            }
        }

        /// <summary>Rolling average snap operations per frame.</summary>
        public double RollingAverageOpsPerFrame
        {
            get
            {
                int count = Math.Min(_frameCount, RollingWindowSize);
                if (count == 0) return 0;
                long sum = 0;
                for (int i = 0; i < count; i++)
                    sum += _frameOpHistory[i];
                return sum / (double)count;
            }
        }

        /// <summary>Estimated CPU overhead as percentage of 16.67ms frame budget.</summary>
        public double CpuOverheadPercent => (RollingAverageFrameUs / 16_667.0) * 100.0;

        // ── Constructor ─────────────────────────────────────────────────────

        public GridSnapProfiler(int rollingWindowSize = 60)
        {
            RollingWindowSize = rollingWindowSize;
            _frameTickHistory = new long[rollingWindowSize];
            _frameOpHistory = new int[rollingWindowSize];
        }

        // ── Profiling API ───────────────────────────────────────────────────

        /// <summary>
        /// Wraps a snap operation with timing. Call this instead of
        /// <see cref="DeterministicGrid.Snap"/> when profiling is active.
        /// </summary>
        public Vector2Int ProfileSnap(DeterministicGrid grid, Vector2 worldPosition)
        {
            if (!Enabled)
                return grid.Snap(worldPosition);

            _stopwatch.Restart();
            Vector2Int result = grid.Snap(worldPosition);
            _stopwatch.Stop();

            long elapsed = _stopwatch.ElapsedTicks;
            _totalTicks += elapsed;
            _frameTicks += elapsed;
            _totalOps++;
            _frameOps++;

            return result;
        }

        /// <summary>
        /// Profiles a batch snap operation on a span of positions.
        /// </summary>
        public void ProfileSnapBatch(DeterministicGrid grid, Vector2[] positions, Vector2Int[] results)
        {
            if (!Enabled)
            {
                for (int i = 0; i < positions.Length; i++)
                    results[i] = grid.Snap(positions[i]);
                return;
            }

            _stopwatch.Restart();
            for (int i = 0; i < positions.Length; i++)
                results[i] = grid.Snap(positions[i]);
            _stopwatch.Stop();

            long elapsed = _stopwatch.ElapsedTicks;
            _totalTicks += elapsed;
            _frameTicks += elapsed;
            _totalOps += positions.Length;
            _frameOps += positions.Length;
        }

        // ── Frame Boundary ──────────────────────────────────────────────────

        /// <summary>
        /// Call at the end of each frame (e.g., in LateUpdate) to record
        /// frame-level statistics and advance the rolling window.
        /// </summary>
        public void EndFrame()
        {
            if (!Enabled) return;

            // Record frame data
            _frameTickHistory[_historyIndex] = _frameTicks;
            _frameOpHistory[_historyIndex] = _frameOps;

            // Track peak
            if (_frameTicks > _peakFrameTicks)
                _peakFrameTicks = _frameTicks;

            // Advance
            _historyIndex = (_historyIndex + 1) % RollingWindowSize;
            _frameCount++;
            _frameTicks = 0;
            _frameOps = 0;
        }

        // ── Reset ───────────────────────────────────────────────────────────

        /// <summary>Resets all profiling data.</summary>
        public void Reset()
        {
            _totalTicks = 0;
            _frameTicks = 0;
            _totalOps = 0;
            _frameOps = 0;
            _frameCount = 0;
            _peakFrameTicks = 0;
            _historyIndex = 0;
            Array.Clear(_frameTickHistory, 0, _frameTickHistory.Length);
            Array.Clear(_frameOpHistory, 0, _frameOpHistory.Length);
        }

        // ── Debug Output ────────────────────────────────────────────────────

        /// <summary>
        /// Returns a formatted string for live overlay or console display.
        /// </summary>
        public string GetLiveReadout()
        {
            return $"[PXG PROFILER] Ops: {TotalOperations:N0} | " +
                   $"Avg: {AverageSnapUs:F3}µs/op | " +
                   $"Frame: {RollingAverageFrameUs:F1}µs ({RollingAverageOpsPerFrame:F0} ops) | " +
                   $"Peak: {PeakFrameUs:F1}µs | " +
                   $"CPU: {CpuOverheadPercent:F2}%";
        }

        /// <summary>Logs the live readout to Unity console.</summary>
        public void LogReadout()
        {
            Debug.Log(GetLiveReadout());
        }
    }
}
