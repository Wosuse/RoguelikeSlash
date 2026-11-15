using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterPanel : MonoBehaviour
{
    [Header("布局")]
    public float slotSize = 48;
    public float slotGap = 6;
    public Color panelColor = new Color(.055f, .09f, .14f, .98f);
    public Color gold = new Color(1, .78f, .2f);
    RunHud hud;
    RectTransform root, popup;
    Font font;
    readonly List<GameObject> generated = new List<GameObject>();

    public void Initialize(RunHud owner) { hud = owner; font = owner.detailLabel.font; }
    public void Show(bool visible)
    {
        gameObject.SetActive(visible);
        if (!visible) return;
        foreach (var item in generated) Destroy(item);
        generated.Clear();
        root = (RectTransform)transform;
        root.sizeDelta = new Vector2(740, 310);
        BuildStats();
        BuildRow("武器", 0, WeaponItems());
        BuildRow("能力卡", 105, AbilityItems());
        BuildRow("预留栏", 210, new List<Item>());
    }
    class Item
    {
        public string label;
        public Sprite icon;
        public bool equipped;
        public System.Action<Vector2> click;
    }
    RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)obj.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(w, h);
        if (parent == root) generated.Add(obj);
        return rect;
    }
    Image Background(RectTransform rect, Color color)
    {
        var image = rect.gameObject.AddComponent<Image>(); image.color = color; return image;
    }
    Text Label(Transform parent, string value, float x, float y, float w, float h, int size = 14)
    {
        var text = Rect(value, parent, x, y, w, h).gameObject.AddComponent<Text>();
        text.font = font; text.fontSize = size; text.color = Color.white;
        text.text = value; text.raycastTarget = false; text.alignment = TextAnchor.MiddleLeft;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }
    Button Button(Transform parent, string name, float x, float y, float w, float h, System.Action action)
    {
        var rect = Rect(name, parent, x, y, w, h);
        var image = Background(rect, new Color(.12f, .2f, .29f));
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var label = Label(rect, name, 4, 0, w - 8, h); label.alignment = TextAnchor.MiddleCenter;
        button.onClick.AddListener(() => action()); return button;
    }
    void BuildStats()
    {
        var area = Rect("属性列表", root, 0, 0, 250, 310);
        Background(area, panelColor);
        var content = Scroll(area, 250, 310, false);
        content.sizeDelta = new Vector2(250, 420);
        Label(content, "角色属性", 12, 5, 225, 30, 18).color = gold;
        Label(content, $"生命 {hud.stats.HP:0.#}/{hud.stats.Value(RunStat.MaxHP):0.#}    护盾 {hud.stats.Shield:0.#}/{hud.stats.Capacity:0.#}", 12, 38, 230, 25, 12);
        for (int i = 0; i < 13; i++)
        {
            var row = Rect("属性" + i, content, 8, 70 + i * 25, 234, 25);
            if (i % 2 == 0) Background(row, new Color(1, 1, 1, .035f)).raycastTarget = false;
            string name = i == 8 ? "移动速度" : StatShardDefinition.Names[i];
            Label(row, name, 5, 0, 158, 25, 12);
            var value = Label(row, hud.stats.Value((RunStat)i).ToString("0.##"), 163, 0, 64, 25, 13);
            value.alignment = TextAnchor.MiddleRight;
        }
    }
    RectTransform Scroll(RectTransform area, float w, float h, bool horizontal)
    {
        var scroll = area.gameObject.AddComponent<ScrollRect>();
        var viewport = Rect("可视区", area, 0, 0, w, h);
        Background(viewport, new Color(1, 1, 1, .001f)); viewport.gameObject.AddComponent<RectMask2D>();
        var content = Rect("内容", viewport, 0, 0, w, h);
        scroll.viewport = viewport; scroll.content = content;
        scroll.horizontal = horizontal; scroll.vertical = !horizontal;
        scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 25;
        return content;
    }
    void BuildRow(string title, float y, List<Item> items)
    {
        float width = 8 * slotSize + 7 * slotGap;
        var row = Rect(title, root, 265, y, width + 36, 94);
        Background(row, panelColor);
        Label(row, title, 8, 0, 300, 28, 16);
        var track = Rect("横向栏", row, 0, 34, width, slotSize);
        var content = Scroll(track, width, slotSize, true);
        content.sizeDelta = new Vector2(Mathf.Max(width, items.Count * (slotSize + slotGap) - slotGap), slotSize);
        for (int i = 0; i < Mathf.Max(8, items.Count); i++)
        {
            int index = i;
            var item = i < items.Count ? items[i] : null;
            var button = Button(content, item == null ? "" : item.label, i * (slotSize + slotGap), 0, slotSize, slotSize,
                () => { if (item != null) item.click(new Vector2(265 + index * (slotSize + slotGap), y + 34)); });
            button.interactable = item != null;
            button.GetComponentInChildren<Text>().fontSize = 10;
            if (item != null && item.icon)
            {
                var icon = Rect("图标", button.transform, 8, 3, slotSize - 16, slotSize - 16).gameObject.AddComponent<Image>();
                icon.sprite = item.icon; icon.preserveAspect = true; icon.raycastTarget = false;
            }
            if (item != null && item.equipped)
            {
                var outline = button.gameObject.AddComponent<Outline>(); outline.effectColor = gold; outline.effectDistance = new Vector2(2, 2);
                Label(button.transform, "已装备", 0, slotSize - 15, slotSize, 15, 9).color = gold;
            }
        }
        var scroll = track.GetComponent<ScrollRect>();
        var next = Button(row, "▶", width + 4, 34, 28, slotSize, () =>
        {
            float overflow = content.rect.width - width;
            if (overflow <= 0) return;
            scroll.horizontalNormalizedPosition = scroll.horizontalNormalizedPosition >= .999f ? 0 : Mathf.Min(1, scroll.horizontalNormalizedPosition + 8 * (slotSize + slotGap) / overflow);
            scroll.StopMovement();
        });
        next.interactable = items.Count > 8;
        next.GetComponentInChildren<Text>().color = items.Count > 8 ? gold : new Color(.38f, .32f, .13f);
    }
    List<Item> WeaponItems()
    {
        var items = new List<Item>();
        foreach (var definition in hud.inventory.definitions)
        {
            if (!hud.inventory.Owns(definition.kind)) continue;
            var weapon = definition;
            items.Add(new Item { label = weapon.displayName, icon = weapon.icon, equipped = hud.inventory.Equipped == weapon.kind,
                click = position => WeaponMenu(weapon) });
        }
        return items;
    }
    List<Item> AbilityItems()
    {
        var items = new List<Item>();
        foreach (var pair in hud.progression.OwnedAbilities)
        {
            var card = pair.Key; int count = pair.Value;
            string description = card.description;
            if (card.kind == AbilityCardKind.Projectile)
                description = "每次获得使每个方向多发射1枚弹丸，最多3枚。同方向逐发发射，间隔0.1秒。不同方向的同一波同时发射。\n\n第一、第二、第三枚的伤害系数分别为100%、80%、60%。更换武器后保留。\n\n飞镖直接与旋转伤害应用弹丸系数。飞刀每次命中独立叠加流血。弓箭子箭继承主箭系数。";
            else if (card.kind == AbilityCardKind.Direction)
                description = "每次获得增加一个攻击方向，最多三个方向。\n\n一级保留中心方向，侧向在左右交替。二级同时向左、中央、右侧发射，默认总夹角60°。\n\n每个方向都使用当前弹丸数量，更换武器后保留。";
            items.Add(new Item { label = card.title + "\n×" + count, icon = card.icon,
                click = position => Detail(card.title, description + $"\n\n当前获得 {count} 次", 340) });
        }
        return items;
    }
    RectTransform Popup(string title, float width, float height)
    {
        if (popup) Destroy(popup.gameObject);
        popup = Rect("详情浮窗", root, (740 - width) / 2, (310 - height) / 2, width, height);
        Background(popup, panelColor);
        Label(popup, title, 16, 8, width - 80, 35, 19).color = gold;
        Button(popup, "关闭", width - 68, 8, 60, 30, () => Destroy(popup.gameObject));
        return popup;
    }
    void WeaponMenu(WeaponDefinition weapon)
    {
        var menu = Popup(weapon.displayName, 196, 80);
        Button(menu, "详情", 10, 44, 84, 28, () => WeaponDetail(weapon));
        Button(menu, "装备", 102, 44, 84, 28, () => { hud.inventory.Equip(weapon.kind); Show(true); });
    }
    void Detail(string title, string description, float width)
    {
        var panel = Popup(title, width, 350);
        var body = Rect("说明滚动", panel, 16, 53, width - 32, 280);
        var content = Scroll(body, width - 32, 280, false);
        var text = Label(content, description, 0, 0, width - 45, 280, 16);
        float height = Mathf.Max(280, text.preferredHeight + 24);
        text.rectTransform.sizeDelta = new Vector2(width - 45, height);
        text.alignment = TextAnchor.UpperLeft; content.sizeDelta = new Vector2(width - 32, height);
    }
    void WeaponDetail(WeaponDefinition weapon)
    {
        string[] descriptions;
        switch (weapon.kind)
        {
            case WeaponKind.Dart: descriptions = new[] { $"每 {weapon.interval:0.#} 秒投出飞镖，命中造成100%攻击力伤害。", "穿透最多三个目标，后续目标分别承受直击伤害的70%和40%。", "命中第三个目标或到达射程后旋转1秒，每0.25秒造成5%攻击力范围伤害。", "首次旋转延长至2秒，返程可穿过敌人，每个敌人最多命中一次，基础伤害为20%攻击力，再乘弹丸伤害系数。到达后在落点再次旋转2秒。" }; break;
            case WeaponKind.Knife: descriptions = new[] { $"每 {weapon.interval:0.#} 秒发射飞刀，直击造成50%攻击力伤害。流血持续4秒，最多3层，各层周期伤害75% / 50% / 25%。命中刷新所有层持续时间。", "流血上限提升至5层，各层完整周期伤害75% / 62.5% / 50% / 37.5% / 25%。", "满层后再命中，额外造成满层非暴击流血伤害的15%，并刷新流血。", "流血周期伤害可独立暴击，使用角色暴击率与暴击伤害。" }; break;
            default: descriptions = new[] { $"每 {weapon.interval:0.#} 秒射击。箭矢减速至最低50%初速，伤害随速度由150%降至75%攻击力。", "主箭命中敌人或障碍物分裂2支子箭。子箭恢复满速，基础伤害为主箭的75%，不再次分裂。", "主箭伤害提升为250%～125%攻击力，射程增加。", "分裂为5支子箭，优先分配附近不同目标。同轮对同一目标重复命中依次乘65%，最低10%。" }; break;
        }
        string body = "";
        for (int i = 0; i < descriptions.Length; i++) body += $"<color=#{ColorUtility.ToHtmlStringRGB(i <= hud.inventory.Level ? gold : Color.gray)}>+{i}  {(i <= hud.inventory.Level ? "已激活" : "未激活")}</color>\n{descriptions[i]}\n\n";
        Detail(weapon.displayName + " · 等级效果", body, 580);
    }
}
