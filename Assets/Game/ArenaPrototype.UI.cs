using UnityEngine;

public sealed partial class ArenaPrototype
{
    [Header("Interface")]
    [SerializeField] Font interfaceFont;
    [SerializeField] Color panelColor = new Color(0.055f, 0.08f, 0.12f, 0.94f);
    [SerializeField] Color healthColor = new Color(0.35f, 0.94f, 0.69f);
    [SerializeField] Color guardColor = new Color(0.37f, 0.73f, 1f);
    [SerializeField] Color dashColor = new Color(1f, 0.69f, 0.36f);
    [SerializeField] Color slashColor = new Color(0.88f, 0.58f, 1f);
    [SerializeField] Texture2D guardIcon, dashIcon, slashIcon;

    GUIStyle caption, value, title, retryHit;
    static readonly Color Muted = new Color(0.61f, 0.70f, 0.79f);
    static readonly Color Edge = new Color(0.24f, 0.31f, 0.40f, 0.9f);
    float UiScale => Mathf.Min(Screen.height / 450f, Screen.safeArea.width / 580f);
    float UiWidth => Screen.width / UiScale;
    float UiHeight => Screen.height / UiScale;
    float SafeLeft => Screen.safeArea.xMin / UiScale;
    float SafeRight => Screen.safeArea.xMax / UiScale;
    float SafeTop => (Screen.height - Screen.safeArea.yMax) / UiScale;
    float SafeBottom => (Screen.height - Screen.safeArea.yMin) / UiScale;

    Rect SkillRect(int index)
    {
        float offset = index == 0 ? 111 : index == 1 ? 158 : 65;
        return new Rect(SafeRight - offset - 34, SafeBottom - (index == 0 ? 165 : 88), 68, 68);
    }

    void OnGUI()
    {
        if (UiScale <= 0) return;
        EnsureUiStyles();
        var previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(UiScale, UiScale, 1));
        float center = (SafeLeft + SafeRight) * 0.5f;
        int seconds = Mathf.FloorToInt(elapsed);
        string time = $"{seconds / 60:00}:{seconds % 60:00}";
        DrawHud(center, time);

        if (hp > 0)
        {
            DrawJoystick();
            for (int i = 0; i < readyAt.Length; i++) DrawSkill(i);
        }
        else DrawResults(center, time);
        GUI.matrix = previousMatrix;
    }

    void EnsureUiStyles()
    {
        if (caption != null) return;
        if (!interfaceFont) interfaceFont = Resources.Load<Font>("ArenaChinese");
        caption = MakeLabel(11, FontStyle.Bold, Muted);
        value = MakeLabel(19, FontStyle.Bold, Color.white);
        title = MakeLabel(30, FontStyle.Bold, Color.white);
        retryHit = new GUIStyle();
    }

    GUIStyle MakeLabel(int size, FontStyle weight, Color color)
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = size,
            fontStyle = weight,
            alignment = TextAnchor.MiddleCenter
        };
        if (interfaceFont) style.font = interfaceFont;
        style.normal.textColor = color;
        return style;
    }

    void DrawHud(float center, string time)
    {
        float y = SafeTop + 12;
        float left = SafeLeft + 16;
        Panel(new Rect(left, y, 164, 60), panelColor, 12);
        Panel(new Rect(center - 90, y, 180, 60), panelColor, 12);
        GUI.Label(new Rect(left + 8, y + 4, 148, 21), $"生命  {hp} / 100", value);
        Panel(new Rect(left + 21, y + 34, 122, 8), Edge, 4);
        if (hp > 0)
            Panel(new Rect(left + 21, y + 34, 122 * hp / 100f, 8),
                hp <= 25 ? new Color(1f, 0.38f, 0.39f) : healthColor, 4);
        DrawStat(center - 44, y, "时间", time);
        DrawStat(center + 44, y, "击杀", kills.ToString());
    }

    void DrawStat(float center, float y, string name, string number)
    {
        GUI.Label(new Rect(center - 40, y + 7, 80, 17), name, caption);
        GUI.Label(new Rect(center - 40, y + 24, 80, 28), number, value);
    }

    void DrawJoystick()
    {
        var center = finger < 0 ? new Vector2(SafeLeft + 84, SafeBottom - 80)
            : new Vector2(touchOrigin.x, Screen.height - touchOrigin.y) / UiScale;
        Panel(new Rect(center.x - 53, center.y - 53, 106, 106), Edge, 53);
        Panel(new Rect(center.x - 50, center.y - 50, 100, 100), panelColor, 50);
        var knob = center + new Vector2(stick.x, -stick.y) * 32;
        Panel(new Rect(knob.x - 19, knob.y - 19, 38, 38), finger < 0 ? Edge : healthColor, 19);
    }

    void DrawSkill(int index)
    {
        Rect rect = SkillRect(index);
        float remaining = Mathf.Max(0, readyAt[index] - elapsed);
        bool active = cast != Cast.Idle && (int)skill == index;
        Color accent = index == 0 ? guardColor : index == 1 ? dashColor : slashColor;
        var icon = index == 0 ? guardIcon : index == 1 ? dashIcon : slashIcon;
        Panel(rect, remaining > 0 ? Edge : accent, 34);
        var inner = new Rect(rect.x + 3, rect.y + 3, 62, 62);
        Panel(inner, active ? Color.Lerp(panelColor, accent, 0.35f) : panelColor, 31);

        if (remaining > 0)
        {
            GUI.Label(new Rect(rect.x, rect.y + 8, 68, 34), Mathf.CeilToInt(remaining) + "秒", value);
            float progress = 1 - Mathf.Clamp01(remaining / Mathf.Max(0.01f, skillCooldown));
            Panel(new Rect(rect.x + 17, rect.y + 48, 34, 4), Edge, 2);
            if (progress > 0) Panel(new Rect(rect.x + 17, rect.y + 48, 34 * progress, 4), accent, 2);
            return;
        }

        if (icon) GUI.DrawTexture(new Rect(rect.x + 20, rect.y + 12, 28, 28), icon, ScaleMode.ScaleToFit);
        else GUI.Label(new Rect(rect.x, rect.y + 7, 68, 33), ((char)('A' + index)).ToString(), value);
        string name = index == 0 ? "格挡" : index == 1 ? "冲刺" : "环形斩";
        GUI.Label(new Rect(rect.x, rect.y + 40, 68, 18), name, caption);
    }

    void DrawResults(float center, string time)
    {
        Panel(new Rect(0, 0, UiWidth, UiHeight), new Color(0.025f, 0.04f, 0.065f, 0.75f), 0);
        float y = (SafeTop + SafeBottom) * 0.5f - 104;
        Panel(new Rect(center - 165, y, 330, 208), panelColor, 18);
        GUI.Label(new Rect(center - 145, y + 16, 290, 45), "战斗结束", title);
        DrawStat(center - 63, y + 64, "存活时间", time);
        DrawStat(center + 63, y + 64, "击杀", kills.ToString());
        var button = new Rect(center - 119, y + 142, 238, 46);
        Panel(button, new Color(0.17f, 0.38f, 0.34f), 10);
        if (GUI.Button(button, GUIContent.none, retryHit)) Retry();
        GUI.Label(button, "再来一局", value);
    }

    static void Panel(Rect rect, Color color, float radius)
    {
        if (Event.current.type != EventType.Repaint || rect.width <= 0 || rect.height <= 0) return;
        radius = Mathf.Min(radius, Mathf.Min(rect.width, rect.height) * 0.5f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, color, 0, radius);
    }
}

