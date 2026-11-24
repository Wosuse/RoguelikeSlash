using UnityEngine;

public sealed partial class V06Skills
{
    [Header("动画装配（角色和剑气共用同一配置资产）")]
    [InspectorName("技能动画配置")] public SkillAnimationSet animationSet;
    bool slashIsRing;
    Vector2 animationFacing;
    public PlayerSkillMotion AnimationMotion => dodgeLeft > 0 ? (backwardDodge ? PlayerSkillMotion.Dodge : PlayerSkillMotion.ForwardDodge)
        : charging ? PlayerSkillMotion.Charge
        : slashLeft > 0 ? (slashIsRing ? PlayerSkillMotion.Spin : PlayerSkillMotion.Slash)
        : sprintLeft > 0 && arena.HasMovementInput ? PlayerSkillMotion.Run : PlayerSkillMotion.None;
    public float AnimationElapsed => dodgeLeft > 0 ? Mathf.Max(0, dodgeDuration - dodgeLeft)
        : charging ? chargeTime
        : slashLeft > 0 ? Mathf.Max(0, slashDuration - slashLeft)
        : sprintLeft > 0 ? Mathf.Max(0, sprintDuration - sprintLeft) : 0;
    public float AnimationDuration => dodgeLeft > 0 ? dodgeDuration : slashLeft > 0 ? slashDuration : 0;
    public Vector2 AnimationFacing => AnimationMotion == PlayerSkillMotion.ForwardDodge ? dashAim
        : AnimationMotion == PlayerSkillMotion.Slash || AnimationMotion == PlayerSkillMotion.Spin
        ? animationFacing : arena.V06Facing;
}
