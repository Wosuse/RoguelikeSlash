using UnityEngine;

public sealed class ArenaAudioSettings : MonoBehaviour
{
    [SerializeField, Range(0, 1), InspectorName("音效音量")] internal float volume = .75f;
    [SerializeField, InspectorName("玩家射击")] internal AudioClip playerShot;
    [SerializeField, InspectorName("敌人射击")] internal AudioClip enemyShot;
    [SerializeField, InspectorName("玩家受伤")] internal AudioClip hurt;
}