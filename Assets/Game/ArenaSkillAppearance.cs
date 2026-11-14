using UnityEngine;

public sealed class ArenaSkillAppearance : MonoBehaviour
{
    [SerializeField, InspectorName("颜色")] internal Color color = new Color(.38f, 1, .79f);
    [SerializeField, InspectorName("材质")] internal Material material;
    [SerializeField, Min(.01f), InspectorName("线宽")] internal float width = .085f;
}
