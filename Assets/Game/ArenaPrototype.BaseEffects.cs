using System.Collections.Generic;
using UnityEngine;

public sealed partial class ArenaPrototype
{
    TrailRenderer dashTrail;

    void TickDashTrail()
    {
        bool emitting = Dashing && guardSlashPhase != GuardSlashPhase.Dodge;
        if (!dashTrail && emitting)
        {
            var trailObject = new GameObject("Dash trail");
            trailObject.transform.SetParent(body.transform, false);
            trailObject.transform.localPosition = new Vector3(0, 0, -0.1f);
            dashTrail = trailObject.AddComponent<TrailRenderer>();
            dashTrail.sharedMaterial = spriteMaterial;
            dashTrail.startWidth = 0.62f;
            dashTrail.endWidth = 0.04f;
            dashTrail.minVertexDistance = 0.035f;
            dashTrail.emitting = false;
        }
        if (!dashTrail) return;
        dashTrail.time = dashTrailLifetime;
        dashTrail.startColor = dashTrailColor;
        var transparent = dashTrailColor;
        transparent.a = 0;
        dashTrail.endColor = transparent;
        dashTrail.emitting = emitting;
    }

    LineRenderer stanceLine, sweepLine, waveLine;
    bool tapGuardUsed, guardWaveActive, shortSlash, baseGuardTap;
    Vector2 waveOrigin, waveDirection;
    float waveStarted, sweepStarted = -1f, sweepRadius;
    const float SweepTime = 0.16f;
    readonly HashSet<int> waveHits = new HashSet<int>();

    bool BaseGuardPressed => !castingFusion && skill == Skill.Guard &&
        !draggingSkill && (cast == Cast.Pending || cast == Cast.Holding);

    void StartGuardWave()
    {
        guardWaveActive = true;
        waveOrigin = body.position;
        waveDirection = facing;
        waveStarted = elapsed;
        finishAt = elapsed + guardWaveLifetime;
        waveHits.Clear();
    }

    void TickGuardWave()
    {
        if (!guardWaveActive) return;
        float age = Mathf.Clamp(elapsed - waveStarted, 0, guardWaveLifetime);
        Vector2 center = waveOrigin + waveDirection * (age * guardWaveSpeed);
        Vector2 step = waveDirection * (Mathf.Min(age, Time.deltaTime) * guardWaveSpeed);
        DrawCircle(ref waveLine, "防御波", center, guardRadius, guardWaveLook ? guardWaveLook.color : holdGuardColor);
        waveLine.sharedMaterial = guardWaveLook && guardWaveLook.material ? guardWaveLook.material : spriteMaterial;
        waveLine.startWidth = waveLine.endWidth = guardWaveLook ? guardWaveLook.width : .085f;
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            var enemy = enemies[i];
            if (SegmentHit(center - step, step, enemy.Position, guardRadius + 0.4f, out _) &&
                waveHits.Add(enemy.SpawnId))
                HitEnemy(i, guardWaveDamage, waveDirection);
        }
    }

    void LateSkillVisuals()
    {
        TickDashTrail();
        if (stanceLine) stanceLine.enabled = false;
        if (BaseGuardPressed)
        {
            Color color = cast == Cast.Pending ? tapGuardColor : holdGuardColor;
            if (cast == Cast.Holding)
            {
                float left = baseGuardLimit - (elapsed - castAt);
                if (left < 0.9f)
                {
                    float progress = Mathf.Clamp01(1f - left / 0.9f);
                    color.a = Mathf.Lerp(0.22f, 1f, Mathf.Abs(Mathf.Sin(8f * progress + 14f * progress * progress)));
                }
            }
            DrawCircle(ref stanceLine, "Guard ring", body.transform.position, guardRadius, color);
            stanceLine.startWidth = stanceLine.endWidth = 0.085f;
        }
        else if (castingFusion && fusion == Fusion.GuardDash && (Dashing || cast == Cast.Holding))
        {
            DrawCircle(ref stanceLine, "Guard dash shield", body.transform.position, guardRadius,
                Dashing ? tapGuardColor : holdGuardColor);
            stanceLine.startWidth = stanceLine.endWidth = 0.16f;
        }
        else if (!castingFusion && skill == Skill.Slash && cast == Cast.Holding)
        {
            Color color = ringSlashColor;
            color.a = 0.4f;
            DrawCircle(ref stanceLine, "Slash range", body.transform.position, slashRadius, color);
            stanceLine.startWidth = stanceLine.endWidth = slashSweepWidth * 0.65f;
        }

        if (sweepStarted < 0 || elapsed - sweepStarted >= SweepTime)
        {
            if (sweepLine) sweepLine.enabled = false;
            return;
        }
        var line = SkillLine(ref sweepLine, "斩击轨迹", slashSweepLook ? slashSweepLook.color : ringSlashColor);
        var appearance = guardSlashPhase == GuardSlashPhase.Counter ? counterLook :
            guardSlashPhase == GuardSlashPhase.FinishGuard ? finishGuardLook : slashSweepLook;
        if (appearance) line.startColor = line.endColor = appearance.color;
        line.sharedMaterial = appearance && appearance.material ? appearance.material : spriteMaterial;
        line.startWidth = line.endWidth = appearance ? appearance.width : slashSweepWidth;
        Vector2 origin = body.transform.position;
        float angle = Mathf.PI * 2f * Mathf.Clamp01((elapsed - sweepStarted) / SweepTime);
        int segments = Mathf.Max(1, Mathf.CeilToInt(angle * 8));
        line.loop = false;
        line.positionCount = segments + 2;
        for (int i = 0; i <= segments; i++)
        {
            float a = angle * i / segments;
            Vector2 edge = origin + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * sweepRadius;
            line.SetPosition(i, new Vector3(edge.x, edge.y, -0.25f));
        }
        line.SetPosition(segments + 1, new Vector3(origin.x, origin.y, -0.25f));
    }

    void DrawCircle(ref LineRenderer cached, string name, Vector2 center, float radius, Color color)
    {
        var line = SkillLine(ref cached, name, color);
        line.loop = true;
        line.positionCount = 40;
        for (int i = 0; i < 40; i++)
        {
            float angle = i * Mathf.PI / 20;
            Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            line.SetPosition(i, new Vector3(point.x, point.y, -0.22f));
        }
    }

    LineRenderer SkillLine(ref LineRenderer line, string name, Color color)
    {
        if (!line)
        {
            line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(EffectParent);
            line.sharedMaterial = spriteMaterial;
            line.startWidth = line.endWidth = 0.085f;
        }
        line.startColor = line.endColor = color;
        line.enabled = true;
        return line;
    }

    void ClearBaseEffects()
    {
        guardWaveActive = shortSlash = false;
        if (waveLine) waveLine.enabled = false;
    }
}
