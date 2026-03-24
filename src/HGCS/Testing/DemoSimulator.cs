using UnityEngine;
using Unity.Profiling;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PXG.HGCS.Testing
{
    /// <summary>
    /// Executes a headless 15-minute presentation simulation pipeline.
    /// Hooks into the CoreCLR ProfilerRecorder to physically log hardware CPU latency during automated CI tests.
    /// </summary>
    public class DemoSimulator : MonoBehaviour
    {
        public float runTime = 900f;          // 15 min in seconds
        private float elapsed = 0f;
        private float lastLogTime = 0f;
        private const float logInterval = 5f;

        private ProfilerRecorder mainThreadTimeRecorder;

#if UNITY_EDITOR
        /// <summary>
        /// CI Pipeline Entry Point.
        /// Executed via: -executeMethod PXG.HGCS.Testing.DemoSimulator.Run
        /// </summary>
        public static void Run()
        {
            Debug.Log("[PXG-HGCS CI] Initializing Headless Simulator via CLI -executeMethod.");
            EditorApplication.EnterPlaymode();
        }
#endif

        void OnEnable()
        {
            // Specifically hook the Unity 6 ProfilerRecorder for Main Thread execution timings
            mainThreadTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
        }

        void OnDisable()
        {
            if (mainThreadTimeRecorder.Valid)
                mainThreadTimeRecorder.Dispose();
        }

        void Update()
        {
            elapsed += Time.deltaTime;

            if (elapsed - lastLogTime >= logInterval)
            {
                if (mainThreadTimeRecorder.Valid)
                {
                    double cpuMs = mainThreadTimeRecorder.LastValue * 1e-6f;
                    Debug.Log($"[PXG-HGCS DEMO SIMULATOR] [{elapsed:F1}s] Engine CPU Time: {cpuMs:F2} ms");
                }
                lastLogTime = elapsed;
            }

            if (elapsed >= runTime)
            {
                Debug.Log("[PXG-HGCS DEMO SIMULATOR] 15‑minute script validation finished. Terminating Simulation.");
#if UNITY_EDITOR
                EditorApplication.isPlaying = false;
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
#else
                Application.Quit(0);
#endif
            }
        }
    }
}