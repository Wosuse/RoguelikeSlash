using UnityEngine;

public sealed class ArenaMovementTuning : MonoBehaviour
{
    [SerializeField, Min(.1f), InspectorName("移动速度")] internal float speed = 5;
    // Legacy fallback only. M6 fire intervals are configured per WeaponDefinition.
    [SerializeField, HideInInspector] internal float shotInterval = .8f;
}
