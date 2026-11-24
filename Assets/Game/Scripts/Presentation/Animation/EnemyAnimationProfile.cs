using UnityEngine;

public sealed class EnemyAnimationProfile : MonoBehaviour
{
    [InspectorName("站立图")] public Sprite idle;
    [InspectorName("移动帧")] public Sprite[] move;
    [InspectorName("攻击帧（前两帧蓄力，后两帧出刀）")] public Sprite[] attack;
    [Min(1), InspectorName("移动帧率")] public float moveFps = 12;
    [Min(.01f), InspectorName("出刀播放时长")] public float releaseSeconds = .18f;
    [Min(.1f), InspectorName("显示尺寸倍率")] public float scale = 1.7f;
    public Sprite MoveFrame(float elapsed) => move != null && move.Length > 0
        ? move[Mathf.FloorToInt(elapsed * moveFps) % move.Length] : idle;
    public Sprite AttackFrame(float progress, bool released)
    {
        if (attack == null || attack.Length == 0) return idle;
        int split = Mathf.Max(1, attack.Length / 2);
        int start = released ? Mathf.Min(split, attack.Length - 1) : 0;
        int count = released ? attack.Length - start : split;
        return attack[start + Mathf.Clamp(Mathf.FloorToInt(progress * count), 0, count - 1)];
    }
}
