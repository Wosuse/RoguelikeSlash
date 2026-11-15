using UnityEngine;

public sealed partial class ArenaPrototype
{
    [SerializeField, InspectorName("技能冷却与按键配置")] ArenaSkillOptions[] skillOptions;
    [SerializeField, InspectorName("组合外观")] ArenaSkillAppearance[] fusionLooks;
    [SerializeField, InspectorName("技能音效")] ArenaSkillAudio[] skillSounds;
    [SerializeField, InspectorName("玩家刚体")] Rigidbody2D scenePlayer;
    [SerializeField, InspectorName("敌人实例")] Transform enemyInstances;
    [SerializeField, InspectorName("玩家子弹")] Transform playerShots;
    [SerializeField, InspectorName("敌人子弹")] Transform enemyShots;
    [SerializeField, InspectorName("技能特效实例")] Transform effectInstances;
    [SerializeField, InspectorName("防御波外观")] ArenaSkillAppearance guardWaveLook;
    [SerializeField, InspectorName("斩击轨迹外观")] ArenaSkillAppearance slashSweepLook;
    [SerializeField, InspectorName("A+C反击外观")] ArenaSkillAppearance counterLook;
    [SerializeField, InspectorName("A+C结束防御外观")] ArenaSkillAppearance finishGuardLook;
    [SerializeField, InspectorName("A+C闪避外观")] ArenaSkillAppearance dodgeLook;

    int SkillConfigIndex => castingFusion ? (int)fusion + 2 : (int)skill;
    ArenaSkillOptions CurrentOptions => skillOptions != null && SkillConfigIndex < skillOptions.Length ? skillOptions[SkillConfigIndex] : null;
    ArenaSkillAudio CurrentAudio => skillSounds != null && SkillConfigIndex < skillSounds.Length ? skillSounds[SkillConfigIndex] : null;
    ArenaSkillAppearance CurrentLook(int index) => fusionLooks != null && index >= 0 && index < fusionLooks.Length ? fusionLooks[index] : null;
    Color FusionColor(int index) => CurrentLook(index) ? CurrentLook(index).color : skillEffectColor;
    Material FusionMaterial(int index) => CurrentLook(index) && CurrentLook(index).material
        ? CurrentLook(index).material : spriteMaterial;
    Transform EffectParent => effectInstances ? effectInstances : transform;
    float HoldTime => CurrentOptions ? CurrentOptions.holdThreshold : holdThreshold;

    internal string SkillStatus(int index)
    {
        if (!Application.isPlaying) return "编辑模式";
        bool selected = index < 3 ? !castingFusion && index == (int)skill : index == (int)fusion + 2 && fusion != Fusion.None;
        string phase = !selected ? "待命" : cast == Cast.Pending ? "点按判定" :
            cast == Cast.Holding ? "长按" : cast == Cast.Active ? "释放中" : "待命";
        if (selected && index == 5)
        {
            if (guardSlashPhase == GuardSlashPhase.Dodge) phase = "闪避";
            if (guardSlashPhase == GuardSlashPhase.Counter) phase = "反击";
            if (guardSlashPhase == GuardSlashPhase.FinishGuard) phase = "结束防御";
        }
        float remaining = index < 3 ? Mathf.Max(0, readyAt[index] - elapsed) : lastCombinationReadyAt[index - 3] - elapsed;
        return $"{phase}　冷却 {Mathf.Max(0, remaining):0.0} 秒";
    }
    [SerializeField, InspectorName("按钮A+B / 功能")] GuardDashTuning guardDashTuning;
    [SerializeField, InspectorName("按钮A_防御 / 功能")] GuardTuning guardTuning;
    [SerializeField, InspectorName("按钮B_冲刺 / 功能")] DashTuning dashTuning;
    [SerializeField, InspectorName("按钮C_斩击 / 功能")] SlashTuning slashTuning;
    [SerializeField, InspectorName("按钮A+C / 功能")] GuardSlashTuning guardSlashTuning;
    [SerializeField, InspectorName("按钮B+C / 功能")] DashSlashTuning dashSlashTuning;
    [SerializeField, InspectorName("按钮A_防御 / 外观")] GuardVisualTuning guardVisualTuning;
    [SerializeField, InspectorName("按钮B_冲刺 / 外观")] DashVisualTuning dashVisualTuning;
    [SerializeField, InspectorName("按钮C_斩击 / 外观")] SlashVisualTuning slashVisualTuning;

    float tapGuardCooldown => guardTuning ? guardTuning.tapGuardCooldown : 2.5f;
    float baseGuardLimit => guardTuning ? guardTuning.baseGuardLimit : 3f;
    float guardWaveSpeed => guardTuning ? guardTuning.guardWaveSpeed : 2f;
    float guardWaveLifetime => guardTuning ? guardTuning.guardWaveLifetime : 1.8f;
    float guardRadius => guardTuning ? guardTuning.guardRadius : 1.05f;
    int guardWaveDamage => guardTuning ? guardTuning.guardWaveDamage : 35;
    float parryWindow => guardTuning ? guardTuning.parryWindow : 0.2f;
    float guardDamageMultiplier => guardTuning ? guardTuning.guardDamageMultiplier : 0.5f;
    float dashDuration => dashTuning ? dashTuning.dashDuration : 0.2f;
    float dashDistance => dashTuning ? dashTuning.dashDistance : 3f;
    float aimedMoveMultiplier => dashTuning ? dashTuning.aimedMoveMultiplier : 0.55f;
    float slashRadius => slashTuning ? slashTuning.slashRadius : 2.2f;
    float spinInterval => slashTuning ? slashTuning.spinInterval : 0.35f;
    int slashDamage => slashTuning ? slashTuning.slashDamage : 34;
    int spinDamage => slashTuning ? slashTuning.spinDamage : 14;
    int aimedDamage => slashTuning ? slashTuning.aimedDamage : 45;
    float slashHoldLimit => slashTuning ? slashTuning.slashHoldLimit : 3f;
    float slashSweepWidth => slashTuning ? slashTuning.slashSweepWidth : 0.24f;
    float DodgeDistance => guardSlashTuning ? guardSlashTuning.dodgeDistance : 1f;
    float counterTime => guardSlashTuning ? guardSlashTuning.counterTime : 3f;
    float guardTime => guardSlashTuning ? guardSlashTuning.guardTime : 5f;
    float perfectDodgeEnemySpeed => guardSlashTuning ? guardSlashTuning.perfectDodgeEnemySpeed : 0.1f;
    float guardSlashDamageMultiplier => guardSlashTuning ? guardSlashTuning.guardSlashDamageMultiplier : 0.4f;
    float dodgeLookAhead => guardSlashTuning ? guardSlashTuning.dodgeLookAhead : 0.12f;
    float dodgeTolerance => guardSlashTuning ? guardSlashTuning.dodgeTolerance : 0.18f;
    float bulletTimeClockRate => guardSlashTuning ? guardSlashTuning.bulletTimeClockRate : 0.3f;
    float bulletTimePlayerSpeed => guardSlashTuning ? guardSlashTuning.bulletTimePlayerSpeed : 1.6f;
    float arcTravelTime => dashSlashTuning ? dashSlashTuning.arcTravelTime : 0.36f;
    Color tapGuardColor => guardVisualTuning ? guardVisualTuning.tapGuardColor : new Color(1f, 0.82f, 0.27f);
    Color holdGuardColor => guardVisualTuning ? guardVisualTuning.holdGuardColor : new Color(0.72f, 0.55f, 0.26f);
    Color guardColor => guardVisualTuning ? guardVisualTuning.guardColor : new Color(0.37f, 0.73f, 1f);
    Texture2D guardIcon => guardVisualTuning ? guardVisualTuning.guardIcon : null;
    Color dashTrailColor => dashVisualTuning ? dashVisualTuning.dashTrailColor : new Color(1f, 0.22f, 0.25f);
    float dashTrailLifetime => dashVisualTuning ? dashVisualTuning.dashTrailLifetime : 0.22f;
    Color dashColor => dashVisualTuning ? dashVisualTuning.dashColor : new Color(1f, 0.69f, 0.36f);
    Texture2D dashIcon => dashVisualTuning ? dashVisualTuning.dashIcon : null;
    Color ringSlashColor => slashVisualTuning ? slashVisualTuning.ringSlashColor : new Color(0.26f, 0.67f, 1f);
    Color slashColor => slashVisualTuning ? slashVisualTuning.slashColor : new Color(0.88f, 0.58f, 1f);
    Texture2D slashIcon => slashVisualTuning ? slashVisualTuning.slashIcon : null;
}
