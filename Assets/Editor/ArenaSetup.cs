using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ArenaSetup
{
    public static void Prepare()
    {
        const string materialPath = "Assets/Game/ArenaSprite.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (!material)
        {
            material = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(material, materialPath);
        }
        const string gridPath = "Assets/Game/ArenaGrid.mat";
        var grid = AssetDatabase.LoadAssetAtPath<Material>(gridPath);
        if (!grid)
        {
            grid = new Material(material) { color = new Color(0.16f, 0.23f, 0.30f) };
            AssetDatabase.CreateAsset(grid, gridPath);
        }
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera").AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.07f, 0.10f, 0.14f);
        camera.transform.position = new Vector3(0, 0, -10);
        var arena = new GameObject("Arena").AddComponent<ArenaPrototype>();
        var settings = new SerializedObject(arena);
        settings.FindProperty("spriteMaterial").objectReferenceValue = material;
        settings.ApplyModifiedPropertiesWithoutUndo();

        var floor = new GameObject("Grid");
        for (int i = -24; i <= 24; i += 2)
        {
            AddLine(floor.transform, grid, new Vector3(i, 0, 2), new Vector3(0.025f, 48, 1));
            AddLine(floor.transform, grid, new Vector3(0, i, 2), new Vector3(48, 0.025f, 1));
        }
        const string path = "Assets/Game/Scenes/GameScene.unity";
        EditorSceneManager.SaveScene(scene, path);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("M1_SCENE_READY");
    }

    static void AddLine(Transform floor, Material material, Vector3 position, Vector3 scale)
    {
        var line = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.DestroyImmediate(line.GetComponent<Collider>());
        line.name = "Grid line";
        line.transform.SetParent(floor);
        line.transform.localPosition = position;
        line.transform.localScale = scale;
        line.GetComponent<Renderer>().sharedMaterial = material;
    }
}
