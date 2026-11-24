using System;
using UnityEngine;

public enum PlayerSkillMotion { None, Dodge, Run, Slash, Charge, Spin, ForwardDodge }

[Serializable]
public sealed class SkillAnimationClip
{
    [InspectorName("序列帧（按顺序）")] public Sprite[] frames = new Sprite[0];
    [Min(.001f), InspectorName("每帧秒数"), Tooltip("循环、蓄力和装配预览使用；短闪、挥剑、旋转适配技能生效时长。")] public float frameSeconds = .1f;
    [Min(.01f), InspectorName("播放倍率")] public float playbackSpeed = 1;
    [Tooltip("与序列帧逐项对应，数值越大停留越久；留空为均匀播放。不改变总播放时长。"), InspectorName("每帧时长权重")] public float[] frameWeights = new float[0];
    [Min(.01f), InspectorName("显示倍率")] public float scale = 1;
    [InspectorName("显示偏移（角色画布单位）")] public Vector2 offset;
    public bool Valid => frames != null && frames.Length > 0;
    public float Duration => Valid ? frames.Length * frameSeconds / Mathf.Max(.01f, playbackSpeed) : 0;

    // Non-looping skill actions fit their existing gameplay duration; charging and running use frame timing.
    public Sprite Sample(float elapsed, bool loop, float actionDuration = 0)
    {
        if (!Valid) return null;
        float frame = actionDuration > 0
            ? elapsed / Mathf.Max(.001f, actionDuration) * frames.Length * playbackSpeed
            : elapsed / Mathf.Max(.001f, frameSeconds) * playbackSpeed;
        if (frameWeights != null && frameWeights.Length == frames.Length)
        {
            float progress = Mathf.Max(0, frame) / frames.Length;
            progress = loop ? Mathf.Repeat(progress, 1) : Mathf.Clamp01(progress);
            float total = 0;
            for (int i = 0; i < frames.Length; i++) total += Mathf.Max(.001f, frameWeights[i]);
            float cursor = progress * total;
            for (int i = 0; i < frames.Length; i++)
            {
                cursor -= Mathf.Max(.001f, frameWeights[i]);
                if (cursor < 0) return frames[i];
            }
            return frames[frames.Length - 1];
        }
        int index = Mathf.Max(0, Mathf.FloorToInt(frame));
        return frames[loop ? index % frames.Length : Mathf.Min(index, frames.Length - 1)];
    }
}

[CreateAssetMenu(menuName = "割草/技能动画装配")]
public sealed class SkillAnimationSet : ScriptableObject
{
    [Header("角色动作：非循环动作按技能生效时间播放")]
    [InspectorName("短按闪：后闪")] public SkillAnimationClip dodge = new SkillAnimationClip();
    [InspectorName("短按闪：向前闪")] public SkillAnimationClip forwardDodge = new SkillAnimationClip();
    [InspectorName("长按闪：奔跑循环")] public SkillAnimationClip run = new SkillAnimationClip();
    [InspectorName("短按斩：挥剑")] public SkillAnimationClip slash = new SkillAnimationClip();
    [InspectorName("蓄势：进入蓄力")] public SkillAnimationClip chargeEnter = new SkillAnimationClip();
    [InspectorName("蓄势：保持循环")] public SkillAnimationClip chargeHold = new SkillAnimationClip();
    [InspectorName("长按斩：旋转")] public SkillAnimationClip spin = new SkillAnimationClip();
    [Header("剑气外观（月牙判定可同步显示大小）")]
    [InspectorName("月牙剑气")] public SkillAnimationClip crescent = new SkillAnimationClip();
    [InspectorName("圆环剑气")] public SkillAnimationClip ring = new SkillAnimationClip();
    [InspectorName("月牙基础宽高（世界单位）")] public Vector2 crescentSize = new Vector2(1.2f, 2);
    [Min(.01f), InspectorName("圆环外径显示倍率（补偿透明边距）")] public float ringDiameterScale = 1.2f;
    [InspectorName("剑气颜色倍率")] public Color swordTint = Color.white;

    public Vector2 CrescentDisplaySize => new Vector2(Mathf.Abs(crescentSize.x), Mathf.Abs(crescentSize.y)) * (crescent != null ? Mathf.Max(.01f,crescent.scale) : 1);

    public SkillAnimationClip Motion(PlayerSkillMotion motion, float elapsed, out float clipTime)
    {
        clipTime = elapsed;
        switch (motion)
        {
            case PlayerSkillMotion.Dodge: return dodge;
            case PlayerSkillMotion.ForwardDodge: return forwardDodge.Valid ? forwardDodge : dodge;
            case PlayerSkillMotion.Run: return run;
            case PlayerSkillMotion.Slash: return slash;
            case PlayerSkillMotion.Spin: return spin;
            case PlayerSkillMotion.Charge:
                if (elapsed < chargeEnter.Duration) return chargeEnter;
                clipTime = Mathf.Max(0, elapsed - chargeEnter.Duration);
                return chargeHold;
            default: return null;
        }
    }
}
