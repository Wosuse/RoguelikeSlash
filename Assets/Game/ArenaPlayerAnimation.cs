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
    Texture2D[] idleFrames, moveFrames;
    Material playerAnimationMaterial;
    bool walkingAnimation;
    float animationPhase;
    Rigidbody2D body;

    internal void Initialize(Rigidbody2D playerBody, Color tint)
    {
        body = playerBody;
        idleFrames = LoadPlayerFrames("idle");
        moveFrames = LoadPlayerFrames("move");
        if (idleFrames.Length == 0 || moveFrames.Length == 0) return;

        playerAnimationMaterial = GetComponent<MeshRenderer>().sharedMaterial;
        playerAnimationMaterial.color = tint;
        animationPhase = 0f;
        playerAnimationMaterial.mainTexture = idleFrames[0];
    }

    static Texture2D[] LoadPlayerFrames(string state)
    {
        var frames = Resources.LoadAll<Texture2D>("PlayerAnimation/" + state);
        Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name));
        return frames;
    }

    void LateUpdate()
    {
        if (!playerAnimationMaterial || !body) return;
        Vector2 velocity = body.velocity;
        float speed = velocity.magnitude;
        bool walking = speed > 0.1f;
        if (walking != walkingAnimation)
        {
            walkingAnimation = walking;
            animationPhase = 0f;
        }

        var frames = walking ? moveFrames : idleFrames;
        float playbackSpeed = walking ? movePlaybackSpeed : idlePlaybackSpeed;
        if (walking && matchMovementSpeed)
            playbackSpeed *= speed / Mathf.Max(0.01f, referenceWalkSpeed);
        animationPhase = Mathf.Repeat(animationPhase + Time.deltaTime * playbackSpeed / PlayerAnimationLoopSeconds, 1f);
        int frame = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(animationPhase * frames.Length));
        playerAnimationMaterial.mainTexture = frames[frame];
        float horizontal = velocity.x;
        if (walking && Mathf.Abs(horizontal) > 0.01f && playerAnimationMaterial.HasProperty("_Flip"))
        {
            bool left = horizontal < 0;
            playerAnimationMaterial.SetVector("_Flip", new Vector4(left ? -1f : 1f, 1f, 1f, 1f));
        }
    }
}
