using UnityEngine;

[DisallowMultipleComponent]
public sealed class GuardSlashTuning : MonoBehaviour
{
    [SerializeField, Min(0f), InspectorName("闪避距离")] internal float dodgeDistance = 1f;
    [SerializeField, Min(0.1f), InspectorName("反击时间（秒）")] internal float counterTime = 3f;
    [SerializeField, Min(0.1f), InspectorName("结束防御时间（秒）")] internal float guardTime = 5f;
    [SerializeField, Range(0.01f, 1), InspectorName("完美闪避时敌人速度倍率")] internal float perfectDodgeEnemySpeed = 0.1f;
    [SerializeField, Range(0, 1), InspectorName("组合防御承伤倍率")] internal float guardSlashDamageMultiplier = 0.4f;
    [SerializeField, Range(0f, 0.25f), InspectorName("闪避预测时间（秒）")] internal float dodgeLookAhead = 0.12f;
    [SerializeField, Range(0f, 0.5f), InspectorName("闪避判定容差")] internal float dodgeTolerance = 0.18f;
    [SerializeField, Range(0.05f, 1f), InspectorName("子弹时间计时倍率")] internal float bulletTimeClockRate = 0.3f;
    [SerializeField, Min(1f), InspectorName("子弹时间玩家速度倍率")] internal float bulletTimePlayerSpeed = 1.6f;
}
