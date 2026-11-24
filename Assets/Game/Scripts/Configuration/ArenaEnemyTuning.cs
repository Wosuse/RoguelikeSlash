using UnityEngine;

public sealed class ArenaEnemyTuning : MonoBehaviour
{
    [SerializeField, Min(0), InspectorName("特殊敌人出现时间（秒）")] internal float specialStart = 30;
    [SerializeField, Range(0, 2f), InspectorName("敌人间距")] internal float spacing = 1.2f;
    [SerializeField, Min(.05f), InspectorName("近战预警时间（秒）")] internal float warningTime = .3f;
    [SerializeField, Min(.1f), InspectorName("近战攻击范围")] internal float attackRange = 1.15f;
}
