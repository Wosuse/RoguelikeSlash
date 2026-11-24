using UnityEngine;

[CreateAssetMenu(menuName="割草/锻体碎片")]
public sealed class StatShardDefinition : ScriptableObject
{
    public static readonly string[] Names = { "攻击力", "防御力", "最大生命", "暴击率 %", "技能急速", "攻击速度 %", "暴击伤害 %", "生命偷取 %", "移速加成 %", "生命回复/秒", "护盾击破 %", "护盾生成", "护盾容量" };
    [InspectorName("属性")] public RunStat stat;
    [InspectorName("品质")] public ShardQuality quality;
    [Min(0), InspectorName("每片增量")] public float amount;
    [InspectorName("图标")] public Sprite icon;
    [InspectorName("获取音效")] public AudioClip sound;
    public string Label => $"{(quality == ShardQuality.Silver ? "银色" : quality == ShardQuality.Gold ? "金色" : "彩色")} · {Names[(int)stat]} +{amount:0.##}";
    public bool Available(RunStats stats)
    {
        if ((int)stat < 6 ? quality == ShardQuality.Colored : quality != ShardQuality.Colored) return false;
        if (stat == RunStat.ShieldBreak) return false; // No shielded enemies in P4.
        if (stat == RunStat.ShieldCapacity && stats.Generators == 0) return false;
        if (stat == RunStat.CritChance || stat == RunStat.LifeSteal) return stats.Value(stat) < 100;
        if (stat == RunStat.Haste) return stats.Value(stat) < stats.hasteCap;
        if (stat == RunStat.AttackSpeed) return stats.Value(stat) < stats.attackSpeedCap;
        return true;
    }
}
