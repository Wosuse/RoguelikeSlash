using UnityEngine;

public enum DamageSource { Skill, Reflected, Weapon, Spin, Bleed }
public sealed class DamageResolver : MonoBehaviour
{
    [InspectorName("玩家属性")] public RunStats stats;
    [SerializeField, InspectorName("最近实际伤害")] float lastDamage;
    [SerializeField, InspectorName("最近暴击")] bool lastCritical;
    public float Resolve(float baseDamage, bool canCrit)
    {
        lastCritical = canCrit && Random.value * 100 < stats.Value(RunStat.CritChance);
        return Mathf.Max(0, baseDamage) * stats.Value(RunStat.Attack) / 100 *
            (lastCritical ? stats.Value(RunStat.CritDamage) / 100 : 1);
    }
    public void Applied(float actualHealthLost, DamageSource source)
    {
        lastDamage = actualHealthLost;
        if (source == DamageSource.Weapon || source == DamageSource.Spin || source == DamageSource.Bleed)
            stats.Heal(actualHealthLost * stats.Value(RunStat.LifeSteal) / 100);
    }
}
