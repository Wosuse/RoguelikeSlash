using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MeshRenderer))]
public sealed class ArenaPlayerAnimation : MonoBehaviour
{
    const float PlayerAnimationLoopSeconds = 56f / 24f;
    [Header("主角动画")]
    [SerializeField, Min(0f), InspectorName("待机播放倍率")] float idlePlaybackSpeed = 1f;
    [SerializeField, Min(0f), InspectorName("移动播放倍率")] float movePlaybackSpeed = 1f;
    [SerializeField, InspectorName("步频跟随实际移速")] bool matchMovementSpeed = true;
    [SerializeField, Min(0.01f), InspectorName("标准步频对应移速")] float referenceWalkSpeed = 5f;
    [Header("技能动作装配")]
    [InspectorName("技能动画配置")] public SkillAnimationSet animationSet;
    [Header("不屈无敌表现")]
    [Range(0,1), InspectorName("无敌期间不透明度"), Tooltip("0 完全透明，1 完全不透明；仅在不屈触发的无敌期间生效。")] public float undyingOpacity = .35f;
    Vector3 baseScale, basePosition;
    bool playedSkillAnimation;
    Texture2D[] idleFrames, moveFrames;
    Material playerAnimationMaterial;
    Color baseTint;
    bool walkingAnimation;
    float animationPhase;
    Rigidbody2D body;
    ArenaPrototype arena;

    internal void Initialize(Rigidbody2D playerBody, Color tint, ArenaPrototype owner=null)
    {
        baseScale = transform.localScale;
        basePosition = transform.localPosition;
        body = playerBody;
        arena = owner;
        idleFrames = LoadPlayerFrames("idle");
        moveFrames = LoadPlayerFrames("move");
        if (idleFrames.Length == 0 || moveFrames.Length == 0) return;

        playerAnimationMaterial = GetComponent<MeshRenderer>().sharedMaterial;
        baseTint = tint;
        playerAnimationMaterial.color = baseTint;
        if(arena&&playerAnimationMaterial.HasProperty("_Flip"))arena.SetVisualFacing(playerAnimationMaterial.GetVector("_Flip").x);
        animationPhase = 0f;
        playerAnimationMaterial.mainTexture = idleFrames[0];
    }

    static Texture2D[] LoadPlayerFrames(string state)
    {
        var frames = Resources.LoadAll<Texture2D>("Animations/Player/" + state);
        Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name));
        return frames;
    }

    void LateUpdate()
    {
        if (!playerAnimationMaterial || !body) return;
        var tint=baseTint;
        if(arena&&arena.v06Attack&&arena.v06Attack.build&&arena.v06Attack.build.Immune)
            tint.a*=Mathf.Clamp01(undyingOpacity);
        playerAnimationMaterial.color=tint;
        if (arena && arena.v06Skills && arena.v06Skills.clock.Paused) return;
        Vector2 velocity = body.velocity;
        float speed = velocity.magnitude;
        bool walking = speed > 0.1f;
        if (walking != walkingAnimation)
        {
            walkingAnimation = walking;
            animationPhase = 0f;
        }

        var skills = arena ? arena.v06Skills : null;
        bool action = false;
        transform.localScale = baseScale;
        transform.localPosition = basePosition;
        if (skills && animationSet && skills.AnimationMotion != PlayerSkillMotion.None)
        {
            float clipTime;
            var clip = animationSet.Motion(skills.AnimationMotion, skills.AnimationElapsed, out clipTime);
            if (skills.AnimationMotion == PlayerSkillMotion.Run && matchMovementSpeed) clipTime *= speed / Mathf.Max(.01f, referenceWalkSpeed);
            if (clip != null && clip.Valid)
            {
                bool loop = skills.AnimationMotion == PlayerSkillMotion.Run ||
                    skills.AnimationMotion == PlayerSkillMotion.Charge && clip == animationSet.chargeHold;
                float duration = skills.AnimationMotion == PlayerSkillMotion.Charge || loop ? 0 : skills.AnimationDuration;
                var sprite = clip.Sample(clipTime, loop, duration);
                if (sprite)
                {
                    playerAnimationMaterial.mainTexture = sprite.texture;
                    transform.localScale = baseScale * clip.scale;
                    transform.localPosition = basePosition + new Vector3(clip.offset.x * baseScale.x, clip.offset.y * baseScale.y, 0);
                    action = true;
                }
            }
        }
        if (!action)
        {
            if (playedSkillAnimation) animationPhase = 0;
            var frames = walking ? moveFrames : idleFrames;
            float playbackSpeed = walking ? movePlaybackSpeed : idlePlaybackSpeed;
            if (walking && matchMovementSpeed)
                playbackSpeed *= speed / Mathf.Max(0.01f, referenceWalkSpeed);
            animationPhase = Mathf.Repeat(animationPhase + Time.deltaTime * playbackSpeed / PlayerAnimationLoopSeconds, 1f);
            int frame = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(animationPhase * frames.Length));
            playerAnimationMaterial.mainTexture = frames[frame];
        }
        playedSkillAnimation = action;
        bool preserveFacing=arena&&arena.v06Skills&&arena.v06Skills.PreserveDodgeFacing;
        float horizontal = arena&&arena.HasMovementInput?arena.V06Facing.x:velocity.x;
        if (action && (skills.AnimationMotion == PlayerSkillMotion.Slash || skills.AnimationMotion == PlayerSkillMotion.Spin || skills.AnimationMotion == PlayerSkillMotion.ForwardDodge)) horizontal = skills.AnimationFacing.x;
        if (!preserveFacing && (walking || action) && Mathf.Abs(horizontal) > 0.01f && playerAnimationMaterial.HasProperty("_Flip"))
        {
            bool left = horizontal < 0;
            if(arena)arena.SetVisualFacing(horizontal);
            playerAnimationMaterial.SetVector("_Flip", new Vector4(left ? -1f : 1f, 1f, 1f, 1f));
        }
    }

    void OnDisable()
    {
        if(playerAnimationMaterial)playerAnimationMaterial.color=baseTint;
    }
}
