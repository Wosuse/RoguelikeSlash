using UnityEngine;

[DisallowMultipleComponent]
public sealed class DashVisualTuning : MonoBehaviour
{
    [SerializeField, InspectorName("拖尾颜色")] internal Color dashTrailColor = new Color(1f, 0.22f, 0.25f);
    [SerializeField, Min(0.02f), InspectorName("拖尾寿命（秒）")] internal float dashTrailLifetime = 0.22f;
    [SerializeField, InspectorName("按钮颜色")] internal Color dashColor = new Color(1f, 0.69f, 0.36f);
    [SerializeField, InspectorName("按钮图标")] internal Texture2D dashIcon;
}
