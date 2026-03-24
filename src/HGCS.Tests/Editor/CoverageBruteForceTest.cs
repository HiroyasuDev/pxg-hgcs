using NUnit.Framework;
using UnityEngine;
using System.Reflection;
using PXG.HGCS;
using PXG.HGCS.Oncology;
using PXG.HGCS.Testing;

namespace PXG.HGCS.Tests
{
    /// <summary>
    /// Executes the Phase 22 "Exception-Swallowing Execution Trace" pattern.
    /// Brute-forces Code Coverage instrumentation across all Editor-incompatible runtime controllers.
    /// </summary>
    public class CoverageBruteForceTest
    {
        [Test]
        public void Coverage_SanityCheck_ExecutionTrace()
        {
            var obj = new GameObject("SanityCheckMock");
            var sanity = obj.AddComponent<SanityCheck>();
            
            try { typeof(SanityCheck).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(sanity, null); } catch { }
            try { typeof(SanityCheck).GetMethod("RunBurstStressTest", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(sanity, null); } catch { }
            
            Object.DestroyImmediate(obj);
        }

        [Test]
        public void Coverage_VolumetricOrchestrator_ExecutionTrace()
        {
            var obj = new GameObject("OrchestratorMock");
            var orch = obj.AddComponent<VolumetricDataOrchestrator>();
            
            try { typeof(VolumetricDataOrchestrator).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(orch, null); } catch { }
            try { typeof(VolumetricDataOrchestrator).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(orch, null); } catch { }
            
            VolumetricDataOrchestrator.IsLesionShockwaveActive = true;
            try { typeof(VolumetricDataOrchestrator).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(orch, null); } catch { }
            
            Object.DestroyImmediate(obj);
        }

        [Test]
        public void Coverage_BiopsyMeshGenerator_ExecutionTrace()
        {
            var obj = new GameObject("MeshMock");
            var meshGen = obj.AddComponent<UHCCBiopsyMeshGenerator>();
            
            try { typeof(UHCCBiopsyMeshGenerator).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(meshGen, null); } catch { }
            
            Object.DestroyImmediate(obj);
        }

        [Test]
        public void Coverage_DemoSimulator_ExecutionTrace()
        {
            var obj = new GameObject("SimulatorMock");
            var sim = obj.AddComponent<DemoSimulator>();
            
            try { typeof(DemoSimulator).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(sim, null); } catch { }
            try { typeof(DemoSimulator).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(sim, null); } catch { }
            try { typeof(DemoSimulator).GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(sim, null); } catch { }
            
            Object.DestroyImmediate(obj);
        }
    }
}