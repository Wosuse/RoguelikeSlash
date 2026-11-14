using System.Collections.Generic;
using UnityEngine;

public sealed partial class ArenaPrototype
{
    enum Fusion { None, GuardDash, DashSlash, GuardSlash }
    Fusion fusion;
    bool castingFusion, draggingSkill, waitingForArcs;
    Vector2 dragPoint, dashStart;
    float arcStarted, arcDistance;
    int arcCount;
    readonly LineRenderer[] arcLines = new LineRenderer[2];
    readonly HashSet<int>[] arcHits = { new HashSet<int>(), new HashSet<int>() };

    int fusionSource, fusionTarget;
    bool HiddenByFusion(int index) => fusion != Fusion.None && index == fusionSource;
    bool CanAim => castingFusion ? fusion != Fusion.GuardSlash : skill == Skill.Dash;

    Rect CancelFusionRect() => SkillRect(fusionSource);

    bool HandleFusionGesture(Vector2 point, bool released)
    {
        if (castingFusion || fusion != Fusion.None || tapGuardUsed) return false;
        dragPoint = point;
        if ((point - pressPoint).sqrMagnitude > 576f * UiScale * UiScale) draggingSkill = true;
        if (!draggingSkill) return false;
        if (!released) return true;

        Vector2 gui = new Vector2(point.x, Screen.height - point.y) / UiScale;
        for (int target = 0; target < readyAt.Length; target++)
        {
            if (target == (int)skill || elapsed < readyAt[target] || !FusionDropRect(target).Contains(gui)) continue;
            int pair = target + (int)skill;
            if (pair == 1) fusion = Fusion.GuardDash;
            if (pair == 3) fusion = Fusion.DashSlash;
            if (pair == 2) fusion = Fusion.GuardSlash;
            fusionSource = (int)skill;
            fusionTarget = target;
            fusionAppearedAt = Time.unscaledTime;
            break;
        }
        draggingSkill = false;
        cast = Cast.Idle;
        return true;
    }

    Rect FusionDropRect(int index)
    {
        Rect rect = SkillRect(index);
        rect.xMin -= 10;
        rect.xMax += 10;
        rect.yMin -= 10;
        rect.yMax += 10;
        return rect;
    }

    void ReleaseFusion(bool held)
    {
        if (fusion == Fusion.GuardSlash)
        {
            ReleaseGuardSlash(held);
            return;
        }
        if (!held)
        {
            aim = facing;
            dashStart = body.position;
            dashUntil = Time.fixedTime + dashDuration;
            finishAt = elapsed + dashDuration;
            waitingForArcs = fusion == Fusion.DashSlash;
            shortParry = fusion == Fusion.GuardDash;
            PlaySound(dashSound);
            return;
        }
        if (fusion == Fusion.GuardDash)
        {
            RectangleAttack(aimedDamage + guardCharge * (guardDashTuning ? guardDashTuning.damagePerCharge : 10));
            finishAt = elapsed + 0.2f;
            PlaySound(guardSound);
        }
        else StartArcs(body.position, 5f, 1);
    }

    void RectangleAttack(int damage)
    {
        Vector2 right = new Vector2(-aim.y, aim.x);
        float reach = guardDashTuning ? guardDashTuning.counterReach : 4.5f;
        float halfWidth = guardDashTuning ? guardDashTuning.counterHalfWidth : 0.8f;
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Vector2 delta = enemies[i].Position - body.position;
            float forward = Vector2.Dot(delta, aim);
            if (forward >= 0 && forward <= reach && Mathf.Abs(Vector2.Dot(delta, right)) <= halfWidth)
                HitEnemy(i, damage, aim);
        }
        DrawSkillEffect(aim, 0.2f);
    }

    void StartArcs(Vector2 origin, float distance, int count)
    {
        waitingForArcs = false;
        dashStart = origin;
        arcDistance = distance;
        arcStarted = elapsed;
        arcCount = count;
        finishAt = elapsed + arcTravelTime;
        for (int i = 0; i < count; i++)
        {
            arcHits[i].Clear();
            if (!arcLines[i])
            {
                var line = new GameObject("Traveling slash").AddComponent<LineRenderer>();
                line.transform.SetParent(EffectParent);
                line.sharedMaterial = spriteMaterial;
                line.startColor = line.endColor = skillEffectColor;
                line.startWidth = line.endWidth = 0.12f;
                line.positionCount = 13;
                arcLines[i] = line;
            }
            arcLines[i].sharedMaterial = FusionMaterial((int)fusion - 1);
            arcLines[i].startColor = arcLines[i].endColor = FusionColor((int)fusion - 1);
            arcLines[i].enabled = true;
        }
        PlaySound(slashSound);
    }

    void TickArcs()
    {
        if (waitingForArcs && !Dashing)
        {
            Vector2 displacement = body.position - dashStart;
            if (displacement.sqrMagnitude > 0.0001f) aim = displacement.normalized;
            StartArcs(dashStart, displacement.magnitude, 2);
        }
        if (arcCount == 0) return;

        float progress = Mathf.Clamp01((elapsed - arcStarted) / arcTravelTime);
        Vector2 across = new Vector2(-aim.y, aim.x);
        for (int arc = 0; arc < arcCount; arc++)
        {
            float halfWidth = arcCount == 1 ? 1.65f : 0.8f;
            float offset = arcCount == 1 ? 0f : arc == 0 ? -0.75f : 0.75f;
            Vector2 center = dashStart + aim * (arcDistance * progress) + across * offset;
            for (int k = 0; k < 13; k++)
            {
                float side = k / 6f - 1f;
                Vector2 position = center + across * (side * halfWidth) - aim * (side * side * 0.65f);
                arcLines[arc].SetPosition(k, new Vector3(position.x, position.y, -0.25f));
            }
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Actor enemy = enemies[i];
                Vector2 delta = enemy.Position - center;
                float side = Vector2.Dot(delta, across) / halfWidth;
                float depth = Vector2.Dot(delta, aim) + Mathf.Min(1, side * side) * 0.65f;
                float sweep = arcDistance * Time.deltaTime / arcTravelTime;
                if (Mathf.Abs(side) <= 1.25f && depth <= 0.5f && depth >= -0.5f - sweep &&
                    arcHits[arc].Add(enemy.SpawnId))
                    HitEnemy(i, arcCount == 1 ? aimedDamage : slashDamage, aim);
            }
        }
    }

    void ClearArcs()
    {
        waitingForArcs = false;
        arcCount = 0;
        foreach (var line in arcLines)
            if (line) line.enabled = false;
    }
}