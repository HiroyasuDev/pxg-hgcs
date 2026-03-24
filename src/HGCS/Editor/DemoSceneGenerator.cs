using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using PXG.HGCS;

public static class DemoSceneGenerator
{
    [MenuItem("PXG / Generate UHCC Demonstration")]
    public static void GenerateAndPlay()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        
        // 1. Spawning the High-Performance Mathematical Tracker 
        GameObject hudGO = new GameObject("RuntimeTelemetryHUD_Node");
        hudGO.AddComponent<RuntimeTelemetryHUD>();
        
        // 2. Immersive Visual Setup: Re-color Camera for Space/Ocean context
        var cam = Camera.main;
        if (cam != null)
        {
            cam.farClipPlane = 10000f; // Obliterate default culling threshold
            cam.backgroundColor = new Color(0.01f, 0.02f, 0.05f); // Abyss Blue
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(0, 0, -400f); // Backup camera for ideal XR framing
        }

        // 3. Spawning the 100-node Spatial UI Generator
        GameObject orchestratorGO = new GameObject("XR_Data_Orchestrator");
        orchestratorGO.AddComponent<VolumetricDataOrchestrator>();

        // 4. Spawning the Biological Presentation Anchor
        GameObject tumorMeshGO = new GameObject("UHCC_Biopsy_Core");
        tumorMeshGO.AddComponent<PXG.HGCS.Oncology.UHCCBiopsyMeshGenerator>();

        if (cam != null) {
            var orbit = cam.gameObject.AddComponent<MouseOrbitCamera>();
            orbit.target = tumorMeshGO.transform;
            orbit.distance = 1500f; // Pull the initial camera back to properly frame the external data nodes
        }

        Selection.activeGameObject = orchestratorGO;
        EditorApplication.ExecuteMenuItem("Window/General/Game");

        Debug.Log("[PXG] Demo Scene Scaffolded. Engaging XR Node Volume & Play Mode...");
        EditorApplication.isPlaying = true;
    }
}
