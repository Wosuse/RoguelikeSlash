using System;
using System.Collections.Generic;
using UnityEngine;

public enum RewardStage { None = 0, Choices = 2, Shards = 3 }
public sealed class RunProgression : MonoBehaviour
{
    [Header("系统引用")]
    public RunStats stats;
    public WeaponInventory inventory;
    public RunClock clock;
    [InspectorName("金币奖励配置")] public EnemyRewards enemyRewards;
    [InspectorName("能力卡池")] public AbilityCardDefinition[] cards;
    [InspectorName("碎片池")] public StatShardDefinition[] shards;
    [Header("升级与经济")]
    [Min(2), InspectorName("三片奖励起始等级")] public int threeShardLevel = 6;
    [Min(1), InspectorName("经验曲线倍率")] public float xpScale = 1;
    [Min(1), InspectorName("每片价格对应普通怪数量")] public int priceInKills = 20;
    [Header("品质权重（无保底）")]
    [Min(0), InspectorName("银色")] public float silverWeight = 70;
    [Min(0), InspectorName("金色")] public float goldWeight = 25;
    [Min(0), InspectorName("彩色")] public float coloredWeight = 5;
    [Header("运行状态")]
    [SerializeField] int level = 1, xp, coins, pendingRewards, rewardLevel, shardIndex, shardTotal;
    [SerializeField] RewardStage stage;
    readonly List<AbilityCardDefinition> offers = new List<AbilityCardDefinition>();
    readonly Queue<int> rewardLevels = new Queue<int>();
    StatShardDefinition currentShard;
    ShardQuality[] batchQualities;
    readonly Dictionary<AbilityCardDefinition, int> ownedAbilities = new Dictionary<AbilityCardDefinition, int>();
    public IReadOnlyDictionary<AbilityCardDefinition, int> OwnedAbilities => ownedAbilities;
    public event Action Changed;
    public int Level => level;
    public int XP => xp;
    public int Coins => coins;
    public int ShardPrice => (enemyRewards ? enemyRewards.tiers[0].coins : 1) * priceInKills;
    public int NeededXP => ExperienceFor(level, xpScale);
    public RewardStage Stage => stage;
    public IReadOnlyList<AbilityCardDefinition> Offers => offers;
    public StatShardDefinition CurrentShard => currentShard;
    public int ShardIndex => shardIndex;
    public int ShardTotal => shardTotal;
    public int CurrentRewardShardCount => ShardCountFor(rewardLevel);
    public static int ExperienceFor(int level, float scale = 1) => Mathf.CeilToInt((40 + 16 * (level-1) + 5 * Mathf.Pow(level-1,1.7f)) * scale);
    public int ShardCountFor(int earnedLevel) => earnedLevel >= threeShardLevel ? 3 : 2;
    public void ResetRun()
    {
        ownedAbilities.Clear();
        level = 1; xp = coins = pendingRewards = shardIndex = shardTotal = 0;
        stage = RewardStage.None; offers.Clear(); rewardLevels.Clear(); currentShard = null;
        clock.SetPause("升级", false); Changed?.Invoke();
    }
    public void AddCoins(int amount) { coins += Mathf.Max(0, amount); Changed?.Invoke(); }
    public bool SpendCoins(int amount) { if (amount < 0 || coins < amount) return false; coins -= amount; Changed?.Invoke(); return true; }
    public void AddXP(int amount)
    {
        xp += Mathf.Max(0, amount);
        while (xp >= NeededXP) { xp -= NeededXP; level++; rewardLevels.Enqueue(level); pendingRewards++; }
        if (stage == RewardStage.None && pendingRewards > 0) BeginReward();
        Changed?.Invoke();
    }
    void BeginReward()
    {
        rewardLevel = rewardLevels.Dequeue();
        stage = RewardStage.Choices;
        clock.SetPause("升级", true);
        offers.Clear();
        var pool = new List<AbilityCardDefinition>();
        foreach (var card in cards) if (card && card.Available(inventory) && !pool.Contains(card)) pool.Add(card);
        while (offers.Count < 3 && pool.Count > 0) { int i = UnityEngine.Random.Range(0,pool.Count); offers.Add(pool[i]); pool.RemoveAt(i); }
    }
    public void SelectCard(int index)
    {
        if (stage != RewardStage.Choices || index < 0 || index >= offers.Count) return;
        var card = offers[index]; stage = RewardStage.None; card.Grant(inventory);
        if (card.kind == AbilityCardKind.Projectile || card.kind == AbilityCardKind.Direction)
        {
            ownedAbilities.TryGetValue(card, out int count);
            ownedAbilities[card] = count + 1;
        }
        CompleteReward();
    }
    public void ChooseShards()
    {
        if (stage != RewardStage.Choices) return;
        shardTotal = ShardCountFor(rewardLevel); shardIndex = 0;
        batchQualities = new ShardQuality[shardTotal];
        for (int i=0;i<shardTotal;i++) batchQualities[i] = RollQuality();
        stage = RewardStage.Shards; currentShard = RollShard(batchQualities[0]); Changed?.Invoke();
    }
    public void ClaimShard(int expectedIndex)
    {
        if (stage != RewardStage.Shards || !currentShard || expectedIndex != shardIndex) return;
        var grant = currentShard; currentShard = null; stats.Add(grant.stat, grant.amount); shardIndex++;
        if (shardIndex >= shardTotal) { CompleteReward(); return; }
        currentShard = RollShard(batchQualities[shardIndex]); Changed?.Invoke();
    }
    public ShardQuality RollQuality()
    {
        float total = Mathf.Max(.001f, silverWeight + goldWeight + coloredWeight);
        float r = UnityEngine.Random.value * total;
        return r < silverWeight ? ShardQuality.Silver : r < silverWeight + goldWeight ? ShardQuality.Gold : ShardQuality.Colored;
    }
    public StatShardDefinition RollShard(ShardQuality quality)
    {
        var pool = new List<StatShardDefinition>();
        foreach (var shard in shards) if (shard && shard.quality == quality && shard.Available(stats)) pool.Add(shard);
        if (pool.Count == 0) foreach (var shard in shards) if (shard && shard.Available(stats)) pool.Add(shard);
        return pool.Count == 0 ? null : pool[UnityEngine.Random.Range(0,pool.Count)];
    }
    void CompleteReward()
    {
        offers.Clear(); currentShard = null; pendingRewards--; stage = RewardStage.None;
        if (pendingRewards > 0) BeginReward(); else clock.SetPause("升级",false);
        Changed?.Invoke();
    }
}

