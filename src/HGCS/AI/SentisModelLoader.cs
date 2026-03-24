// =============================================================================
// PXG // HARD-GRID COORDINATE SYSTEM (HGCS)
// AI Module: Sentis Model Loader
// Complete ONNX model lifecycle for Unity Sentis neural-network inference.
// Operational Order: PXG-2026-SOMU-0325
// =============================================================================

using System;
using System.IO;
using UnityEngine;

// Unity.Sentis namespace — requires com.unity.sentis package
// using Unity.Sentis;

namespace PXG.HGCS.AI
{
    /// <summary>
    /// Manages the complete Unity Sentis model lifecycle: load .onnx models,
    /// create inference workers, execute inference, and dispose resources.
    ///
    /// Supports both GPU (recommended) and CPU fallback for environments
    /// without dedicated compute (e.g., GT 1030 / integrated graphics).
    /// </summary>
    public class SentisModelLoader : IDisposable
    {
        // ── Model State ─────────────────────────────────────────────────────

        /// <summary>Whether a model is currently loaded and ready.</summary>
        public bool IsLoaded { get; private set; }

        /// <summary>Model file path on disk.</summary>
        public string ModelPath { get; private set; }

        /// <summary>Model input dimensions (anchors × features).</summary>
        public (int anchors, int features) InputShape { get; private set; }

        /// <summary>Model output dimensions (anchors × delta_features).</summary>
        public (int anchors, int deltas) OutputShape { get; private set; }

        // ── Backend Configuration ───────────────────────────────────────────

        /// <summary>Inference backend preference.</summary>
        public enum BackendType
        {
            /// <summary>GPU compute via Sentis (preferred for speed).</summary>
            GPUCompute,

            /// <summary>CPU fallback (Burst-compiled, for non-GPU setups).</summary>
            CPU,

            /// <summary>Auto-detect: GPU if available, else CPU.</summary>
            Auto
        }

        /// <summary>Active backend.</summary>
        public BackendType ActiveBackend { get; private set; } = BackendType.Auto;

        // ── Sentis Objects (commented until package is imported) ─────────────
        // private Model _model;
        // private IWorker _worker;
        // private BackendType _resolvedBackend;

        // ── Constructor ─────────────────────────────────────────────────────

        /// <summary>
        /// Creates a new model loader. Call <see cref="LoadModel"/> to
        /// initialize inference.
        /// </summary>
        public SentisModelLoader(BackendType backend = BackendType.Auto)
        {
            ActiveBackend = backend;
        }

        // ── Load Model ──────────────────────────────────────────────────────

        /// <summary>
        /// Loads an ONNX model from disk and creates an inference worker.
        /// </summary>
        /// <param name="onnxPath">Absolute path to .onnx model file.</param>
        /// <param name="maxAnchors">Maximum anchor count the model supports.</param>
        /// <param name="featureCount">Features per anchor (x, y, scale, locked, displayIdx).</param>
        public void LoadModel(string onnxPath, int maxAnchors = 256, int featureCount = 5)
        {
            if (!File.Exists(onnxPath))
                throw new FileNotFoundException($"ONNX model not found: {onnxPath}");

            ModelPath = onnxPath;
            InputShape = (maxAnchors, featureCount);
            OutputShape = (maxAnchors, 3); // dx, dy, confidence

            // ── Sentis Loading (uncomment when com.unity.sentis is installed) ──
            //
            // _model = ModelLoader.Load(onnxPath);
            //
            // _resolvedBackend = ActiveBackend;
            // if (_resolvedBackend == BackendType.Auto)
            // {
            //     _resolvedBackend = SystemInfo.supportsComputeShaders
            //         ? BackendType.GPUCompute : BackendType.CPU;
            // }
            //
            // var backendType = _resolvedBackend == BackendType.GPUCompute
            //     ? Unity.Sentis.BackendType.GPUCompute
            //     : Unity.Sentis.BackendType.CPU;
            //
            // // 10/10 PRESENTATION EDGE CASE TRAP: Extinguish GPU Lag Spikes
            // try 
            // {
            //     _worker = WorkerFactory.CreateWorker(backendType, _model);
            // } 
            // catch (Exception ex)
            // {
            //     Debug.LogWarning($"[PXG 10/10] AI GPU Mount Failed. Gracefully aborting to save Framerate: {ex.Message}");
            //     IsLoaded = false;
            //     return;
            // }
            //
            // Debug.Log($"[PXG.HGCS.AI] Model loaded: {onnxPath}");
            // Debug.Log($"[PXG.HGCS.AI] Backend: {_resolvedBackend}");
            // Debug.Log($"[PXG.HGCS.AI] Input: {maxAnchors}×{featureCount}, Output: {maxAnchors}×3");

            // Stub logging
            Debug.Log($"[PXG.HGCS.AI] SentisModelLoader: Stub-loaded model from {onnxPath}");
            Debug.Log($"[PXG.HGCS.AI] Input shape: ({maxAnchors}, {featureCount}), Output: ({maxAnchors}, 3)");
            Debug.Log($"[PXG.HGCS.AI] Backend: {ActiveBackend} (stub — install com.unity.sentis to activate)");

            IsLoaded = true;
        }

        // ── Execute Inference ────────────────────────────────────────────────

        /// <summary>
        /// Runs inference on an input tensor (flattened anchor data).
        /// Returns output tensor (flattened delta suggestions).
        /// </summary>
        /// <param name="inputData">Flattened input: [anchors × features].</param>
        /// <returns>Flattened output: [anchors × 3] (dx, dy, confidence).</returns>
        public float[] Execute(float[] inputData)
        {
            if (!IsLoaded)
                throw new InvalidOperationException("No model loaded. Call LoadModel() first.");

            int expectedLength = InputShape.anchors * InputShape.features;
            if (inputData.Length != expectedLength)
                throw new ArgumentException(
                    $"Input length {inputData.Length} doesn't match expected {expectedLength}");

            // ── Sentis Inference (uncomment when package is installed) ────
            //
            // using var inputTensor = new TensorFloat(
            //     new TensorShape(1, InputShape.anchors, InputShape.features), inputData);
            //
            // _worker.Execute(inputTensor);
            //
            // var outputTensor = _worker.PeekOutput() as TensorFloat;
            // outputTensor.MakeReadable();
            //
            // float[] output = new float[OutputShape.anchors * OutputShape.deltas];
            // for (int i = 0; i < output.Length; i++)
            //     output[i] = outputTensor[i];
            //
            // return output;

            // Stub: return zero deltas with random confidence for demo
            int outputLength = OutputShape.anchors * OutputShape.deltas;
            float[] output = new float[outputLength];
            var rng = new System.Random(DateTime.Now.Millisecond);

            for (int i = 0; i < OutputShape.anchors; i++)
            {
                int baseIdx = i * OutputShape.deltas;
                output[baseIdx + 0] = 0f;      // dx (no suggestion in stub)
                output[baseIdx + 1] = 0f;      // dy
                output[baseIdx + 2] = (float)rng.NextDouble() * 0.5f; // low confidence in stub
            }

            return output;
        }

        // ── Dispose ─────────────────────────────────────────────────────────

        public void Dispose()
        {
            // _worker?.Dispose();
            // _model = null;
            IsLoaded = false;
            Debug.Log("[PXG.HGCS.AI] SentisModelLoader disposed.");
        }
    }
}
