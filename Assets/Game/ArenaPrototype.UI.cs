using UnityEngine;

public sealed partial class ArenaPrototype
{
    [SerializeField, InspectorName("战斗界面")] ArenaHud hud;
    float fusionAppearedAt;
    float UiScale => hud ? hud.Scale : 1f;

    Rect SkillRect(int index)
    {
        if (hud) return hud.SlotRect(index);
        float offset = index == 0 ? 111 : index == 1 ? 158 : 65;
        return new Rect(Screen.width - offset - 34, Screen.height - (index == 0 ? 165 : 88), 68, 68);
    }

    void UpdateHud()
    {
        if (!hud) return;
        int seconds = Mathf.FloorToInt(elapsed);
        string time = $"{seconds / 60:00}:{seconds % 60:00}";
        hud.SetScore(time, kills);
        hud.SetHealth(runStats ? runStats.HP : hp, runStats ? runStats.Value(RunStat.MaxHP) : 100);
        hud.SetJoystick(hp > 0, finger >= 0, touchOrigin, stick);
        hud.SetResults(hp <= 0, time, kills);
        hud.retry.interactable = hp <= 0;

        int combination = fusion == Fusion.None ? -1 : (int)fusion - 1;
        float progress = Mathf.Clamp01((Time.unscaledTime - fusionAppearedAt) / hud.AppearDuration);
        float scale = Mathf.Lerp(0.24f, 1f, 1f - Mathf.Pow(1f - progress, 3f));
        for (int i = 0; i < 3; i++)
        {
            var button = hud.slots[i];
            bool active = cast != Cast.Idle && i == (int)skill;
            bool combined = combination >= 0 && i == fusionTarget;
            button.gameObject.SetActive(hp > 0 && !HiddenByFusion(i) && !combined && !(draggingSkill && active));
            float remaining = Mathf.Max(0, readyAt[i] - elapsed);
            Color accent = i == 0 ? guardColor : i == 1 ? dashColor : slashColor;
            Texture2D icon = i == 0 ? guardIcon : i == 1 ? dashIcon : slashIcon;
            button.Show(accent, icon, remaining, cooldownLengths[i], active && !draggingSkill ? SkillDurationRemaining() : -1);
            if (draggingSkill && i == (int)skill)
            {
                hud.drag.CopyText(button);
                hud.PositionAtScreen(hud.drag.Rect, dragPoint);
                hud.drag.Show(accent, icon, 0, 1, -1);
            }
        }
        hud.drag.gameObject.SetActive(hp > 0 && draggingSkill);
        for (int i = 0; i < hud.combinations.Length; i++)
        {
            var button = hud.combinations[i];
            bool visible = hp > 0 && i == combination;
            button.gameObject.SetActive(visible);
            if (!visible) continue;
            hud.AlignToSlot(button, fusionTarget, scale);
            button.Show(FusionColor(i), null, 0, 1, cast == Cast.Idle ? -1 : SkillDurationRemaining());
            button.SetCharges(guardSlashPhase == GuardSlashPhase.Counter, perfectDodge ? 4 : 2, counterRemaining);
        }
        hud.cancel.gameObject.SetActive(hp > 0 && combination >= 0 && cast == Cast.Idle);
        if (hud.cancel.gameObject.activeSelf) hud.AlignToSlot(hud.cancel, fusionSource, scale);
        int warnings = 0;
        if (hp > 0)
            foreach (var enemy in enemies)
                if (IsMeleeWarning(enemy))
                    hud.SetWarning(warnings++, view.WorldToScreenPoint((Vector3)enemy.Position + Vector3.up * 0.74f));
        hud.HideWarningsFrom(warnings);
        float notice = hp > 0 ? perfectNoticeUntil - Time.time : 0;
        hud.SetDodgeNotice(notice, view.WorldToScreenPoint(body.position), dodgeLook ? dodgeLook.color : new Color(1f, .9f, .53f));
    }

    float SkillDurationRemaining()
    {
        if (guardSlashPhase == GuardSlashPhase.Counter)
            return (counterUntil - Time.time) / counterTime;
        if (guardSlashPhase == GuardSlashPhase.FinishGuard)
            return (guardRemaining * 0.22f + Mathf.Max(0, nextGuardStrike - elapsed)) / 0.66f;
        if (cast == Cast.Active)
        {
            if (Dashing) return (dashUntil - Time.fixedTime) / dashDuration;
            if (arcCount > 0) return (finishAt - elapsed) / arcTravelTime;
            return (finishAt - elapsed) / Mathf.Max(0.01f, finishAt - activeStarted);
        }
        if (cast == Cast.Pending)
            return 1f - (elapsed - castAt) / HoldTime;
        if (HoldingGuardDash)
            return 1f - (elapsed - holdStarted) / GuardDashHoldDuration;
        if (HoldingGuardSlash)
            return 1f - (elapsed - holdStarted) / guardTime;
        if (!castingFusion && skill == Skill.Guard)
            return (baseGuardLimit - (elapsed - castAt)) / Mathf.Max(0.01f, baseGuardLimit - HoldTime);
        if (!castingFusion && skill == Skill.Slash)
            return 1f - (elapsed - holdStarted) / slashHoldLimit;
        // Aiming lasts until release.
        return 1f;
    }
}
