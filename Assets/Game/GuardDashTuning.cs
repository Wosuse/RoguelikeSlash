using UnityEngine;

public sealed class GuardDashTuning : MonoBehaviour
{
    [SerializeField, Min(0.1f), InspectorName("最长举盾时间")] internal float holdDuration = 3f;
    [SerializeField, Range(0, 1), InspectorName("举盾移动倍率")] internal float holdMoveMultiplier = 0.55f;
    [SerializeField, Min(0), InspectorName("反击距离")] internal float counterReach = 4.5f;
    [SerializeField, Min(0), InspectorName("反击半宽")] internal float counterHalfWidth = 0.8f;
    [SerializeField, Min(0), InspectorName("每层格挡追加伤害")] internal int damagePerCharge = 10;
}
