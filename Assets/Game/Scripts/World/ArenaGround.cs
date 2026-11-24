using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class ArenaGround : MonoBehaviour
{
    [SerializeField] Material grassMaterial;
    [SerializeField, Min(0.5f)] float repeatSize = 8f;

    Mesh mesh;
    GameObject surface;
    RectInt covered;
    float lastSize;

    void LateUpdate()
    {
        var camera = Camera.main;
        if (!camera || !camera.orthographic || !grassMaterial) return;
        float size = Mathf.Max(0.5f, repeatSize);
        Vector2 half = new Vector2(camera.orthographicSize * camera.aspect, camera.orthographicSize);
        Vector2 center = camera.transform.position;
        int left = Mathf.FloorToInt((center.x - half.x) / size);
        int right = Mathf.CeilToInt((center.x + half.x) / size);
        int bottom = Mathf.FloorToInt((center.y - half.y) / size);
        int top = Mathf.CeilToInt((center.y + half.y) / size);
        var bounds = new RectInt(left, bottom, right - left, top - bottom);
        if (!surface) CreateSurface();
        surface.GetComponent<MeshRenderer>().sharedMaterial = grassMaterial;
        if (bounds.Equals(covered) && size == lastSize) return;
        covered = bounds;
        lastSize = size;
        surface.transform.position = new Vector3(left * size, bottom * size, 3f);
        float width = bounds.width * size;
        float height = bounds.height * size;
        mesh.vertices = new[] { Vector3.zero, new Vector3(0, height), new Vector3(width, height), new Vector3(width, 0) };
        mesh.uv = new[] { Vector2.zero, new Vector2(0, bounds.height), new Vector2(bounds.width, bounds.height), new Vector2(bounds.width, 0) };
        mesh.RecalculateBounds();
    }

    void CreateSurface()
    {
        surface = new GameObject("Grass surface", typeof(MeshFilter), typeof(MeshRenderer));
        surface.hideFlags = HideFlags.DontSave;
        surface.transform.SetParent(transform, false);
        mesh = new Mesh { name = "Repeating grass", hideFlags = HideFlags.DontSave };
        mesh.MarkDynamic();
        mesh.vertices = new Vector3[4];
        mesh.colors32 = new[] { White, White, White, White };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        surface.GetComponent<MeshFilter>().sharedMesh = mesh;
        lastSize = 0;
    }

    static readonly Color32 White = new Color32(255, 255, 255, 255);

    void OnDisable()
    {
        if (Application.isPlaying)
        {
            if (surface) Destroy(surface);
            if (mesh) Destroy(mesh);
        }
        else
        {
            if (surface) DestroyImmediate(surface);
            if (mesh) DestroyImmediate(mesh);
        }
        surface = null;
        mesh = null;
    }
}
