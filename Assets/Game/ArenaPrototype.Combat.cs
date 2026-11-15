using System.Collections.Generic;
using UnityEngine;

public sealed partial class ArenaPrototype
{
    enum EnemyKind { Chaser, Charger, Shooter }
    [SerializeField, InspectorName("追击敌人动画")] EnemyAnimationProfile chaserAnimation;

    sealed class Actor
    {
        public GameObject Object;
        public Material Material;
        public EnemyKind Kind;
        public Vector2 Position, Direction;
        public float Timer, Rush, Aim;
        public float Health; public int SpawnId, RewardTier;
        public bool Hostile;
        public SpriteRenderer Animation;
        public float MoveAnimationTime, ReleaseRemaining;
    }

    readonly List<Actor> enemies = new List<Actor>(100);
    readonly List<Actor> bullets = new List<Actor>(160);
    readonly Stack<Actor> enemyPool = new Stack<Actor>();
    readonly Stack<Actor> bulletPool = new Stack<Actor>();
    int kills, spawnSequence;

    bool IsMeleeWarning(Actor enemy) => enemy.Kind != EnemyKind.Shooter &&
        enemy.Aim > 0f && enemy.Aim <= meleeWarningTime;

    bool InWarnedAttackRange()
    {
        foreach (var enemy in enemies)
        {
            if (!IsMeleeWarning(enemy)) continue;
            float range = enemy.Kind == EnemyKind.Charger ? 2.4f : meleeAttackRange;
            if ((body.position - enemy.Position).sqrMagnitude <= range * range) return true;
        }
        return false;
    }
    float nextSpawn, nextShot;
    static readonly Color ChaserColor = new Color(0.94f, 0.27f, 0.32f);
    static readonly Color ChargerColor = new Color(1f, 0.60f, 0.22f);
    static readonly Color ShooterColor = new Color(0.72f, 0.43f, 0.95f);

    Actor Rent(Stack<Actor> pool, Vector2 position, float size, Color color)
    {
        Actor actor;
        if (pool.Count == 0)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(obj.GetComponent<Collider>());
            obj.transform.SetParent(pool == enemyPool && enemyInstances ? enemyInstances : transform);
            var material = new Material(spriteMaterial);
            obj.GetComponent<Renderer>().sharedMaterial = material;
            actor = new Actor { Object = obj, Material = material };
        }
        else actor = pool.Pop();
        actor.Position = position;
        actor.Direction = Vector2.zero;
        actor.Timer = actor.Aim = actor.Rush = 0;
        actor.Health = 0;
        actor.Hostile = false;
        actor.Material.color = color;
        actor.Object.transform.localScale = Vector3.one * size;
        actor.Object.transform.rotation = Quaternion.identity;
        actor.Object.transform.position = position;
        actor.Object.SetActive(true);
        return actor;
    }

    static void Return(List<Actor> active, int index, Stack<Actor> pool)
    {
        var actor = active[index];
        actor.Object.SetActive(false);
        pool.Push(actor);
        active[index] = active[active.Count - 1];
        active.RemoveAt(active.Count - 1);
    }

    void TickCombat(float dt)
    {
        if (elapsed >= nextSpawn && enemies.Count < 100)
        {
            SpawnEnemy();
            nextSpawn = elapsed + Mathf.Lerp(0.8f, 0.25f, elapsed / 300f);
        }

        Actor closest = null;
        float closestDistance = 49;
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            var enemy = enemies[i];
            var delta = body.position - enemy.Position;
            float distance = delta.magnitude;
            if (distance > Mathf.Max(35, view.orthographicSize * view.aspect + 18))
            {
                Return(enemies, i, enemyPool);
                continue;
            }
            if (delta.sqrMagnitude < closestDistance)
            {
                closest = enemy;
                closestDistance = delta.sqrMagnitude;
            }
            TickEnemy(enemy, delta.normalized, distance, dt * EnemyTimeMultiplier);
            if (hp == 0) return;
        }

        SeparateEnemies();

        if (counterPending)
        {
            counterPending = false;
            Ring(30, 1.7f);
            if (closest != null && !closest.Object.activeSelf) closest = null;
        }

        if (!autoWeapons && CanAutoAttack && closest != null && elapsed >= nextShot &&
            Fire(body.position, (closest.Position - body.position).normalized, false))
            nextShot = elapsed + autoShotInterval;

        for (int i = bullets.Count - 1; i >= 0; i--)
        {
            TickBullet(i, bullets[i].Hostile ? dt * EnemyTimeMultiplier : dt);
            if (hp == 0) return;
        }
        FlushGuardStrike();
    }

    void SeparateEnemies()
    {
        if (enemySpacing <= 0 || enemies.Count < 2) return;
        float minSquared = enemySpacing * enemySpacing;
        // Two passes reduce overlap in groups while keeping the capped enemy count cheap.
        for (int pass = 0; pass < 2; pass++)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                var first = enemies[i];
                for (int j = i + 1; j < enemies.Count; j++)
                {
                    var second = enemies[j];
                    Vector2 delta = second.Position - first.Position;
                    float squared = delta.sqrMagnitude;
                    if (squared >= minSquared) continue;
                    float distance = Mathf.Sqrt(squared);
                    Vector2 normal = distance > 0.0001f ? delta / distance : ((i + j) % 2 == 0 ? Vector2.right : Vector2.up);
                    Vector2 correction = normal * Mathf.Min((enemySpacing - distance) * 0.5f, 0.15f);
                    first.Position -= correction;
                    second.Position += correction;
                }
            }
        }
        foreach (var enemy in enemies) enemy.Object.transform.position = enemy.Position;
    }

    void SpawnEnemy()
    {
        Vector2 half = new Vector2(view.orthographicSize * view.aspect, view.orthographicSize);
        int side = Random.Range(0, 4);
        float lane = Random.Range(-1f, 1f);
        Vector2 offset = side < 2
            ? new Vector2((half.x + 1.5f) * (side == 0 ? -1 : 1), half.y * lane)
            : new Vector2(half.x * lane, (half.y + 1.5f) * (side == 2 ? -1 : 1));
        float roll = Random.value;
        float specialChance = Mathf.Lerp(0.28f, 0.52f, (elapsed - specialEnemyStart) / 150);
        var kind = EnemyKind.Chaser;
        if (elapsed >= specialEnemyStart && roll < specialChance)
        {
            int shooters = enemies.FindAll(enemy => enemy.Kind == EnemyKind.Shooter).Count;
            kind = roll < specialChance * 0.5f && shooters < 12 ? EnemyKind.Shooter : EnemyKind.Charger;
        }
        var color = kind == EnemyKind.Charger ? ChargerColor : kind == EnemyKind.Shooter ? ShooterColor : ChaserColor;
        var actor = Rent(enemyPool, (Vector2)view.transform.position + offset, 0.8f, color);
        actor.Kind = kind;
        actor.SpawnId = ++spawnSequence;
        ApplyLook(actor.Material, kind == EnemyKind.Charger ? chargerLook : kind == EnemyKind.Shooter ? shooterLook : chaserLook, color);
        ConfigureEnemyAnimation(actor);
        actor.Health = kind == EnemyKind.Charger ? 80 : kind == EnemyKind.Shooter ? 40 : 30;
        actor.RewardTier = enemyRewards ? enemyRewards.RollTier() : 0;
        if (enemyRewards) actor.Health *= enemyRewards.tiers[actor.RewardTier].healthMultiplier;
        actor.Object.name = kind.ToString() + (enemyRewards ? "·" + enemyRewards.tiers[actor.RewardTier].name : "");
        enemies.Add(actor);
    }

    void ConfigureEnemyAnimation(Actor enemy)
    {
        bool animated = enemy.Kind == EnemyKind.Chaser && chaserAnimation && chaserAnimation.idle;
        if (animated && !enemy.Animation)
        {
            enemy.Animation = new GameObject("山贼动画").AddComponent<SpriteRenderer>();
            enemy.Animation.transform.SetParent(enemy.Object.transform, false);
            enemy.Animation.transform.localPosition = new Vector3(0, .12f, -.08f);
        }
        enemy.Object.GetComponent<MeshRenderer>().enabled = !animated;
        if (!enemy.Animation) return;
        enemy.Animation.enabled = animated;
        if (!animated) return;
        enemy.Animation.sprite = chaserAnimation.idle;
        enemy.Animation.flipX = false;
        enemy.Animation.transform.localScale = Vector3.one * chaserAnimation.scale;
        enemy.MoveAnimationTime = enemy.ReleaseRemaining = 0;
    }

    void TickEnemy(Actor enemy, Vector2 toward, float distance, float dt)
    {
        enemy.Timer = Mathf.Max(0, enemy.Timer - dt);
        enemy.ReleaseRemaining = Mathf.Max(0, enemy.ReleaseRemaining - dt);
        Vector2 velocity = Vector2.zero;
        if (enemy.Rush > 0)
        {
            enemy.Rush -= dt;
            velocity = enemy.Direction * 8;
            DetectDodgedAttack(enemy.Position, velocity * dt, 1.15f, dt);
            if (distance < 1.15f) DamagePlayer(18);
        }
        else if (enemy.Aim > 0)
        {
            enemy.Aim -= dt;
            if (enemy.Aim <= 0)
            {
                if (enemy.Kind == EnemyKind.Chaser)
                {
                    if (distance <= meleeAttackRange) DamagePlayer(9);
                    if (enemy.Animation && enemy.Animation.enabled) enemy.ReleaseRemaining = chaserAnimation.releaseSeconds;
                    enemy.Timer = 0.8f;
                    ApplyLook(enemy.Material, chaserLook, ChaserColor);
                }
                else if (enemy.Kind == EnemyKind.Charger)
                {
                    enemy.Rush = 0.38f;
                    enemy.Timer = 1.6f;
                    ApplyLook(enemy.Material, chargerLook, ChargerColor);
                }
                else
                {
                    Fire(enemy.Position, enemy.Direction, true);
                    enemy.Timer = 2.5f;
                    ApplyLook(enemy.Material, shooterLook, ShooterColor);
                }
                enemy.Object.transform.rotation = Quaternion.identity;
                enemy.Object.transform.localScale = Vector3.one * 0.8f;
            }
        }
        else if (enemy.Kind == EnemyKind.Chaser)
        {
            if (distance > 0.66f) velocity = toward * 2;
            if (distance <= meleeAttackRange && enemy.Timer <= 0)
            {
                enemy.Aim = meleeWarningTime;
                enemy.Direction = toward;
                velocity = Vector2.zero;
            }
        }
        else
        {
            bool shooter = enemy.Kind == EnemyKind.Shooter;
            float attackRange = shooter ? 5.8f : 2.4f;
            if (enemy.Timer <= 0 && distance <= attackRange && (!shooter || distance >= 3.5f))
            {
                enemy.Direction = toward;
                enemy.Aim = shooter ? 0.6f : 0.65f;
                enemy.Material.color = Color.white;
                enemy.Object.transform.localScale = new Vector3(1.25f, 0.45f, 1);
                enemy.Object.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(toward.y, toward.x) * Mathf.Rad2Deg);
            }
            else if (distance > attackRange || !shooter && distance > 1.5f)
                velocity = toward * 1.65f;
            else if (shooter && distance < 4.2f)
                velocity = -toward * 1.45f;
        }

        enemy.Position += velocity * dt;
        enemy.Object.transform.position = enemy.Position;
        if (enemy.Animation && enemy.Animation.enabled)
        {
            float facingX = enemy.Aim > 0 || enemy.ReleaseRemaining > 0 ? enemy.Direction.x : velocity.x;
            if (Mathf.Abs(facingX) > .01f) enemy.Animation.flipX = facingX < 0;
            if (enemy.Aim > 0)
                enemy.Animation.sprite = chaserAnimation.AttackFrame(1 - enemy.Aim / meleeWarningTime, false);
            else if (enemy.ReleaseRemaining > 0)
                enemy.Animation.sprite = chaserAnimation.AttackFrame(1 - enemy.ReleaseRemaining / chaserAnimation.releaseSeconds, true);
            else if (velocity.sqrMagnitude > .001f)
            {
                enemy.MoveAnimationTime += dt;
                enemy.Animation.sprite = chaserAnimation.MoveFrame(enemy.MoveAnimationTime);
            }
            else
            {
                enemy.MoveAnimationTime = 0;
                enemy.Animation.sprite = chaserAnimation.idle;
            }
        }
    }

    bool Fire(Vector2 position, Vector2 direction, bool hostile)
    {
        if (direction == Vector2.zero || bullets.Count >= 160) return false;
        int factionCount = 0;
        foreach (var bullet in bullets)
            if (bullet.Hostile == hostile) factionCount++;
        if (factionCount >= 80) return false;
        var shot = Rent(bulletPool, position, hostile ? 0.25f : 0.16f,
            hostile ? new Color(1f, 0.48f, 0.18f) : new Color(0.48f, 1f, 0.88f));
        ApplyLook(shot.Material, hostile ? enemyBulletLook : playerBulletLook, shot.Material.color);
        PlaySound(hostile ? enemyShotSound : shotSound);
        shot.Hostile = hostile;
        Transform container = hostile ? enemyShots : playerShots;
        if (container) shot.Object.transform.SetParent(container);
        shot.Direction = direction;
        shot.Timer = 4;
        shot.Object.name = hostile ? "Enemy projectile" : "Player projectile";
        bullets.Add(shot);
        return true;
    }

    void TickBullet(int index, float dt)
    {
        var shot = bullets[index];
        var step = shot.Direction * (shot.Hostile ? 5 : 10) * dt;
        shot.Timer -= dt;
        if (shot.Timer <= 0 || (shot.Position - body.position).sqrMagnitude > 1600)
        {
            Return(bullets, index, bulletPool);
            return;
        }
        if (shot.Hostile) DetectDodgedAttack(shot.Position, step, 0.55f, dt);
        float parryRadius = BaseGuardPressed ? Mathf.Max(.55f, guardRadius) : .55f;
        if (shot.Hostile && Parrying && SegmentHit(shot.Position, step, body.position, parryRadius, out float contact))
        {
            if (BaseGuardPressed) tapGuardUsed = true;
            PlaySound(parrySound);
            shot.Position += step * contact;
            shot.Direction = -shot.Direction;
            shot.Hostile = false;
            shot.Timer = 4;
            shot.Object.name = "Reflected projectile";
            if (playerShots) shot.Object.transform.SetParent(playerShots);
            ApplyLook(shot.Material, playerBulletLook, new Color(.48f, 1f, .88f));
            shot.Object.transform.position = new Vector3(shot.Position.x, shot.Position.y, -.1f);
            return;
        }
        if (shot.Hostile && SegmentHit(shot.Position, step, body.position, 0.55f, out _))
        {
            DamagePlayer(10);
            Return(bullets, index, bulletPool);
            return;
        }
        if (!shot.Hostile)
        {
            int hit = -1;
            float first = float.PositiveInfinity;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (SegmentHit(shot.Position, step, enemies[i].Position, 0.48f, out float t) && t < first)
                {
                    first = t;
                    hit = i;
                }
            }
            if (hit >= 0)
            {
                ApplyEnemyHit(hit, 20, shot.Direction, DamageSource.Reflected);
                Return(bullets, index, bulletPool);
                return;
            }
        }
        shot.Position += step;
        shot.Object.transform.position = new Vector3(shot.Position.x, shot.Position.y, -0.1f);
    }

    static bool SegmentHit(Vector2 start, Vector2 step, Vector2 center, float radius, out float time)
    {
        Vector2 delta = start - center;
        float outside = delta.sqrMagnitude - radius * radius;
        time = 0;
        if (outside <= 0) return true;
        float lengthSquared = step.sqrMagnitude;
        if (lengthSquared <= 0) return false;
        float projection = Vector2.Dot(delta, step);
        float discriminant = projection * projection - lengthSquared * outside;
        if (discriminant < 0) return false;
        time = (-projection - Mathf.Sqrt(discriminant)) / lengthSquared;
        return time >= 0 && time <= 1;
    }

    void HitEnemy(int index, int damage, Vector2 knockback) => ApplyEnemyHit(index, damage, knockback, DamageSource.Skill);

    void DamagePlayer(int damage)
    {
        if (BaseGuardPressed)
        {
            if (cast == Cast.Pending) tapGuardUsed = true;
            return;
        }
        if (hp <= 0 || Dodging || PerfectCounter) return;
        if (Parrying)
        {
            if (elapsed < nextHit) return;
            nextHit = elapsed + 0.3f;
            PlaySound(parrySound);
            counterPending = true;
            return;
        }
        if (elapsed < nextHit) return;
        if (Dashing && dashUntil - Time.fixedTime > 0.12f) return;
        if (HoldingGuardSlash)
        {
            damage = Mathf.CeilToInt(damage * guardSlashDamageMultiplier);
            if (guardRemaining > 0 && elapsed >= nextGuardStrike)
            {
                guardRemaining--;
                guardStrikeQueued = true;
                nextGuardStrike = elapsed + 0.35f;
            }
        }
        else if (cast == Cast.Holding && (skill == Skill.Guard || castingFusion && fusion == Fusion.GuardDash))
        {
            guardCharge = Mathf.Min(guardCharge + 1, 4);
            damage = Mathf.CeilToInt(damage * guardDamageMultiplier);
        }
        PlaySound(hurtSound);
        if (runStats) { runStats.Receive(damage); hp = Mathf.CeilToInt(runStats.HP); }
        else hp = Mathf.Max(0, hp - damage);
        nextHit = elapsed + 0.3f;
        if (hp == 0)
        {
            ClearInput();
            InterruptSkill();
        }
    }

    void ResetCombat()
    {
        while (enemies.Count > 0) Return(enemies, enemies.Count - 1, enemyPool);
        while (bullets.Count > 0) Return(bullets, bullets.Count - 1, bulletPool);
        counterPending = false;
        perfectNoticeUntil = 0;
        kills = 0;
        nextSpawn = nextShot = 0;
    }
}
