using UnityEngine;

[DisallowMultipleComponent]
public sealed class GuardVisualTuning : MonoBehaviour
{
    [SerializeField, InspectorName("点按防御颜色")] internal Color tapGuardColor = new Color(1f, 0.82f, 0.27f);
    [SerializeField, InspectorName("长按防御颜色")] internal Color holdGuardColor = new Color(0.72f, 0.55f, 0.26f);
    [SerializeField, InspectorName("按钮颜色")] internal Color guardColor = new Color(0.37f, 0.73f, 1f);
    [SerializeField, InspectorName("按钮图标")] internal Texture2D guardIcon;
}
