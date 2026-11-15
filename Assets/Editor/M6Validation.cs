using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Integration regression suite. Runs against an isolated play scene; leaves no test modifiers behind.</summary>
[InitializeOnLoad]
public static class M6Validation
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static int checks;
    static readonly List<string> results=new List<string>();
    static M6Validation(){EditorApplication.playModeStateChanged+=OnState;}
    public static void RunBatch()
    {
        EditorSceneManager.OpenScene("Assets/Game/Scenes/GameScene.unity");
        M6SceneSetup.Install();SessionState.SetBool("M6Batch",true);EditorApplication.isPlaying=true;
    }
    static void OnState(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("M6Batch",false))return;
        EditorApplication.delayCall+=()=>{
            try {Run();SessionState.SetBool("M6Batch",false);EditorApplication.Exit(0);}
            catch(Exception e){File.WriteAllText("M6-validation.txt","FAILED\n"+e);Debug.LogException(e);EditorApplication.Exit(1);}
        };
    }
    static object Get(object obj,string field)=>obj.GetType().GetField(field,Flags).GetValue(obj);
    static void Set(object obj,string field,object value)=>obj.GetType().GetField(field,Flags).SetValue(obj,value);
    static object Call(object obj,string method,params object[] args)=>obj.GetType().GetMethod(method,Flags).Invoke(obj,args);
    static void Check(bool condition,string label){if(!condition)throw new Exception(label);checks++;results.Add("PASS "+label);}
    static void Near(float value,float expected,string label)=>Check(Mathf.Abs(value-expected)<.002f,label+" = "+value);
    [MenuItem("Tools/Arena/M6/运行集成验证（运行模式）")]
    public static void Run()
    {
        if(!Application.isPlaying)throw new Exception("请进入运行模式，测试会重置本局");
        checks=0;results.Clear();
        var arena=UnityEngine.Object.FindObjectOfType<ArenaPrototype>();
        var stats=UnityEngine.Object.FindObjectOfType<RunStats>();var inventory=UnityEngine.Object.FindObjectOfType<WeaponInventory>();
        var progression=UnityEngine.Object.FindObjectOfType<RunProgression>();var clock=UnityEngine.Object.FindObjectOfType<RunClock>();
        var weapons=UnityEngine.Object.FindObjectOfType<AutoWeaponController>();var bleed=UnityEngine.Object.FindObjectOfType<BleedStatusController>();
        var drops=UnityEngine.Object.FindObjectOfType<EnemyRewards>();
        arena.DebugRestart();
        var characterHud = UnityEngine.Object.FindObjectOfType<RunHud>();
        characterHud.inventoryButton.onClick.Invoke();
        var character = characterHud.GetComponentInChildren<CharacterPanel>(true);
        Check(character && character.gameObject.activeInHierarchy, "角色按钮打开新面板");
        Check(character.GetComponentsInChildren<UnityEngine.UI.ScrollRect>().Length == 4, "属性滚动与三个内容栏");
        Call(character, "WeaponMenu", inventory.Current);
        Check(character.transform.Find("详情浮窗") != null, "武器操作浮窗");
        Call(character, "WeaponDetail", inventory.Current);
        characterHud.closeButton.onClick.Invoke();
        Check(!clock.Paused, "角色面板关闭恢复战斗");
        Near(stats.Cooldown(2.5f),2.5f,"A 点按冷却保留");stats.Add(RunStat.Haste,100);
        Near(stats.Cooldown(8),4,"基础急速");Near(stats.Cooldown(12),6,"融合急速");Near(stats.Cooldown(2.5f),1.25f,"点按急速");
        stats.ResetRun();stats.Add(RunStat.ShieldGeneration,1);Near(stats.Capacity,5,"护盾基础容量");
        stats.Tick(14.9f);Near(stats.Shield,0,"首片不立即生成");stats.Tick(.1f);Near(stats.Shield,5,"15秒生成");
        stats.Receive(7);Near(stats.Shield,0,"护盾先扣除");Near(stats.HP,98,"剩余伤害扣血");
        stats.Add(RunStat.ShieldCapacity,10);Near(stats.Capacity,15,"容量碎片");
        stats.Add(RunStat.ShieldGeneration,1);Near(stats.GenerationInterval,15/1.1f,"生成效率速率加法");
        stats.Add(RunStat.CritDamage,15);Near(stats.Value(RunStat.CritDamage),165,"暴伤百分点");
        stats.Add(RunStat.MaxHP,20);Near(stats.HP,118,"最大生命成长回复增量");
        stats.ResetRun();stats.Add(RunStat.Defense,100);stats.Receive(20);Near(stats.HP,90,"防御公式");
        stats.Add(RunStat.Regen,.1f);stats.Tick(10);Near(stats.HP,91,"小数回复累计");
        inventory.ResetRun();inventory.Acquire(WeaponKind.Knife);Check(inventory.Mastery==0&&inventory.Owns(WeaponKind.Knife),"首次武器进背包");
        inventory.Acquire(WeaponKind.Knife);Check(inventory.Level==1,"重复武器熟练");inventory.AddMastery(3);
        inventory.Equip(WeaponKind.Knife);Check(inventory.Level==2&&inventory.Mastery==4,"换装保留未完成进度");
        inventory.AddMastery(99);Check(inventory.Level==3&&inventory.Mastery==6,"熟练封顶");
        foreach(var card in progression.cards)
        {
            if(card.kind==AbilityCardKind.Mastery)Check(!card.Available(inventory),"满级过滤熟练卡");
            if(card.kind==AbilityCardKind.Weapon&&card.weapon==WeaponKind.Knife)Check(!card.Available(inventory),"满级过滤重复武器");
            if(card.kind==AbilityCardKind.Weapon&&card.weapon==WeaponKind.Bow)Check(card.Available(inventory),"满级仍能获取新武器");
        }
        stats.ResetRun();
        foreach(var shard in progression.shards)
        {
            if(shard.stat==RunStat.ShieldCapacity){Check(!shard.Available(stats),"护盾容量前置过滤");stats.Add(RunStat.ShieldGeneration,1);Check(shard.Available(stats),"解锁后容量入池");}
            if(shard.stat==RunStat.ShieldBreak)Check(!shard.Available(stats),"无带盾敌人时过滤击破");
        }
        progression.ResetRun();inventory.ResetRun();progression.AddXP(101);
        Check(progression.Level==3&&progression.XP==0,"溢出经验连续升级");
        var rewardHud = UnityEngine.Object.FindObjectOfType<RunHud>();
        Check(progression.Stage == RewardStage.Choices && progression.Offers.Count == 3 &&
            rewardHud.optionButtons[3].gameObject.activeInHierarchy && rewardHud.optionLabels[3].text == "随机成长碎片 ×2", "奖励直接四选一");
        progression.ChooseShards();Check(progression.ShardTotal==2,"低等级两片");
        progression.ClaimShard(0);progression.ClaimShard(0);Check(progression.ShardIndex==1,"重复领取索引幂等");progression.ClaimShard(1);
        Check(progression.Stage==RewardStage.Choices&&clock.Paused,"第二次奖励排队保持暂停");
        progression.SelectCard(0);Check(progression.Stage==RewardStage.None&&!clock.Paused,"全部奖励结束恢复");
        Check(progression.ShardCountFor(5)==2&&progression.ShardCountFor(6)==3,"奖励等级阈值");
        progression.AddCoins(20);Check(progression.SpendCoins(20)&&!progression.SpendCoins(20),"钱包余额防重复扣费");
        clock.SetPause("升级测试",true);clock.SetPause("面板测试",true);clock.SetPause("升级测试",false);
        Check(clock.Paused&&Time.timeScale==0,"暂停原因独立");clock.SetPause("面板测试",false);Near(Time.timeScale,1,"暂停恢复");
        Near(BleedStatusController.CycleCoefficient(3,0),1.5f,"飞刀基础满层");Near(BleedStatusController.CycleCoefficient(5,1),2.5f,"飞刀升级满层");
        Near(AutoWeaponController.BowRepeatMultiplier(1),.65f,"弓箭第二击");Near(AutoWeaponController.BowRepeatMultiplier(6),.1f,"弓箭衰减下限");
        Near(AutoWeaponController.ProjectileFactor(2),.6f,"第三弹丸系数");
        arena.DebugRestart();stats.critChance=0;stats.maxHP=10000;stats.ResetRun();
        var enemy=CreateEnemy(arena,new Vector2(2,0),10000);int id=(int)Get(enemy,"SpawnId");
        var targets=new List<ArenaPrototype.Target>();arena.ReadTargets(targets);
        var castField=arena.GetType().GetField("cast",Flags);var idle=Enum.Parse(castField.FieldType,"Idle");
        var skillField = arena.GetType().GetField("skill", Flags);
        skillField.SetValue(arena, Enum.Parse(skillField.FieldType, "Guard"));
        castField.SetValue(arena, Enum.Parse(castField.FieldType, "Pending"));
        Call(arena, "Fire", new Vector2(1.2f, 0), Vector2.left, true);
        Call(arena, "TickBullet", 0, .06f);
        var reflected = ((IList)Get(arena, "bullets"))[0];
        Check(!(bool)Get(reflected, "Hostile") && (Vector2)Get(reflected, "Direction") == Vector2.right,
            "点按格挡在护圈边缘反弹敌弹");
        Call(arena, "TickBullet", 0, .15f);
        Near((float)Get(enemy, "Health"), 9980, "反弹弹丸命中敌人并造成伤害");
        Set(enemy, "Health", 10000f);
        foreach(string phase in new[]{"Pending","Holding","Active"})
        {castField.SetValue(arena,Enum.Parse(castField.FieldType,phase));Check(!arena.CanAutoAttack,"技能 "+phase+" 禁止普攻");Call(weapons,"Update");Check(weapons.ActiveCount==0,"技能期间实际无新弹丸 "+phase);}
        castField.SetValue(arena,idle);Check(arena.CanAutoAttack,"技能结束恢复普攻资格");
        Call(weapons,"Update");Check(weapons.ActiveCount==1,"空闲时实际发射飞镖");weapons.ResetRun();
        // Use the real projectile state machine with deterministic dt and target positions.
        inventory.AddMastery(6);Call(weapons,"Update");var shot=((IList)Get(weapons,"shots"))[0];
        Set(shot,"position",new Vector2(8.99f,0));Set(shot,"distance",8.99f);Call(weapons,"Tick",shot,.1f);
        Check(Get(shot,"phase").ToString()=="Spin","飞镖射程末端进入旋转");
        Set(enemy,"Position",(Vector2)Get(shot,"position"));float before=(float)Get(enemy,"Health");
        for(int i=0;i<8;i++)Call(weapons,"Tick",shot,.25f);
        Near(before-(float)Get(enemy,"Health"),40,"飞镖2秒精确8次伤害");Check(Get(shot,"phase").ToString()=="Return","飞镖返程阶段");
        Set(shot, "position", new Vector2(3, 0)); Set(shot, "factor", AutoWeaponController.ProjectileFactor(1));
        Set(enemy, "Position", new Vector2(2, 0)); before = (float)Get(enemy, "Health");
        Call(weapons, "Tick", shot, .09f);
        Near(before - (float)Get(enemy, "Health"), 16, "第二枚飞镖返程伤害应用80%衰减");
        Call(weapons, "Tick", shot, .01f);
        Near(before - (float)Get(enemy, "Health"), 16, "同一枚飞镖返程不重复命中同一敌人");
        Call(weapons,"Tick",shot,2f);Check(Get(shot,"phase").ToString()=="HomeSpin","飞镖到达后第二段旋转");
        weapons.ResetRun();Set(enemy,"Position",new Vector2(2,0));
        for(int i=0;i<5;i++)bleed.Apply(id,1,.5f);
        var bleedState=((IList)Get(bleed,"states"))[0];Set(bleedState,"tick",.4f);bleed.Apply(id,1,.5f);
        Near((float)Get(bleedState,"tick"),.4f,"流血刷新不重置tick");Check(bleed.IsFull(id,1),"流血满层识别");
        inventory.Acquire(WeaponKind.Bow);inventory.Equip(WeaponKind.Bow);inventory.AddProjectile();inventory.AddProjectile();inventory.AddDirection();inventory.AddDirection();
        Call(weapons,"Update");Check(weapons.ActiveCount==3,"首波同时发射三个方向");
        Call(weapons,"AdvanceBurst",.09f);Check(weapons.ActiveCount==3,"追加弹丸未满0.1秒不发射");
        Call(weapons,"AdvanceBurst",.01f);Check(weapons.ActiveCount==6,"0.1秒后三个方向同发第二波");
        Call(weapons,"AdvanceBurst",.1f);Check(weapons.ActiveCount==9,"三波合计九主箭");
        shot=((IList)Get(weapons,"shots"))[0];Call(weapons,"Split",shot);Check(weapons.ActiveCount==14,"三级弓箭分裂五支");
        var child=((IList)Get(weapons,"shots"))[13];Call(weapons,"Split",child);Check(weapons.ActiveCount==14,"子箭不递归分裂");
        Near((float)Get(child,"speed"),inventory.Current.speed,"子箭满速重置");
        var roots=new List<object>();foreach(var item in (IList)Get(weapons,"shots"))if(!(bool)Get(item,"child"))roots.Add(item);
        for(int i=1;i<roots.Count;i++)Call(weapons,"Split",roots[i]);Check(weapons.ActiveCount==54,"九主箭完整分裂共54弹丸");
        weapons.ResetRun();inventory.ResetRun();inventory.Acquire(WeaponKind.Bow);inventory.Equip(WeaponKind.Bow);
        Set(enemy,"Position",new Vector2(2,0));Call(weapons,"Update");shot=((IList)Get(weapons,"shots"))[0];
        Set(shot,"speed",inventory.Current.speed*.5f);before=(float)Get(enemy,"Health");Call(weapons,"Tick",shot,.4f);
        Near(before-(float)Get(enemy,"Health"),75,"半速基础弓箭实际伤害");weapons.ResetRun();
        inventory.ResetRun();inventory.AddMastery(1);
        var e2=CreateEnemy(arena,new Vector2(3,0),10000);var e3=CreateEnemy(arena,new Vector2(4,0),10000);
        before=(float)Get(enemy,"Health");Call(weapons,"Update");shot=((IList)Get(weapons,"shots"))[0];Call(weapons,"Tick",shot,.5f);
        Near(before-(float)Get(enemy,"Health"),100,"飞镖首目标伤害");Near(10000-(float)Get(e2,"Health"),70,"飞镖第二目标伤害");Near(10000-(float)Get(e3,"Health"),40,"飞镖第三目标伤害");
        weapons.ResetRun();bleed.ResetRun();before=(float)Get(enemy,"Health");
        for(int i=0;i<3;i++)bleed.Apply(id,0,.5f);
        for(int i=0;i<8;i++)Call(bleed,"Tick",.5f);
        Near(before-(float)Get(enemy,"Health"),150,"基础流血完整4秒实际伤害");Check(((IList)Get(bleed,"states")).Count==0,"流血到期清除");
        for(int i=0;i<5;i++)bleed.Apply(id,1,.5f);before=(float)Get(enemy,"Health");
        for(int i=0;i<8;i++)Call(bleed,"Tick",.5f);
        Near(before-(float)Get(enemy,"Health"),250,"升级流血完整4秒实际伤害");
        Set(arena,"elapsed",20f);Set(arena,"baseGuardTap",true);Set(arena,"castingFusion",false);castField.SetValue(arena,Enum.Parse(castField.FieldType,"Active"));
        Set(arena,"skill",Enum.Parse(arena.GetType().GetField("skill",Flags).FieldType,"Guard"));Call(arena,"FinishSkill");
        Near(((float[])Get(arena,"readyAt"))[0],22.5f,"技能结束事件启动点按CD");
        stats.Add(RunStat.LifeSteal,5);stats.Receive(20);float playerHP=stats.HP;
        var weak=CreateEnemy(arena,new Vector2(5,0),1);arena.WeaponHit((int)Get(weak,"SpawnId"),1000,DamageSource.Weapon,false);
        Near(stats.HP-playerHP,.05f,"吸血只按实际生命损失");
        // Exhaust one enemy, verify real death rewards and no second reward for the same id.
        int dropBefore=drops.LiveDrops;arena.WeaponHit(id,99999,DamageSource.Weapon);Check(drops.LiveDrops==dropBefore+2,"死亡生成经验金币");
        arena.WeaponHit(id,99999,DamageSource.Weapon);Check(drops.LiveDrops==dropBefore+2,"重复命中不重复发奖");
        stats.maxHP=100;stats.critChance=5;arena.DebugRestart();
        Check(progression.Level==1&&progression.Coins==0&&inventory.Mastery==0&&stats.Generators==0&&weapons.ActiveCount==0&&drops.LiveDrops==0,"重开清空本局状态");
        string report=$"M6 P0-P4: {checks} checks passed\n"+string.Join("\n",results);
        File.WriteAllText("M6-validation.txt",report);Debug.Log(report);
    }
    static object CreateEnemy(ArenaPrototype arena,Vector2 position,float health)
    {
        arena.DebugSpawnEnemy();var enemies=(IList)Get(arena,"enemies");var enemy=enemies[enemies.Count-1];
        Set(enemy,"Position",position);Set(enemy,"Health",health);((GameObject)Get(enemy,"Object")).transform.position=position;return enemy;
    }
}
