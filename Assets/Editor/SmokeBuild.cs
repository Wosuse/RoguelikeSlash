using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SmokeBuild
{
    public static void BuildWebGL()
    {
        Prepare();
        var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes,
            "Builds/WebGLSmoke", BuildTarget.WebGL, BuildOptions.Development);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new System.Exception("WebGL smoke build failed: " + report.summary.result);
        Debug.Log("SMOKE_WEBGL_BUILD_OK");
    }
    public static void Prepare()
    {
        EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
        PlayerSettings.productName = "Roguelike Slash";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera").AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.orthographic = true;
        camera.transform.position = new Vector3(0, 0, -10);
        camera.backgroundColor = new Color(0.08f, 0.12f, 0.18f);
        var marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
        marker.name = "Smoke test marker";
        System.IO.Directory.CreateDirectory("Assets/Game/Scenes");
        const string path = "Assets/Game/Scenes/SmokeTest.unity";
        EditorSceneManager.SaveScene(scene, path);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("SMOKE_PREPARE_OK WebGL=" + BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL));
    }
}
