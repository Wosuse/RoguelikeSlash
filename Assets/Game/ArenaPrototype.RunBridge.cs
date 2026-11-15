using System.Collections.Generic;
using UnityEngine;

// Compatibility boundary only: new M6 state and rules live on dedicated child components.
public sealed partial class ArenaPrototype
{
    [Header("M6 系统引用")]
    [SerializeField] RunStats runStats;
    [SerializeField] RunClock runClock;
    [SerializeField] DamageResolver damageResolver;
    [SerializeField] EnemyRewards enemyRewards;
    [SerializeField] RunProgression progression;
    [SerializeField] WeaponInventory inventory;
    [SerializeField] AutoWeaponController autoWeapons;
    public Vector2 PlayerPosition => body ? body.position : Vector2.zero;
    public bool Alive => hp > 0;
    public bool SkillBusy => cast != Cast.Idle || guardWaveActive || waitingForArcs || arcCount > 0;
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
    void ApplyEnemyHit(int index,float baseDamage,Vector2 knockback,DamageSource source,bool crit=true)
    {
        var enemy=enemies[index]; float damage=damageResolver?damageResolver.Resolve(baseDamage,crit):baseDamage;
        float actual=Mathf.Min(enemy.Health,damage); enemy.Health-=damage;
        if(damageResolver)damageResolver.Applied(actual,source);
        if(enemy.Health<=0){kills++;if(enemyRewards)enemyRewards.DropRewards(enemy.Position,enemy.RewardTier);Return(enemies,index,enemyPool);return;}
        enemy.Position+=knockback*.15f;enemy.Object.transform.position=enemy.Position;
    }
    void ResetRunSystems()
    {
        if(runClock)runClock.ResetRun(); if(runStats)runStats.ResetRun(); if(inventory)inventory.ResetRun();
        if(progression)progression.ResetRun();if(enemyRewards)enemyRewards.ResetRun();if(autoWeapons)autoWeapons.ResetRun();
    }
    public void DebugRestart() => Retry();
    public void DebugSpawnEnemy() => SpawnEnemy();
    public void DebugClearCooldowns(){System.Array.Clear(readyAt,0,3);System.Array.Clear(lastCombinationReadyAt,0,3);}
}
