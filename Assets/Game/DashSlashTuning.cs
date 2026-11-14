using UnityEngine;

[DisallowMultipleComponent]
public sealed class DashSlashTuning : MonoBehaviour
{
    [SerializeField, Min(0.05f), InspectorName("飞行斩击时长（秒）")] internal float arcTravelTime = 0.36f;
}
