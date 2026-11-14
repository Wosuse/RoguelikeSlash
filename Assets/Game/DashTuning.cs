using UnityEngine;

[DisallowMultipleComponent]
public sealed class DashTuning : MonoBehaviour
{
    [SerializeField, Min(0.02f), InspectorName("冲刺时长（秒）")] internal float dashDuration = 0.2f;
    [SerializeField, Min(0), InspectorName("冲刺距离")] internal float dashDistance = 3f;
    [SerializeField, Range(0, 1), InspectorName("瞄准时移动倍率")] internal float aimedMoveMultiplier = 0.55f;
}
