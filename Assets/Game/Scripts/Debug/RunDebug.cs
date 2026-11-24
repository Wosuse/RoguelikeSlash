using UnityEngine;

public sealed class RunDebug : MonoBehaviour
{
    public ArenaPrototype arena;
    public RunStats stats;
    public RunProgression progression;
    public WeaponInventory inventory;
    public RunClock clock;
    [Header("调试输入（仅运行模式生效）")]
    [Min(1), InspectorName("增加经验")] public int xp = 40;
    [Min(1), InspectorName("增加金币")] public int coins = 20;
    [InspectorName("测试属性")] public RunStat stat;
    [Min(0), InspectorName("属性增量")] public float amount = 15;
    [InspectorName("测试武器")] public WeaponKind weapon;
    [ContextMenu("调试/增加经验")] public void AddXP(){if(Application.isPlaying)progression.AddXP(xp);}
    [ContextMenu("调试/增加金币")] public void AddCoins(){if(Application.isPlaying)progression.AddCoins(coins);}
    [ContextMenu("调试/增加属性")] public void AddStat(){if(Application.isPlaying)stats.Add(stat,amount);}
    [ContextMenu("调试/获取测试武器")] public void Acquire(){if(Application.isPlaying)inventory.Acquire(weapon);}
    [ContextMenu("调试/熟练加一点")] public void Mastery(){if(Application.isPlaying)inventory.AddMastery();}
    [ContextMenu("调试/生成敌人")] public void Spawn(){if(Application.isPlaying)arena.DebugSpawnEnemy();}
    [ContextMenu("调试/清空技能冷却")] public void Cooldowns(){if(Application.isPlaying)arena.DebugClearCooldowns();}
    [ContextMenu("调试/重新开始")] public void Restart(){if(Application.isPlaying)arena.DebugRestart();}
}
