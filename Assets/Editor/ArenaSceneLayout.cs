using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class ArenaSceneLayout
{
    [MenuItem("Tools/Arena/Organize GameScene")]
    public static void Organize()
    {
        var arena = Object.FindObjectOfType<ArenaPrototype>();
        if (!arena || arena.gameObject.scene.name != "GameScene") return;
        if (arena.transform.Find("玩家")) { M6SceneSetup.Install(); return; }
        var serialized = new SerializedObject(arena);
        Transform systems = Child(arena.transform, "系统");
        Child(systems, "对局流程");
        Child(systems, "输入管理");
        Child(systems, "技能组合管理");
        Set(serialized, "audioSettings", Child(systems, "音频管理").gameObject.AddComponent<ArenaAudioSettings>());
        var world = Child(arena.transform, "场景");
        var grid = GameObject.Find("Grid");
        if (grid) { grid.transform.SetParent(world, true); grid.name = "网格"; }
        var player = Child(arena.transform, "玩家");
        var body = player.gameObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 0;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.freezeRotation = true;
        player.gameObject.AddComponent<BoxCollider2D>();
        var visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.name = "本体";
        var playerAppearance = Child(player, "外观");
        visual.transform.SetParent(playerAppearance, false);
        visual.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/PlayerIdlePreview.mat");
        visual.AddComponent<ArenaPlayerAnimation>();
        Child(player, "特效挂点");
        Set(serialized, "scenePlayer", body);
        Set(serialized, "movement", Child(player, "功能").gameObject.AddComponent<ArenaMovementTuning>());
        var enemies = Child(arena.transform, "敌人");
        Set(serialized, "enemyTuning", Child(enemies, "生成管理").gameObject.AddComponent<ArenaEnemyTuning>());
        var runtime = Child(arena.transform, "运行时");
        Set(serialized, "enemyInstances", Child(runtime, "敌人实例"));
        Set(serialized, "playerShots", Child(runtime, "玩家子弹"));
        Set(serialized, "enemyShots", Child(runtime, "敌人子弹"));
        Set(serialized, "effectInstances", Child(runtime, "技能特效实例"));
        var actors = new[] { playerAppearance, Child(enemies,"追击敌人"), Child(enemies,"冲锋敌人"),
            Child(enemies,"射击敌人"), Child(player,"子弹外观"), Child(enemies,"子弹外观") };
        var looks = serialized.FindProperty("actorLooks");
        looks.arraySize = actors.Length;
        for (int i = 0; i < actors.Length; i++)
            looks.GetArrayElementAtIndex(i).objectReferenceValue = actors[i].gameObject.AddComponent<ArenaActorAppearance>();

        var options = serialized.FindProperty("skillOptions");
        var sounds = serialized.FindProperty("skillSounds");
        var fusionLooks = serialized.FindProperty("fusionLooks");
        options.arraySize = sounds.arraySize = 6;
        fusionLooks.arraySize = 3;
        string[] names = { "按钮A_防御", "按钮B_冲刺", "按钮C_斩击", "按钮A+B", "按钮B+C", "按钮A+C" };
        var hud = arena.GetComponentInChildren<ArenaHud>();
        for (int i = 0; i < names.Length; i++)
        {
            var skill = arena.transform.Find("技能/" + names[i]);
            var function = skill.Find("功能");
            var preview = new SerializedObject(Child(skill, "调试").gameObject.AddComponent<ArenaSkillPreview>());
            Set(preview, "tuning", function.GetComponent<MonoBehaviour>());
            Set(preview, "player", player);
            Set(preview, "arena", arena);
            preview.FindProperty("skillIndex").intValue = i;
            preview.ApplyModifiedPropertiesWithoutUndo();
            var rule = function.gameObject.AddComponent<ArenaSkillOptions>();
            options.GetArrayElementAtIndex(i).objectReferenceValue = rule;
            var ruleData = new SerializedObject(rule);
            ruleData.FindProperty("cooldown").floatValue = i < 3 ? 8 : 12;
            if (hud) Set(ruleData, "button", i < 3 ? hud.slots[i] : hud.combinations[i - 3]);
            ruleData.ApplyModifiedPropertiesWithoutUndo();
            sounds.GetArrayElementAtIndex(i).objectReferenceValue = Child(skill, "音效").gameObject.AddComponent<ArenaSkillAudio>();
            if (i >= 3)
                fusionLooks.GetArrayElementAtIndex(i - 3).objectReferenceValue =
                    Child(skill.Find("外观"), "组合表现").gameObject.AddComponent<ArenaSkillAppearance>();
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(arena.gameObject.scene);
        EditorSceneManager.SaveScene(arena.gameObject.scene);
    }

    static Transform Child(Transform parent, string name)
    {
        var child = new GameObject(name).transform;
        child.SetParent(parent, false);
        return child;
    }

    static void Set(SerializedObject data, string name, Object value) =>
        data.FindProperty(name).objectReferenceValue = value;

    [MenuItem("Tools/Arena/Bind Skill Debug")]
    public static void BindSkillDebug()
    {
        var arena = Object.FindObjectOfType<ArenaPrototype>();
        if (!arena || arena.gameObject.scene.name != "GameScene") return;
        string[] names = { "按钮A_防御", "按钮B_冲刺", "按钮C_斩击", "按钮A+B", "按钮B+C", "按钮A+C" };
        for (int i = 0; i < names.Length; i++)
        {
            var obj = arena.transform.Find("技能/" + names[i] + "/调试");
            if (!obj) continue;
            var data = new SerializedObject(obj.GetComponent<ArenaSkillPreview>());
            Set(data, "arena", arena);
            data.FindProperty("skillIndex").intValue = i;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorSceneManager.SaveScene(arena.gameObject.scene);
    }

    [MenuItem("Tools/Arena/Bind Effect Profiles")]
    public static void BindEffectProfiles()
    {
        var arena = Object.FindObjectOfType<ArenaPrototype>();
        if (!arena || arena.gameObject.scene.name != "GameScene") return;
        var data = new SerializedObject(arena);
        AddProfile(data, "guardWaveLook", arena.transform.Find("技能/按钮A_防御/外观"), "防御波",
            new Color(.72f,.55f,.26f));
        AddProfile(data, "slashSweepLook", arena.transform.Find("技能/按钮C_斩击/外观"), "斩击轨迹",
            new Color(.26f,.67f,1f), .24f);
        var guardSlash = arena.transform.Find("技能/按钮A+C/外观");
        AddProfile(data, "counterLook", guardSlash, "反击阶段", new Color(.26f,.67f,1f), .24f);
        AddProfile(data, "finishGuardLook", guardSlash, "结束防御", new Color(.26f,.67f,1f), .24f);
        AddProfile(data, "dodgeLook", guardSlash, "闪避提示", new Color(1f,.9f,.53f));
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(arena.gameObject.scene);
    }

    static void AddProfile(SerializedObject arena, string field, Transform parent, string name, Color color, float width = .085f)
    {
        if (arena.FindProperty(field).objectReferenceValue) return;
        var profile = Child(parent, name).gameObject.AddComponent<ArenaSkillAppearance>();
        var data = new SerializedObject(profile);
        data.FindProperty("color").colorValue = color;
        data.FindProperty("width").floatValue = width;
        data.ApplyModifiedPropertiesWithoutUndo();
        Set(arena, field, profile);
    }

    static Font Font => Resources.Load<Font>("ArenaChinese");
    static readonly Color Panel = new Color(.055f, .08f, .12f, .94f);
    static readonly Color Edge = new Color(.24f, .31f, .4f);
    static readonly Vector2 Center = Vector2.one * .5f;

    [MenuItem("Tools/Arena/Build GameScene UI")]
    public static void Build()
    {
        var arena = Object.FindObjectOfType<ArenaPrototype>();
        if (!arena || arena.gameObject.scene.name != "GameScene" || arena.transform.Find("界面")) return;
        EnsureCircleSprite("ArenaCircle", false);
        EnsureCircleSprite("ArenaRing", true);
        var root = Rect("界面", arena.transform, Center, Vector2.zero, Vector2.zero);
        var canvas = root.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        root.gameObject.AddComponent<GraphicRaycaster>();
        var hud = root.gameObject.AddComponent<ArenaHud>();
        hud.safeArea = Stretch("安全区域", root);
        var ui = hud.safeArea;
        var topLeft = Vector2.up;
        var top = new Vector2(.5f, 1);
        var health = Rect("生命条", ui, topLeft, new Vector2(98,-42), new Vector2(164,60));
        Image(health, Panel);
        hud.hpLabel = Label("生命数值", health, Center, new Vector2(0,12), new Vector2(150,28), "生命  100 / 100", 19);
        Image(Rect("生命条底色", health, Center, new Vector2(0,-10), new Vector2(122,8)), Edge);
        hud.hpFill = Image(Rect("生命条填充", health, Center, new Vector2(0,-10), new Vector2(122,8)), new Color(.35f,.94f,.69f));
        SetFill(hud.hpFill, UnityEngine.UI.Image.FillMethod.Horizontal);
        var stats = Rect("对局状态", ui, top, new Vector2(0,-42), new Vector2(180,60));
        Image(stats, Panel);
        hud.timeLabel = Label("时间", stats, Center, new Vector2(-44,0), new Vector2(84,54), "时间\n00:00", 17);
        hud.killsLabel = Label("击杀", stats, Center, new Vector2(44,0), new Vector2(84,54), "击杀\n0", 17);
        hud.joystick = Rect("移动摇杆", ui, Vector2.zero, new Vector2(84,80), new Vector2(106,106));
        Image(hud.joystick, Panel, true);
        hud.knob = Rect("摇杆中心", hud.joystick, Center, Vector2.zero, new Vector2(38,38));
        Image(hud.knob, Edge, true);
        var slots = Stretch("技能按钮区域", ui);
        hud.slots = new[] {
            Skill("按钮A", slots, new Vector2(-111,131), "A", "格挡"),
            Skill("按钮B", slots, new Vector2(-158,54), "B", "冲刺"),
            Skill("按钮C", slots, new Vector2(-65,54), "C", "环形斩") };
        hud.combinations = new[] {
            Skill("按钮A+B", slots, new Vector2(-65,54), "A+B", "防御冲锋"),
            Skill("按钮B+C", slots, new Vector2(-65,54), "B+C", "冲刺双斩"),
            Skill("按钮A+C", slots, new Vector2(-65,54), "A+C", "闪") };
        foreach (var button in hud.combinations) button.gameObject.SetActive(false);
        hud.cancel = Skill("取消组合", slots, new Vector2(-111,131), "取消", "");
        hud.cancel.gameObject.SetActive(false);
        hud.drag = Skill("拖拽显示", Stretch("拖拽显示层", ui), Vector2.zero, "", "");
        hud.drag.gameObject.SetActive(false);
        hud.warningLayer = Stretch("战斗提示", ui);
        hud.warningTemplate = Label("近战预警模板", hud.warningLayer, Center, Vector2.zero, new Vector2(24,28), "!", 22);
        hud.warningTemplate.color = new Color(1,.24f,.3f);
        hud.warningTemplate.verticalOverflow = VerticalWrapMode.Overflow;
        hud.warningTemplate.gameObject.SetActive(false);
        hud.dodgeNotice = Label("极限闪避提示", hud.warningLayer, Center, Vector2.zero, new Vector2(192,28), "极限闪避！", 19);
        hud.dodgeNotice.gameObject.SetActive(false);
        var overlay = Stretch("结算面板", ui);
        hud.resultPanel = overlay.gameObject;
        Image(overlay, new Color(.025f,.04f,.065f,.75f));
        var results = Rect("结算内容", overlay, Center, Vector2.zero, new Vector2(330,208));
        Image(results, Panel);
        Label("标题", results, Center, new Vector2(0,65), new Vector2(290,45), "战斗结束", 30);
        hud.resultStats = Label("战斗统计", results, Center, new Vector2(0,19), new Vector2(280,40), "存活时间  00:00     击杀  0", 18);
        var again = Rect("再来一局", results, Center, new Vector2(0,-61), new Vector2(238,46));
        var target = Image(again, new Color(.17f,.38f,.34f));
        target.raycastTarget = true;
        hud.retry = again.gameObject.AddComponent<Button>();
        hud.retry.targetGraphic = target;
        Label("按钮文字", again, Center, Vector2.zero, again.sizeDelta, "再来一局", 19);
        overlay.gameObject.SetActive(false);
        if (!Object.FindObjectOfType<EventSystem>())
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        var serialized = new SerializedObject(arena);
        serialized.FindProperty("hud").objectReferenceValue = hud;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(arena.gameObject.scene);
        EditorSceneManager.SaveScene(arena.gameObject.scene);
    }

    static ArenaSkillButton Skill(string name, Transform parent, Vector2 position, string key, string caption)
    {
        var root = Rect(name, parent, Vector2.right, position, new Vector2(68,68));
        var button = root.gameObject.AddComponent<ArenaSkillButton>();
        button.border = Image(root, Edge, true);
        Image(Rect("背景", root, Center, Vector2.zero, new Vector2(62,62)), Panel, true);
        var icon = Rect("图标", root, Center, new Vector2(0,8), new Vector2(28,28));
        button.icon = icon.gameObject.AddComponent<RawImage>();
        button.icon.raycastTarget = false;
        button.icon.enabled = false;
        button.title = Label("主文字", root, Center, new Vector2(0,10), new Vector2(68,33), key, 19);
        button.caption = Label("说明", root, Center, new Vector2(0,-15), new Vector2(68,18), caption, 11);
        button.cooldown = Label("冷却数字", root, Center, new Vector2(0,9), new Vector2(68,34), "", 19);
        button.cooldown.enabled = false;
        button.progress = Image(Rect("冷却进度", root, Center, new Vector2(0,-16), new Vector2(34,4)), Edge);
        SetFill(button.progress, UnityEngine.UI.Image.FillMethod.Horizontal);
        button.progress.enabled = false;
        button.duration = Image(Rect("持续时间环", root, Center, Vector2.zero, new Vector2(84,84)), new Color(.6f,1,.87f));
        button.duration.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Resources/ArenaRing.png");
        SetFill(button.duration, UnityEngine.UI.Image.FillMethod.Radial360);
        button.duration.enabled = false;
        button.charges = Label("反击次数", root, Center, new Vector2(0,8), new Vector2(68,28), "", 14);
        button.charges.color = new Color(1,.89f,.42f);
        button.charges.enabled = false;
        return button;
    }

    static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    static RectTransform Stretch(string name, Transform parent)
    {
        var rect = Rect(name, parent, Center, Vector2.zero, Vector2.zero);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        return rect;
    }

    static Text Label(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, string value, int fontSize)
    {
        var text = Rect(name, parent, anchor, position, size).gameObject.AddComponent<Text>();
        text.font = Font;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }

    static Image Image(RectTransform rect, Color color, bool circle = false)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        if (circle) image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Resources/ArenaCircle.png");
        return image;
    }

    static void SetFill(Image image, UnityEngine.UI.Image.FillMethod method)
    {
        image.type = UnityEngine.UI.Image.Type.Filled;
        image.fillMethod = method;
    }

    static void EnsureCircleSprite(string name, bool ring)
    {
        string path = $"Assets/Game/Resources/{name}.png";
        if (AssetDatabase.LoadAssetAtPath<Sprite>(path)) return;
        const int size = 96;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float radius = new Vector2(x + .5f - size / 2f, y + .5f - size / 2f).magnitude;
            float alpha = Mathf.Clamp01(size / 2f - radius);
            if (ring) alpha *= Mathf.Clamp01(radius - 42f);
            texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
        }
        System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }
}
