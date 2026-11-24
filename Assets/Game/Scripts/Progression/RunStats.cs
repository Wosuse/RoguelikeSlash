using System;
using UnityEngine;

public enum RunStat { Attack, Defense, MaxHP, CritChance, Haste, AttackSpeed, CritDamage, LifeSteal, MoveSpeed, Regen, ShieldBreak, ShieldGeneration, ShieldCapacity }
public enum ShardQuality { Silver, Gold, Colored }

[DisallowMultipleComponent]
public sealed class RunStats : MonoBehaviour
{
    public V09Build build;
    [Header("基础属性（百分比用百分数填写）")]
    [Min(0), InspectorName("攻击力")] public float attack = 100;
    [Min(0), InspectorName("防御力")] public float defense;
    [Min(1), InspectorName("最大生命")] public float maxHP = 100;
    [Range(0,100), InspectorName("暴击率 %")] public float critChance = 5;
    [Min(0), InspectorName("技能急速")] public float haste;
    [Min(0), InspectorName("攻速加成 %")] public float attackSpeed;
    [Min(100), InspectorName("暴击总伤害 %")] public float critDamage = 150;
    [Range(0,100), InspectorName("生命偷取 %")] public float lifeSteal;
    [InspectorName("基础移动配置")] public ArenaMovementTuning movement;
    [HideInInspector] public float moveSpeed = 5;
    [Min(0), InspectorName("每秒回血")] public float regen;
    [Header("护盾")]
    [Min(0), InspectorName("初始容量")] public float baseShield = 5;
    [Min(.1f), InspectorName("基础生成间隔")] public float shieldInterval = 15;
    [Header("有效属性上限")]
    [Min(0), InspectorName("急速上限")] public float hasteCap = 300;
    [Min(0), InspectorName("攻速加成上限 %")] public float attackSpeedCap = 300;
    [Header("运行状态（只读观察）")]
    [SerializeField] float health, shield, shieldCountdown;
    [SerializeField] float[] modifiers = new float[13];
    [SerializeField] int[] shardCounts = new int[13];
    public event Action Changed;
    public float HP => health;
    public float Shield => shield;
    public float ShieldCountdown => shieldCountdown;
    public int Generators => shardCounts[(int)RunStat.ShieldGeneration];
    public float Capacity => Generators > 0 ? baseShield + modifiers[12] : 0;
    public float GenerationInterval => shieldInterval / (1 + .1f * Mathf.Max(0, Generators - 1));
    public float RawAttack=>Mathf.Max(0,attack+modifiers[(int)RunStat.Attack]+(build?build.AttackBonus:0));
    public float Value(RunStat stat)
    {
        float m = modifiers[(int)stat];
        switch (stat)
        {
            case RunStat.Attack: return build&&build.Has(V06CardKind.嗡嗡攻势)?attack:RawAttack;
            case RunStat.Defense: return Mathf.Max(0, defense + m);
            case RunStat.MaxHP: return Mathf.Max(1, (maxHP + m)*(build&&build.Has(V06CardKind.血狂)?.75f:1));
            case RunStat.CritChance: return Mathf.Clamp(critChance + m, 0, 100);
            case RunStat.Haste: return Mathf.Clamp(haste + m, 0, hasteCap);
            case RunStat.AttackSpeed: return Mathf.Clamp(attackSpeed + m, 0, attackSpeedCap);
            case RunStat.CritDamage: return Mathf.Max(100, critDamage + m + (build?build.CritBonus:0));
            case RunStat.LifeSteal: return Mathf.Clamp(lifeSteal + m, 0, 100);
            case RunStat.MoveSpeed: return (movement ? movement.WorldSpeed : moveSpeed) * (1 + m / 100);
            case RunStat.Regen: return regen + m;
            case RunStat.ShieldGeneration: return Generators;
            case RunStat.ShieldCapacity: return Capacity;
            default: return m;
        }
    }
    public int Count(RunStat stat) => shardCounts[(int)stat];
    public float Cooldown(float original) => original / (1 + Value(RunStat.Haste) / 100);
    public void ResetRun()
    {
        Array.Clear(modifiers, 0, modifiers.Length); Array.Clear(shardCounts, 0, shardCounts.Length);
        health = Value(RunStat.MaxHP); shield = 0; shieldCountdown = shieldInterval; Changed?.Invoke();
    }
    public void Add(RunStat stat, float amount)
    {
        modifiers[(int)stat] += amount; shardCounts[(int)stat]++;
        if (stat == RunStat.MaxHP) Heal(amount);
        if (stat == RunStat.ShieldGeneration && Generators == 1) shieldCountdown = GenerationInterval;
        Changed?.Invoke();
    }
    public void Tick(float dt)
    {
        if (health <= 0 || dt <= 0) return;
        Heal(Value(RunStat.Regen) * dt);
        if (Generators == 0) return;
        shieldCountdown -= dt;
        if (shieldCountdown <= .00001f) { shield = Capacity; shieldCountdown = Mathf.Repeat(shieldCountdown, GenerationInterval); if(shieldCountdown <= .00001f) shieldCountdown = GenerationInterval; }
    }
    public void Heal(float amount) { if (health > 0) health = Mathf.Min(Value(RunStat.MaxHP), health + Mathf.Max(0, amount)); }
    public void ClampHealth()=>health=Mathf.Min(health,Value(RunStat.MaxHP));
    public void Receive(float damage)
    {
        if(build&&build.Immune)return;
        damage = Mathf.Max(0, damage) * 100 / (100 + Value(RunStat.Defense));
        float absorbed = Mathf.Min(shield, damage); shield -= absorbed;
        float healthDamage = Mathf.Min(health, damage - absorbed);
        if (healthDamage > 0 && build) build.OnHealthDamaged();
        health = Mathf.Max(0, health - healthDamage);if(health<=0&&build&&build.SaveFromDeath())health=1;
    }
    public float DisplayValue(RunStat stat)
    {
        float value=Value(stat);
        if(stat==RunStat.MoveSpeed) return value / ArenaMovementTuning.UnitsPerSpeedPoint * (build && build.attack ? build.attack.MoveMultiplier : 1);
        if(stat==RunStat.AttackSpeed && build) return value + build.DanceAttackSpeedBonus;
        return value;
    }
    public string Summary()
    {
        string text = $"生命 {HP:0.#}/{Value(RunStat.MaxHP):0.#}  护盾 {Shield:0.#}/{Capacity:0.#}\n";
        for (int i=0;i<13;i++) {
            float value = DisplayValue((RunStat)i);
            text += $"{(i == (int)RunStat.MoveSpeed ? "移动速度" : StatShardDefinition.Names[i])}：{value:0.##}  （碎片 {shardCounts[i]}）\n";
        }
        return text + (Generators > 0 ? $"护盾生成 {shieldCountdown:0.0}/{GenerationInterval:0.0}s" : "护盾生成：未解锁");
    }
}
