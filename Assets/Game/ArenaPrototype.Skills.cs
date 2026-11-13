using UnityEngine;

public sealed partial class ArenaPrototype
{
    enum Skill { Guard, Dash, Slash }
    enum Cast { Idle, Pending, Holding, Active }
    readonly float[] readyAt = new float[3];
    readonly float[] cooldownLengths = new float[3];
    Cast cast;
    Skill skill;
    int skillFinger;
    float castAt, holdStarted, activeStarted, finishAt, nextSpin;
    int guardCharge;
    bool shortParry, counterPending;
    Vector2 pressPoint, aim, facing = Vector2.right;
    LineRenderer skillEffect;
    float effectUntil;

    void TickSkills()
    {
        if (move.sqrMagnitude > 0.01f) facing = move.normalized;
        if (skillEffect && elapsed >= effectUntil) skillEffect.enabled = false;
        if (cast == Cast.Active)
        {
            if (castingFusion && fusion == Fusion.GuardSlash)
            {
                if (!TickGuardSlash()) FinishSkill();
                return;
            }
            if (castingFusion) TickArcs();
            TickGuardWave();
            if (elapsed >= finishAt && !Dashing) FinishSkill();
            return;
        }

        if (cast == Cast.Idle)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began && BeginSkill(touch.position, touch.fingerId)) return;
            }
            if (Input.touchCount == 0 && Input.GetMouseButtonDown(0))
                BeginSkill(Input.mousePosition, -1);
            return;
        }

        Vector2 point = Input.mousePosition;
        bool released = !Input.GetMouseButton(0), canceled = false;
        if (skillFinger >= 0)
        {
            released = true;
            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.fingerId != skillFinger) continue;
                point = touch.position;
                released = touch.phase == TouchPhase.Ended;
                canceled = touch.phase == TouchPhase.Canceled;
                break;
            }
        }
        if (canceled)
        {
            InterruptSkill();
            return;
        }
        if (cast == Cast.Pending && HandleFusionGesture(point, released)) return;
        if (cast == Cast.Pending && elapsed - castAt >= holdThreshold)
        {
            cast = Cast.Holding;
            holdStarted = elapsed;
            nextSpin = elapsed;
        }
        if (!castingFusion && skill == Skill.Slash && cast == Cast.Holding && elapsed - holdStarted >= slashHoldLimit)
        {
            ReleaseSkill();
            return;
        }
        if (BaseGuardPressed && cast == Cast.Holding && elapsed - castAt >= baseGuardLimit)
        {
            ReleaseSkill();
            return;
        }
        if (cast == Cast.Holding && castingFusion && fusion == Fusion.GuardSlash &&
            (elapsed - holdStarted >= guardTime || guardRemaining == 0))
        {
            ReleaseSkill();
            return;
        }
        if (cast == Cast.Holding)
        {
            var drag = point - pressPoint;
            if (CanAim)
                aim = drag.sqrMagnitude > 144f * UiScale * UiScale ? drag.normalized : facing;
            if (CanAim && !released) DrawSkillEffect(aim, 0.08f);
            if (!castingFusion && skill == Skill.Slash && !released && elapsed >= nextSpin)
            {
                Ring(spinDamage, slashRadius);
                nextSpin = elapsed + spinInterval;
            }
        }
        if (released) ReleaseSkill();
    }

    bool BeginSkill(Vector2 point, int pointer)
    {
        var guiPoint = new Vector2(point.x, Screen.height - point.y) / UiScale;
        if (fusion != Fusion.None && CancelFusionRect().Contains(guiPoint))
        {
            fusion = Fusion.None;
            return true;
        }
        for (int i = 0; i < readyAt.Length; i++)
        {
            if (HiddenByFusion(i) || elapsed < readyAt[i] || !SkillRect(i).Contains(guiPoint)) continue;
            skill = (Skill)i;
            castingFusion = fusion != Fusion.None && i == fusionTarget;
            draggingSkill = false;
            skillFinger = pointer;
            castAt = elapsed;
            pressPoint = point;
            aim = facing;
            guardCharge = 0;
            tapGuardUsed = baseGuardTap = false;
            if (!castingFusion && skill == Skill.Guard) PlaySound(guardSound);
                        if (castingFusion && fusion == Fusion.GuardSlash)
            {
                ResetGuardSlash();
                perfectDodge = InWarnedAttackRange();
            }
            cast = Cast.Pending;
            return true;
        }
        return false;
    }

    void ReleaseSkill()
    {
        bool held = cast == Cast.Holding;
        cast = Cast.Active;
        activeStarted = elapsed;
        shortParry = false;
        if (castingFusion) { ReleaseFusion(held); return; }
        if (skill == Skill.Dash) PlaySound(dashSound);
        if (skill == Skill.Guard)
        {
            baseGuardTap = !held;
            if (held) StartGuardWave();
            else FinishSkill();
        }
        else if (skill == Skill.Dash)
        {
            finishAt = elapsed + 0.2f;
            if (held)
            {
                RectangleAttack(aimedDamage);
            }
            else
            {
                aim = facing;
                finishAt = elapsed + dashDuration;
                dashUntil = Time.fixedTime + dashDuration;
            }
        }
        else
        {
            finishAt = held ? Mathf.Max(elapsed, sweepStarted + SweepTime) : elapsed + SweepTime;
            shortSlash = !held;
            if (shortSlash) Ring(slashDamage, slashRadius);
        }
    }

    float dashUntil;
    bool Dashing => cast == Cast.Active && (castingFusion || skill == Skill.Dash) && Time.fixedTime < dashUntil;
    bool Parrying => BaseGuardPressed && cast == Cast.Pending || cast == Cast.Active && shortParry && castingFusion && Dashing;
    float SkillMoveScale => shortSlash ? 0f : cast != Cast.Holding ? 1f : castingFusion ? (fusion == Fusion.GuardSlash ? 0.6f : 0f) : skill == Skill.Guard ? 0.55f : skill == Skill.Slash ? 0.75f : aimedMoveMultiplier;

    void FinishSkill()
    {
        if (castingFusion)
        {
            cooldownLengths[fusionTarget] = cooldownLengths[fusionSource] = fusionCooldown;
            readyAt[fusionTarget] = readyAt[fusionSource] = elapsed + fusionCooldown;
            fusion = Fusion.None;
        }
        else
        {
            cooldownLengths[(int)skill] = baseGuardTap ? tapGuardCooldown : skillCooldown;
            readyAt[(int)skill] = elapsed + cooldownLengths[(int)skill];
        }
        castingFusion = false;
        ClearArcs();
        ClearBaseEffects();
        ResetGuardSlash();
        cast = Cast.Idle;
        dashUntil = 0;
    }

    void InterruptSkill()
    {
        if (cast == Cast.Active || cast == Cast.Holding || tapGuardUsed && cast == Cast.Pending)
        {
            if (cast == Cast.Pending) baseGuardTap = true;
            FinishSkill();
        }
        else cast = Cast.Idle;
        draggingSkill = false;
        sweepStarted = -1f;
        if (skillEffect) skillEffect.enabled = false;
    }

    void Ring(int damage, float radius)
    {
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            var delta = enemies[i].Position - body.position;
            if (delta.sqrMagnitude <= radius * radius) HitEnemy(i, damage, delta.normalized);
        }
        PlaySound(slashSound);
        sweepStarted = elapsed;
        sweepRadius = radius;
    }

    void DrawSkillEffect(Vector2 direction, float duration, float radius = 2.2f)
    {
        if (!skillEffect)
        {
            skillEffect = new GameObject("Skill indicator").AddComponent<LineRenderer>();
            skillEffect.transform.SetParent(transform);
            skillEffect.sharedMaterial = skillEffectMaterial ? skillEffectMaterial : spriteMaterial;
            skillEffect.startWidth = skillEffect.endWidth = 0.085f;
            skillEffect.startColor = skillEffect.endColor = skillEffectColor;
        }
        Vector3 origin = new Vector3(body.position.x, body.position.y, -0.2f);
        bool circle = direction == Vector2.zero;
        skillEffect.loop = true;
        skillEffect.positionCount = circle ? 32 : 4;
        if (circle)
        {
            for (int i = 0; i < 32; i++)
            {
                float angle = i * Mathf.PI / 16;
                skillEffect.SetPosition(i, origin + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }
        else
        {
            Vector3 edge = new Vector3(-direction.y, direction.x) * 0.8f;
            Vector3 end = origin + (Vector3)(direction * 4.5f);
            skillEffect.SetPosition(0, origin - edge);
            skillEffect.SetPosition(1, end - edge);
            skillEffect.SetPosition(2, end + edge);
            skillEffect.SetPosition(3, origin + edge);
        }
        effectUntil = elapsed + duration;
        skillEffect.enabled = true;
    }

}
