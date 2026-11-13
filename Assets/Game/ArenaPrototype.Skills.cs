using UnityEngine;

public sealed partial class ArenaPrototype
{
    enum Skill { Guard, Dash, Slash }
    enum Cast { Idle, Pending, Holding, Active }
    readonly float[] readyAt = new float[3];
    Cast cast;
    Skill skill;
    int skillFinger;
    float castAt, finishAt, nextSpin;
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
            canceled = true;
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
        if (cast == Cast.Pending && elapsed - castAt >= holdThreshold)
        {
            cast = Cast.Holding;
            nextSpin = elapsed;
        }
        if (cast == Cast.Holding)
        {
            var drag = point - pressPoint;
            if (skill == Skill.Dash && drag.sqrMagnitude > 144f * UiScale * UiScale) aim = drag.normalized;
            if (skill == Skill.Dash && !released) DrawSkillEffect(aim, 0.08f);
            if (skill == Skill.Slash && !released && elapsed >= nextSpin)
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
        for (int i = 0; i < readyAt.Length; i++)
        {
            if (elapsed < readyAt[i] || !SkillRect(i).Contains(guiPoint)) continue;
            skill = (Skill)i;
            skillFinger = pointer;
            castAt = elapsed;
            pressPoint = point;
            aim = facing;
            guardCharge = 0;
            cast = Cast.Pending;
            return true;
        }
        return false;
    }

    void ReleaseSkill()
    {
        bool held = cast == Cast.Holding;
        cast = Cast.Active;
        if (skill != Skill.Slash) PlaySound(skill == Skill.Guard ? guardSound : dashSound);
        if (skill == Skill.Guard)
        {
            finishAt = elapsed + (held ? 0.16f : parryWindow);
            shortParry = !held;
            if (held && guardCharge > 0) Ring(Mathf.Min(48, guardCharge * 14), slashRadius);
        }
        else if (skill == Skill.Dash)
        {
            finishAt = elapsed + 0.2f;
            if (held)
            {
                Vector2 right = new Vector2(-aim.y, aim.x);
                for (int i = enemies.Count - 1; i >= 0; i--)
                {
                    var delta = enemies[i].Position - body.position;
                    float forward = Vector2.Dot(delta, aim);
                    if (forward >= 0 && forward <= 4.5f && Mathf.Abs(Vector2.Dot(delta, right)) <= 0.8f)
                        HitEnemy(i, aimedDamage, aim);
                }
                DrawSkillEffect(aim, 0.2f);
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
            finishAt = elapsed + 0.16f;
            if (!held) Ring(slashDamage, slashRadius);
        }
    }

    float dashUntil;
    bool Dashing => cast == Cast.Active && skill == Skill.Dash && Time.fixedTime < dashUntil;
    bool Parrying => cast == Cast.Active && skill == Skill.Guard && shortParry;
    float SkillMoveScale => cast != Cast.Holding ? 1f : skill == Skill.Guard ? 0.55f : skill == Skill.Slash ? 0.75f : 0f;

    void FinishSkill()
    {
        readyAt[(int)skill] = elapsed + skillCooldown;
        cast = Cast.Idle;
        dashUntil = 0;
    }

    void InterruptSkill()
    {
        if (cast == Cast.Active || cast == Cast.Holding) FinishSkill();
        else cast = Cast.Idle;
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
        DrawSkillEffect(Vector2.zero, 0.16f, radius);
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
