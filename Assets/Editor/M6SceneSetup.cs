using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class M6SceneSetup
{
    const string Folder="Assets/Game/Data/M6";
    [MenuItem("Tools/Arena/M6/安装或修复场景引用")]
    public static void Install()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("请先退出运行模式");
        var arena=Object.FindObjectOfType<ArenaPrototype>();
        if(!arena||arena.gameObject.scene.name!="GameScene")throw new System.InvalidOperationException("请打开 GameScene");
        System.IO.Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        var root=arena.transform;
        var legacyStatus=root.Find("界面/安全区域/对局状态") as RectTransform;
        if(legacyStatus){legacyStatus.anchorMin=legacyStatus.anchorMax=new Vector2(1,1);legacyStatus.anchoredPosition=new Vector2(-110,-42);}
        var stats=Component<RunStats>(root,"玩家/功能/角色属性");
        stats.movement=root.GetComponentInChildren<ArenaMovementTuning>();
        var clock=Component<RunClock>(root,"系统/时间与暂停");
        var resolver=Component<DamageResolver>(root,"战斗/伤害结算");resolver.stats=stats;
        var inventory = MoveWeaponComponent<WeaponInventory>(root, "玩家/功能/武器背包", "武器背包");
        if(inventory.definitions==null||inventory.definitions.Length!=3)
        {
            inventory.definitions=new WeaponDefinition[3];
            string[] names={"飞镖","飞刀","弓箭"};
            for(int i=0;i<3;i++)
            {
                int index=i;
                inventory.definitions[i]=Asset<WeaponDefinition>("Weapon"+i,d=>{
                    d.kind=(WeaponKind)index;d.displayName=names[index];d.interval=index==2?3.5f:2.5f;
                    d.speed=index==0?10:index==1?7:14;d.range=index==2?14:9;
                    d.color=index==0?Color.cyan:index==1?new Color(1,.35f,.35f):new Color(1,.85f,.3f);
                });
            }
        }
        var weaponRoot = Path(root, "武器");
        foreach (var definition in inventory.definitions)
        {
            if (!definition) continue;
            Component<WeaponSetup>(weaponRoot, definition.displayName).definition = definition;
        }
        var progression=Component<RunProgression>(root,"成长/经验与升级奖励");
        progression.stats=stats;progression.inventory=inventory;progression.clock=clock;
        if(progression.cards==null||progression.cards.Length==0)
        {
            var cards = new System.Collections.Generic.List<AbilityCardDefinition>();
            string[] titles={"追加弹丸","多向攻击","武器熟练","获取飞镖","获取飞刀","获取弓箭"};
            string[] descriptions={"每方向弹丸 +1，最多 3 发\n后续弹丸伤害 80% / 60%","攻击方向 +1，最多 3 向","共享熟练 +1\n累计 1 / 3 / 6 点升级","加入武器背包，重复则提升熟练","加入武器背包，重复则提升熟练","加入武器背包，重复则提升熟练"};
            foreach (int index in new[] { 0, 1, 3, 4, 5 })
                cards.Add(Asset<AbilityCardDefinition>("Card"+index,d=>{d.title=titles[index];d.description=descriptions[index];d.kind=index<2?(AbilityCardKind)index:AbilityCardKind.Weapon;d.weapon=(WeaponKind)Mathf.Max(0,index-3);}));
            progression.cards = cards.ToArray();
        }
        if(progression.shards==null||progression.shards.Length==0)
        {
            progression.shards=new StatShardDefinition[19];float[] amounts={10,12,20,8,15,12,15,5,5,.1f,10,1,10};int n=0;
            for(int i=0;i<13;i++)for(int q=i<6?0:2;q< (i<6?2:3);q++)
            {int stat=i,quality=q;progression.shards[n++]=Asset<StatShardDefinition>($"Shard{i}_{q}",d=>{d.stat=(RunStat)stat;d.quality=(ShardQuality)quality;d.amount=amounts[stat]*(quality==1?2:1);});}
        }
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/ArenaSprite.mat");
        var shieldVisual=Component<PlayerShieldVisual>(root,"玩家/外观/护盾表现");shieldVisual.arena=arena;shieldVisual.stats=stats;shieldVisual.material=material;
        var drops=Component<EnemyRewards>(root,"敌人/奖励档位与掉落");drops.arena=arena;drops.progression=progression;drops.clock=clock;drops.material=material;drops.container=Path(root,"运行时/经验与金币");
        progression.enemyRewards=drops;
        var bleed=Component<BleedStatusController>(root,"战斗/流血状态");bleed.arena=arena;bleed.clock=clock;
        var weapons = MoveWeaponComponent<AutoWeaponController>(root, "玩家/功能/自动武器", "自动武器");
        weapons.arena=arena;weapons.stats=stats;weapons.clock=clock;weapons.inventory=inventory;weapons.bleed=bleed;weapons.fallbackMaterial=material;weapons.container=Path(root,"运行时/自动武器弹丸");
        BindWeaponArt(inventory, weapons);
        var debug=Component<RunDebug>(root,"系统/M6调试");debug.arena=arena;debug.stats=stats;debug.progression=progression;debug.inventory=inventory;debug.clock=clock;
        var serialized=new SerializedObject(arena);
        Bind(serialized,"runStats",stats);Bind(serialized,"runClock",clock);Bind(serialized,"damageResolver",resolver);
        Bind(serialized,"enemyRewards",drops);Bind(serialized,"progression",progression);Bind(serialized,"inventory",inventory);Bind(serialized,"autoWeapons",weapons);
        BindEnemyArt(root, serialized);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        BuildUI(root,arena,stats,clock,progression,inventory);
        EditorSceneManager.MarkSceneDirty(arena.gameObject.scene);EditorSceneManager.SaveScene(arena.gameObject.scene);AssetDatabase.SaveAssets();
    }
    static void BindEnemyArt(Transform root, SerializedObject arena)
    {
        const string folder = "Assets/Game/Enemies/Bandit";
        var idle = ImportSprite(folder + "/bandit-standing.png", 256, 256);
        if (!idle) return;
        var profile = Component<EnemyAnimationProfile>(root, "敌人/追击敌人/山贼动画");
        if (!profile.idle) profile.idle = idle;
        if (profile.move == null || profile.move.Length == 0) profile.move = LoadEnemySequence(folder, "move", 8);
        if (profile.attack == null || profile.attack.Length == 0) profile.attack = LoadEnemySequence(folder, "attack", 4);
        Bind(arena, "chaserAnimation", profile);
    }
    static Sprite[] LoadEnemySequence(string folder, string state, int count)
    {
        var frames = new Sprite[count];
        for (int i = 0; i < count; i++) frames[i] = ImportSprite($"{folder}/{state}/{state}-{i + 1:00}.png", 256, 256);
        return frames;
    }
    static void BindWeaponArt(WeaponInventory inventory, AutoWeaponController controller)
    {
        string[] names = { "shuriken", "throwing-knife", "arrow" };
        foreach (var definition in inventory.definitions)
        {
            if (!definition || definition.texture) continue;
            var sprite = ImportWeaponSprite(names[(int)definition.kind]);
            if (!sprite) continue;
            definition.texture = sprite.texture;
            if (!definition.icon) definition.icon = sprite;
            definition.color = Color.white;
            EditorUtility.SetDirty(definition);
        }
        var serialized = new SerializedObject(controller);
        var frames = serialized.FindProperty("hitFrames");
        if (frames.arraySize != 0) return;
        var first = ImportWeaponSprite("hit-01");
        var second = ImportWeaponSprite("hit-02");
        if (!first || !second) return;
        frames.arraySize = 2;
        frames.GetArrayElementAtIndex(0).objectReferenceValue = first;
        frames.GetArrayElementAtIndex(1).objectReferenceValue = second;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static Sprite ImportWeaponSprite(string name) => ImportSprite($"Assets/Game/Weapons/Art/{name}.png", 128, 2048);

    static Sprite ImportSprite(string path, int pixelsPerUnit, int maxSize)
    {
        if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return null;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.maxTextureSize = maxSize;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static T MoveWeaponComponent<T>(Transform root, string oldPath, string name) where T : Component
    {
        var parent = Path(root, "武器");
        var original = root.Find(oldPath);
        if (original && original.GetComponent<T>())
        {
            original.SetParent(parent, false);
            return original.GetComponent<T>();
        }
        return Component<T>(parent, name);
    }
    static T Asset<T>(string name,System.Action<T> initialize) where T:ScriptableObject
    {
        string path=Folder+"/"+name+".asset";var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(asset)return asset;
        asset=ScriptableObject.CreateInstance<T>();initialize(asset);AssetDatabase.CreateAsset(asset,path);return asset;
    }
    static Transform Path(Transform root,string path)
    {
        foreach(var name in path.Split('/')){var next=root.Find(name);if(!next){next=new GameObject(name).transform;next.SetParent(root,false);}root=next;}return root;
    }
    static T Component<T>(Transform root,string path)where T:Component {var obj=Path(root,path);return obj.GetComponent<T>()??obj.gameObject.AddComponent<T>();}
    static void Bind(SerializedObject obj,string field,Object value)=>obj.FindProperty(field).objectReferenceValue=value;
    static RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size)
    {
        var obj=new GameObject(name,typeof(RectTransform));var rect=(RectTransform)obj.transform;rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=anchor;rect.pivot=anchor;rect.anchoredPosition=pos;rect.sizeDelta=size;return rect;
    }
    static Text Text(string name,Transform parent,Vector2 pos,Vector2 size,int fontSize=17)
    {
        var rect=Rect(name,parent,new Vector2(.5f,.5f),pos,size);var text=rect.gameObject.AddComponent<Text>();
        text.font=Resources.Load<Font>("ArenaChinese");text.fontSize=fontSize;text.color=Color.white;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;text.verticalOverflow=VerticalWrapMode.Overflow;return text;
    }
    static Image Image(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size,Color color)
    {var rect=Rect(name,parent,anchor,pos,size);var image=rect.gameObject.AddComponent<Image>();image.color=color;return image;}
    static Button Button(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size,string label)
    {
        var image=Image(name,parent,anchor,pos,size,new Color(.12f,.2f,.3f,.98f));var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
        Text("文字",image.transform,Vector2.zero,size-Vector2.one*12).text=label;return button;
    }
    static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
    static void BuildUI(Transform root,ArenaPrototype arena,RunStats stats,RunClock clock,RunProgression progression,WeaponInventory inventory)
    {
        var existing=root.GetComponentInChildren<RunHud>(true);
        if (existing)
        {
            // Only migrate the original layout. Preserve hand-edited HUD geometry.
            if (!existing.coinsLabel) ConfigureBars(existing);
            return;
        }
        var canvas=Rect("成长界面",root,Vector2.zero,Vector2.zero,Vector2.zero);
        var c=canvas.gameObject.AddComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=20;
        canvas.gameObject.AddComponent<GraphicRaycaster>();var scaler=canvas.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(960,540);scaler.matchWidthOrHeight=.5f;
        var hud=canvas.gameObject.AddComponent<RunHud>();hud.arena=arena;hud.stats=stats;hud.clock=clock;hud.progression=progression;hud.inventory=inventory;
        var safe=Rect("安全区域",canvas,Vector2.zero,Vector2.zero,Vector2.zero);Stretch(safe);safe.gameObject.AddComponent<RunSafeArea>();
        var top=Rect("成长状态",safe,new Vector2(.5f,1),new Vector2(0,-8),new Vector2(430,70));
        hud.progressLabel=Text("等级经验金币",top,new Vector2(0,20),new Vector2(430,24),16);
        hud.progressLabel.text="等级 1   经验 0/40   金币 0";
        var bar=Image("经验底",top,new Vector2(.5f,.5f),new Vector2(0,0),new Vector2(380,6),new Color(.1f,.15f,.2f));
        hud.xpFill=Image("经验填充",bar.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(380,6),Color.cyan);hud.xpFill.type=UnityEngine.UI.Image.Type.Filled;hud.xpFill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;hud.xpFill.fillAmount=0;
        hud.shieldLabel=Text("护盾",top,new Vector2(0,-18),new Vector2(430,24),14);hud.shieldLabel.text="护盾未解锁";
        hud.shieldFill=Image("护盾填充",top,new Vector2(.5f,.5f),new Vector2(0,-34),new Vector2(260,4),new Color(.4f,.7f,1));hud.shieldFill.type=UnityEngine.UI.Image.Type.Filled;hud.shieldFill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;hud.shieldFill.fillAmount=0;
        var left=Rect("背包入口",safe,new Vector2(0,1),new Vector2(12,-90),new Vector2(210,85));
        hud.weaponLabel=Text("武器状态",left,new Vector2(0,23),new Vector2(210,26),14);hud.weaponLabel.text="飞镖 +0   熟练 0/6";
        hud.inventoryButton=Button("武器背包",left,new Vector2(.5f,.5f),new Vector2(-51,-13),new Vector2(98,34),"武器背包");
        hud.statsButton=Button("属性面板",left,new Vector2(.5f,.5f),new Vector2(51,-13),new Vector2(98,34),"角色属性");
        var shade=Image("弹窗遮罩",safe,Vector2.zero,Vector2.zero,Vector2.zero,new Color(.015f,.025f,.04f,.95f));Stretch(shade.rectTransform);hud.modal=shade.gameObject;
        var center=Rect("奖励与背包",shade.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(740,430));
        hud.panelTitle=Text("标题",center,new Vector2(0,175),new Vector2(700,48),25);hud.panelTitle.text="升级奖励";
        hud.closeButton=Button("关闭",center,new Vector2(.5f,.5f),new Vector2(0,-175),new Vector2(160,42),"返回战斗");
        var choices=Rect("卡片选择",center,new Vector2(.5f,.5f),Vector2.zero,new Vector2(710,280));hud.choices=choices.gameObject;
        hud.optionButtons=new Button[3];hud.optionLabels=new Text[3];hud.optionIcons=new Image[3];
        for(int i=0;i<3;i++)
        {
            var button=Button("选项"+(i+1),choices,new Vector2(.5f,.5f),new Vector2((i-1)*235,0),new Vector2(220,250),"能力卡");hud.optionButtons[i]=button;hud.optionLabels[i]=button.GetComponentInChildren<Text>();
            hud.optionLabels[i].fontSize=19;hud.optionIcons[i]=Image("图标",button.transform,new Vector2(.5f,.5f),new Vector2(0,80),new Vector2(48,48),Color.white);hud.optionIcons[i].raycastTarget=false;
        }
        hud.detailLabel=Text("属性详情",center,new Vector2(0,0),new Vector2(660,310),16);hud.detailLabel.alignment=TextAnchor.UpperLeft;hud.detail=hud.detailLabel.gameObject;
        hud.modal.SetActive(false);
        ConfigureBars(hud);
    }
    static void ConfigureBars(RunHud hud)
    {
        foreach(var label in hud.GetComponentsInChildren<Text>(true))label.verticalOverflow=VerticalWrapMode.Overflow;
        hud.detailLabel.fontSize=14;hud.detailLabel.lineSpacing=.9f;
        hud.xpFill.rectTransform.pivot=new Vector2(0,.5f);hud.xpFill.rectTransform.anchoredPosition=new Vector2(-190,0);
        hud.shieldFill.rectTransform.pivot=new Vector2(0,.5f);hud.shieldFill.rectTransform.anchoredPosition=new Vector2(-130,-34);
        var safe = hud.transform.Find("安全区域");
        var status = safe.Find("成长状态") as RectTransform;
        status.anchorMin = status.anchorMax = status.pivot = new Vector2(0, 1);
        status.anchoredPosition = new Vector2(16, -14);
        status.sizeDelta = new Vector2(220, 182);
        var background = status.GetComponent<Image>() ?? status.gameObject.AddComponent<Image>();
        background.color = new Color(.035f, .065f, .09f, .92f);
        background.raycastTarget = false;
        var legacyHud = hud.arena.GetComponentInChildren<ArenaHud>();
        Place(legacyHud.hpLabel.rectTransform, status, new Vector2(12, -7), new Vector2(196, 25));
        Place(legacyHud.hpFill.rectTransform, status, new Vector2(12, -35), new Vector2(196, 8));
        var oldHealth = legacyHud.safeArea.Find("生命条");
        if (oldHealth) oldHealth.gameObject.SetActive(false);
        Place(hud.shieldLabel.rectTransform, status, new Vector2(12, -48), new Vector2(196, 22));
        hud.shieldLabel.fontSize = 13;
        Place(hud.shieldFill.rectTransform, status, new Vector2(12, -73), new Vector2(196, 6));
        Place(hud.progressLabel.rectTransform, status, new Vector2(12, -87), new Vector2(196, 22));
        hud.progressLabel.fontSize = 13;
        var xpBackground = hud.xpFill.transform.parent as RectTransform;
        Place(xpBackground, status, new Vector2(12, -113), new Vector2(196, 6));
        Place(hud.xpFill.rectTransform, xpBackground, Vector2.zero, new Vector2(196, 6));
        if (!hud.coinsLabel) hud.coinsLabel = Text("金币计数", status, Vector2.zero, new Vector2(196, 24), 16);
        Place(hud.coinsLabel.rectTransform, status, new Vector2(12, -133), new Vector2(196, 24));
        hud.coinsLabel.text = "金币 0";
        hud.progressLabel.text = "等级 1   经验 0/40";
        var entry = safe.Find("背包入口") as RectTransform;
        Place(entry, safe, new Vector2(16, -204), new Vector2(220, 40));
        hud.weaponLabel.gameObject.SetActive(false);
        hud.statsButton.gameObject.SetActive(false);
        hud.inventoryButton.name = "角色按钮";
        Place((RectTransform)hud.inventoryButton.transform, entry, Vector2.zero, new Vector2(100, 38));
        var buttonText = hud.inventoryButton.GetComponentInChildren<Text>();
        buttonText.text = "角色";
        buttonText.rectTransform.sizeDelta = new Vector2(88, 30);
    }
    static void Place(RectTransform rect, Transform parent, Vector2 position, Vector2 size)
    {
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}

[CustomEditor(typeof(RunDebug))]
public sealed class RunDebugEditor:Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();var d=(RunDebug)target;
        EditorGUILayout.HelpBox("先运行 GameScene，再使用下方按钮。配置资产不保存本局成长。",MessageType.Info);
        using(new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if(GUILayout.Button("增加经验 / 测试升级"))d.AddXP();if(GUILayout.Button("增加金币"))d.AddCoins();
            if(GUILayout.Button("增加选定属性"))d.AddStat();if(GUILayout.Button("获取 / 重复测试武器"))d.Acquire();
            if(GUILayout.Button("共享熟练 +1"))d.Mastery();if(GUILayout.Button("生成一只敌人"))d.Spawn();
            if(GUILayout.Button("清空技能冷却"))d.Cooldowns();if(GUILayout.Button("重开并清空本局成长"))d.Restart();
        }
    }
}

[CustomEditor(typeof(RunStats))]
public sealed class RunStatsEditor:Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();DrawPropertiesExcluding(serializedObject,"health","shield","shieldCountdown","modifiers","shardCounts");serializedObject.ApplyModifiedProperties();
        if(Application.isPlaying)EditorGUILayout.HelpBox(((RunStats)target).Summary(),MessageType.None);
    }
}

[CustomEditor(typeof(WeaponInventory))]
public sealed class WeaponInventoryEditor:Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();DrawPropertiesExcluding(serializedObject,"owned","equipped","mastery","extraProjectiles","extraDirections");serializedObject.ApplyModifiedProperties();
        var inventory=(WeaponInventory)target;
        if(Application.isPlaying)EditorGUILayout.HelpBox($"装备：{inventory.Current.displayName} +{inventory.Level}\n共享熟练：{inventory.Mastery}/6\n每方向 {inventory.ProjectileCount} 发，{inventory.DirectionCount} 方向\n测试修改请使用 系统/M6调试。",MessageType.None);
    }
}
