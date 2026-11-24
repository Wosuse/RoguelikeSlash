using UnityEngine;

// Scene entry point for the shared weapon asset used by the inventory and combat.
[DisallowMultipleComponent]
public sealed class WeaponSetup : MonoBehaviour
{
    [InspectorName("武器配置资产")] public WeaponDefinition definition;
}
