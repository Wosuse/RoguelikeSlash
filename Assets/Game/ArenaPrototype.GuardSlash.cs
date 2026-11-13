using UnityEngine;

public sealed partial class ArenaPrototype
{
    enum GuardSlashPhase { None, Dodge, Counter, FinishGuard }
    [SerializeField, Min(0.1f)] float counterTime = 3f;
    [SerializeField, Min(0.1f)] float guardTime = 5f;
    [SerializeField, Range(0.01f, 1)] float perfectDodgeEnemySpeed = 0.1f;
    [SerializeField, Range(0, 1)] float guardSlashDamageMultiplier = 0.4f;
    GuardSlashPhase guardSlashPhase;
    [SerializeField, Range(0f, 0.25f)] float dodgeLookAhead = 0.12f;
    [SerializeField, Range(0f, 0.5f)] float dodgeTolerance = 0.18f;
    float perfectNoticeUntil;
    bool perfectDodge, guardStrikeQueued;
    int guardRemaining, counterRemaining;
    float counterUntil, nextGuardStrike;

    bool HoldingGuardSlash => castingFusion && fusion == Fusion.GuardSlash && cast == Cast.Holding;
    bool Dodging => guardSlashPhase == GuardSlashPhase.Dodge && Dashing;
    bool PerfectCounter => perfectDodge && guardSlashPhase == GuardSlashPhase.Counter;
    [SerializeField, Range(0.05f, 1f)] float bulletTimeClockRate = 0.3f;
    [SerializeField, Min(1f)] float bulletTimePlayerSpeed = 1.6f;
    bool BulletTime => perfectDodge && (Dodging || PerfectCounter);
    float EnemyTimeMultiplier => BulletTime ? perfectDodgeEnemySpeed : 1f;
    float ClockRate => BulletTime ? bulletTimeClockRate : 1f;

    void ResetGuardSlash()
    {
        guardSlashPhase = GuardSlashPhase.None;
        perfectDodge = guardStrikeQueued = false;
        guardRemaining = 3;
        counterRemaining = 0;
        nextGuardStrike = 0;
    }

    void ReleaseGuardSlash(bool held)
    {
        if (held)
        {
            guardSlashPhase = GuardSlashPhase.FinishGuard;
            return;
        }
        perfectDodge |= InWarnedAttackRange();
        guardSlashPhase = GuardSlashPhase.Dodge;
        aim = facing;
        dashStart = body.position;
        dashUntil = Time.fixedTime + dashDuration;
        PlaySound(dashSound);
    }

    bool TickGuardSlash()
    {
        if (guardSlashPhase == GuardSlashPhase.Dodge)
        {
            if (Dashing) return true;
            guardSlashPhase = GuardSlashPhase.Counter;
            counterRemaining = perfectDodge ? 4 : 2;
            if (perfectDodge) perfectNoticeUntil = Time.time + 1.15f;
            counterUntil = Time.time + counterTime;
            finishAt = elapsed;
            return true;
        }
        if (guardSlashPhase == GuardSlashPhase.Counter)
        {
            if (Time.time >= counterUntil || counterRemaining == 0)
                return elapsed < finishAt;
            if (elapsed >= finishAt && CounterPressed())
            {
                counterRemaining--;
                Ring(slashDamage, slashRadius);
                finishAt = elapsed + 0.16f * ClockRate;
            }
            return true;
        }
        if (guardSlashPhase == GuardSlashPhase.FinishGuard)
        {
            if (guardStrikeQueued || elapsed < nextGuardStrike) return true;
            if (guardRemaining <= 0) return false;
            guardRemaining--;
            Ring(slashDamage, slashRadius);
            nextGuardStrike = elapsed + 0.22f;
            return true;
        }
        return false;
    }

    bool CounterPressed()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Began && CounterContains(touch.position)) return true;
        }
        return Input.touchCount == 0 && Input.GetMouseButtonDown(0) && CounterContains(Input.mousePosition);
    }

    bool CounterContains(Vector2 point) =>
        SkillRect(fusionTarget).Contains(new Vector2(point.x, Screen.height - point.y) / UiScale);

    void FlushGuardStrike()
    {
        if (!guardStrikeQueued || hp == 0) return;
        guardStrikeQueued = false;
        Ring(slashDamage, slashRadius);
    }

    void DetectDodgedAttack(Vector2 attackStart, Vector2 attackStep, float radius, float dt)
    {
        if (!Dodging) return;
        Vector2 predicted = attackStep * (1f + dodgeLookAhead / Mathf.Max(dt, 0.001f));
        if (SegmentHit(attackStart, attackStep, body.position, radius, out _) ||
            SegmentHit(attackStart, predicted, dashStart, radius + dodgeTolerance, out _))
            perfectDodge = true;
    }
}
