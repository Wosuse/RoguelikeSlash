using UnityEngine;

[DisallowMultipleComponent]
public sealed class SlashVisualTuning : MonoBehaviour
{
    [SerializeField, InspectorName("环形斩击颜色")] internal Color ringSlashColor = new Color(0.26f, 0.67f, 1f);
    [SerializeField, InspectorName("按钮颜色")] internal Color slashColor = new Color(0.88f, 0.58f, 1f);
    [SerializeField, InspectorName("按钮图标")] internal Texture2D slashIcon;
}
