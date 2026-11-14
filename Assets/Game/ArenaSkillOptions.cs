using UnityEngine;

public sealed class ArenaSkillOptions : MonoBehaviour
{
    [SerializeField, Min(0), InspectorName("冷却时间（秒）")] internal float cooldown = 8;
    [SerializeField, Min(.05f), InspectorName("长按判定（秒）")] internal float holdThreshold = .25f;
    [SerializeField, InspectorName("对应场景按钮")] internal ArenaSkillButton button;
}