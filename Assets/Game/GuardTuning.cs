using UnityEngine;

[DisallowMultipleComponent]
public sealed class GuardTuning : MonoBehaviour
{
    [SerializeField, Min(0), InspectorName("点按冷却（秒）")] internal float tapGuardCooldown = 2.5f;
    [SerializeField, Min(0.3f), InspectorName("长按上限（秒）")] internal float baseGuardLimit = 3f;
    [SerializeField, Min(0.1f), InspectorName("防御波速度")] internal float guardWaveSpeed = 2f;
    [SerializeField, Min(0.1f), InspectorName("防御波寿命（秒）")] internal float guardWaveLifetime = 1.8f;
    [SerializeField, Min(0.1f), InspectorName("防御半径")] internal float guardRadius = 1.05f;
    [SerializeField, Min(1), InspectorName("防御波伤害")] internal int guardWaveDamage = 35;
    [SerializeField, Min(0.01f), InspectorName("格挡窗口（秒）")] internal float parryWindow = 0.2f;
    [SerializeField, Range(0, 1), InspectorName("防御时承伤倍率")] internal float guardDamageMultiplier = 0.5f;
}
