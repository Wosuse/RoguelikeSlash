using System.Collections.Generic;
using UnityEngine;

// Compatibility boundary only: new M6 state and rules live on dedicated child components.
public sealed partial class ArenaPrototype
{
    [Header("M6 绯荤粺寮曠敤")]
    [SerializeField] RunStats runStats;
    [SerializeField] RunClock runClock;
    [SerializeField] DamageResolver damageResolver;
    [SerializeField] EnemyRewards enemyRewards;
    [SerializeField] RunProgression progression;
    [SerializeField] WeaponInventory inventory;
    public Vector2 PlayerPosition => body ? body.position : Vector2.zero;
    public bool Alive => hp > 0;
    public V06Skills v06Skills;
    public V06Attack v06Attack;
    public bool HasMovementInput=>move.sqrMagnitude>.01f;
    public Vector2 VisualFacing=>visualFacingSign<0?Vector2.left:Vector2.right;
    public void SetVisualFacing(float horizontal){if(Mathf.Abs(horizontal)>.01f)visualFacingSign=Mathf.Sign(horizontal);}
    public Vector2 DodgeDirection=>HasMovementInput?move.normalized:-VisualFacing;
    public Vector2 V06Facing => move.sqrMagnitude>.01f?move.normalized:facing;
    public void V06Slash(float radius,float angle,float damage){for(int i=enemies.Count-1;i>=0;i--){var delta=enemies[i].Position-PlayerPosition;if(delta.magnitude<=radius && Vector2.Angle(V06Facing,delta)<=angle/2)ApplyEnemyHit(i,damage,Vector2.zero,DamageSource.Skill);}}
    public void V09FinalHit(int id,float amount,DamageSource source){for(int i=enemies.Count-1;i>=0;i--)if(enemies[i].SpawnId==id){ApplyEnemyHit(i,amount,Vector2.zero,source,false,true);return;}}
    public void V09Slash(float radius,float angle,float damage,Vector2 direction,bool guaranteed=false){float attack=runStats.Value(RunStat.Attack)/100;float chance=v06Skills.SkillCritChance,critical=v06Skills.SkillCritDamage;for(int i=enemies.Count-1;i>=0;i--){var delta=enemies[i].Position-PlayerPosition;if(delta.magnitude<=radius&&Vector2.Angle(direction,delta)<=angle/2){if(v06Attack&&v06Attack.elements)v06Attack.elements.SkillPoison(enemies[i].SpawnId,damage*attack);ApplyEnemyHit(i,damage*attack*(guaranteed?(critical+50)/100:Random.value*100<chance?critical/100:1),Vector2.zero,DamageSource.Skill,false,true);}}}
    public bool V09AttackAt(Vector2 position){for(int i=bullets.Count-1;i>=0;i--)if(bullets[i].Hostile&&Vector2.Distance(bullets[i].Position,position)<.7f){Return(bullets,i,bulletPool);return true;}foreach(var enemy in enemies)if(IsMeleeWarning(enemy)&&enemy.Aim<.06f&&Vector2.Distance(enemy.Position,position)<=meleeAttackRange)return true;return false;}
 public bool WarnedAttack()=>InWarnedAttackRange();
 public void V09Push(int id,Vector2 direction,float distance){foreach(var e in enemies)if(e.SpawnId==id){e.Position+=direction.normalized*Mathf.Max(0,distance);e.Object.transform.position=e.Position;return;}}
 public void V09ClearBullets(Vector2 origin,Vector2 direction,float from,float to,bool ring,float halfWidth=.5f){for(int i=bullets.Count-1;i>=0;i--){if(!bullets[i].Hostile)continue;Vector2 delta=bullets[i].Position-origin;bool hit=ring?delta.magnitude>=from-.5f&&delta.magnitude<=to+.5f:Vector2.Distance(bullets[i].Position,origin+direction*Mathf.Clamp(Vector2.Dot(delta,direction),from,to))<halfWidth;if(hit)Return(bullets,i,bulletPool);}}
 public bool SkillBusy => v06Skills && v06Skills.Busy;
    public bool CanAutoAttack => Alive && !SkillBusy && !(runClock && runClock.Paused);
    public float WorldRate => ClockRate;
    public struct Target { public int id; public Vector2 position; public float health; }
    public void ReadTargets(List<Target> targets)
    {
        targets.Clear(); foreach(var enemy in enemies)targets.Add(new Target {id=enemy.SpawnId,position=enemy.Position,health=enemy.Health});
    }
    public bool TargetPosition(int id,out Vector2 position)
    {
        foreach(var enemy in enemies) if(enemy.SpawnId==id){position=enemy.Position;return true;}
        position=Vector2.zero;return false;
    }
    public void WeaponHit(int id,float baseDamage,DamageSource source,bool crit=true)
    {
        for(int i=enemies.Count-1;i>=0;i--) if(enemies[i].SpawnId==id){ApplyEnemyHit(i,baseDamage,Vector2.zero,source,crit);return;}
    }
    void ApplyEnemyHit(int index,float baseDamage,Vector2 knockback,DamageSource source,bool crit=true,bool resolved=false)
    {
        var enemy=enemies[index]; if(v06Attack&&v06Attack.elements)baseDamage*=v06Attack.elements.Vulnerability(enemy.SpawnId);float damage=resolved?baseDamage:v06Attack&&(source==DamageSource.Weapon||source==DamageSource.Bleed)?v06Attack.ResolveNormal(baseDamage,crit):damageResolver?damageResolver.Resolve(baseDamage,crit):baseDamage;
        float actual=Mathf.Min(enemy.Health,damage); enemy.Health-=damage;
        if(damageResolver)damageResolver.Applied(actual,source);if(source==DamageSource.Skill&&v06Attack&&v06Attack.build)v06Attack.build.SkillHeal(actual);
        if(enemy.Health<=0){kills++;if(!labMode&&enemyRewards)enemyRewards.DropRewards(enemy.Position,enemy.RewardTier);Return(enemies,index,enemyPool);return;}
        enemy.Position+=knockback*.15f;enemy.Object.transform.position=enemy.Position;
    }
    void ResetRunSystems()
    {
        if(v06Skills)v06Skills.ResetRun();if(v06Attack)v06Attack.ResetRun();
        if(runClock)runClock.ResetRun(); if(runStats)runStats.ResetRun(); if(inventory)inventory.ResetRun();
        if(progression)progression.ResetRun();if(enemyRewards)enemyRewards.ResetRun();
    }
    public void DebugRestart() => Retry();
    public void DebugSpawnEnemy() => SpawnEnemy();
    public void DebugClearCooldowns(){if(v06Skills)v06Skills.DebugClearCooldowns();}
}
