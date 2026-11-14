using UnityEngine;

public sealed class ArenaActorAppearance : MonoBehaviour
{
    [SerializeField, InspectorName("材质")] internal Material material;
    [SerializeField, InspectorName("纹理")] internal Texture2D texture;
    [SerializeField, InspectorName("颜色倍率")] internal Color tint = Color.white;
}