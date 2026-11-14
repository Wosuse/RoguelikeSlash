using UnityEngine;

[DisallowMultipleComponent]
public sealed class SlashTuning : MonoBehaviour
{
    [SerializeField, Min(0.01f), InspectorName("斩击半径")] internal float slashRadius = 2.2f;
    [SerializeField, Min(0.02f), InspectorName("旋转伤害间隔（秒）")] internal float spinInterval = 0.35f;
    [SerializeField, Min(0), InspectorName("斩击伤害")] internal int slashDamage = 34;
    [SerializeField, Min(0), InspectorName("旋转伤害")] internal int spinDamage = 14;
    [SerializeField, Min(0), InspectorName("瞄准斩击伤害")] internal int aimedDamage = 45;
    [SerializeField, Min(0.1f), InspectorName("长按上限（秒）")] internal float slashHoldLimit = 3f;
    [SerializeField, Min(0.05f), InspectorName("斩击扫掠宽度")] internal float slashSweepWidth = 0.24f;
}
