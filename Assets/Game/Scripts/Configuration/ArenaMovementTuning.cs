using UnityEngine;

public sealed class ArenaMovementTuning : MonoBehaviour
{
    // Separate from the old world-units speed field: Unity may retain its value during script reload.
    public const float UnitsPerSpeedPoint = .05f;
    [SerializeField, Min(.1f), InspectorName("基础移动速度"), Tooltip("100对应5单位/秒；按此基准调整，不影响闪避距离。")] internal float baseSpeed = 100;
    public float WorldSpeed => baseSpeed * UnitsPerSpeedPoint;
    // Legacy fallback only. M6 fire intervals are configured per WeaponDefinition.
    [SerializeField, HideInInspector] internal float shotInterval = .8f;
}
