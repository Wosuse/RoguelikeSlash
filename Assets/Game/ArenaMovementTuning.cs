using UnityEngine;

public sealed class ArenaMovementTuning : MonoBehaviour
{
    [SerializeField, Min(.1f), InspectorName("移动速度")] internal float speed = 5;
    [SerializeField, Min(.02f), InspectorName("自动射击间隔（秒）")] internal float shotInterval = .8f;
}