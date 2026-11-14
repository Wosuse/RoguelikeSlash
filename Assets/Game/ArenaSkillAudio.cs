using UnityEngine;

public sealed class ArenaSkillAudio : MonoBehaviour
{
    [SerializeField, InspectorName("防御音效")] internal AudioClip guard;
    [SerializeField, InspectorName("格挡音效")] internal AudioClip parry;
    [SerializeField, InspectorName("冲刺音效")] internal AudioClip dash;
    [SerializeField, InspectorName("斩击音效")] internal AudioClip slash;
}