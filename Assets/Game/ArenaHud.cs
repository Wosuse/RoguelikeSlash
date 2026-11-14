using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public sealed class ArenaHud : MonoBehaviour
{
    enum Preview { 默认, 冷却, 长按, 组合, 结算 }
    [SerializeField, InspectorName("编辑预览")] Preview preview;
    [SerializeField, Min(0.01f), InspectorName("组合入场时长（秒）")] float appearDuration = 0.16f;
    [InspectorName("安全区域")] public RectTransform safeArea;
    [InspectorName("摇杆底座")] public RectTransform joystick;
    [InspectorName("摇杆中心")] public RectTransform knob;
    [InspectorName("战斗提示层")] public RectTransform warningLayer;
    [InspectorName("生命数值")] public Text hpLabel;
    [InspectorName("时间")] public Text timeLabel;
    [InspectorName("击杀数")] public Text killsLabel;
    [InspectorName("结算统计")] public Text resultStats;
    [InspectorName("闪避提示")] public Text dodgeNotice;
    [InspectorName("近战预警模板")] public Text warningTemplate;
    [InspectorName("血条填充")] public Image hpFill;
    [InspectorName("结算面板")] public GameObject resultPanel;
    [InspectorName("重试按钮")] public Button retry;
    [InspectorName("基础技能按钮")] public ArenaSkillButton[] slots;
    [InspectorName("组合技能按钮")] public ArenaSkillButton[] combinations;
    [InspectorName("取消组合")] public ArenaSkillButton cancel;
    [InspectorName("拖拽显示")] public ArenaSkillButton drag;
    [SerializeField, InspectorName("生命颜色")] Color healthColor = new Color(0.35f, 0.94f, 0.69f);
    readonly List<Text> warnings = new List<Text>();
    Vector2 joystickHome;
    bool joystickMoved;
    public float Scale => Mathf.Max(0.01f, Mathf.Min(Screen.height / 450f, Screen.safeArea.width / 580f));
    public float AppearDuration => Mathf.Max(0.01f, appearDuration);

    void Update()
    {
        if (Application.isPlaying || !safeArea || slots == null || combinations == null || slots.Length != 3 || combinations.Length != 3) return;
        for (int i = 0; i < 3; i++)
        {
            slots[i].gameObject.SetActive(preview != Preview.组合 || i == 1);
            slots[i].Show(i == 0 ? new Color(.37f,.73f,1f) : i == 1 ? new Color(1f,.69f,.36f) : new Color(.88f,.58f,1f),
                null, preview == Preview.冷却 ? 3 : 0, 8, preview == Preview.长按 ? .65f : -1);
            combinations[i].gameObject.SetActive(preview == Preview.组合 && i == 2);
        }
        cancel.gameObject.SetActive(preview == Preview.组合);
        resultPanel.SetActive(preview == Preview.结算);
    }

    public void Layout()
    {
        var canvas = GetComponent<Canvas>();
        if (Screen.width <= 0 || Screen.height <= 0) return;
        canvas.scaleFactor = Scale;
        Rect rect = Screen.safeArea;
        safeArea.anchorMin = new Vector2(rect.xMin / Screen.width, rect.yMin / Screen.height);
        safeArea.anchorMax = new Vector2(rect.xMax / Screen.width, rect.yMax / Screen.height);
    }

    public Rect SlotRect(int index)
    {
        var corners = new Vector3[4];
        slots[index].Rect.GetWorldCorners(corners);
        Vector2 a = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
        Vector2 b = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
        return new Rect(a.x / Scale, (Screen.height - b.y) / Scale, (b.x - a.x) / Scale, (b.y - a.y) / Scale);
    }

    public void AlignToSlot(ArenaSkillButton button, int index, float scale)
    {
        RectTransform slot = slots[index].Rect;
        button.Rect.position = slot.TransformPoint(slot.rect.center);
        button.Rect.sizeDelta = slot.rect.size;
        button.Rect.localScale = Vector3.one * scale;
    }

    public void PositionAtScreen(RectTransform target, Vector2 point)
    {
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)target.parent, point, null, out Vector3 world))
            target.position = world;
    }

    public void SetScore(int health, string time, int kills)
    {
        hpLabel.text = $"生命  {health} / 100";
        hpFill.fillAmount = health / 100f;
        hpFill.color = health <= 25 ? new Color(1f, .38f, .39f) : healthColor;
        timeLabel.text = "时间\n" + time;
        killsLabel.text = "击杀\n" + kills;
    }

    public void SetResults(bool show, string time, int kills)
    {
        resultPanel.SetActive(show);
        if (show) resultStats.text = $"存活时间  {time}     击杀  {kills}";
    }

    public void SetJoystick(bool visible, bool touched, Vector2 point, Vector2 direction)
    {
        joystick.gameObject.SetActive(visible);
        if (touched)
        {
            if (!joystickMoved) joystickHome = joystick.anchoredPosition;
            joystickMoved = true;
            PositionAtScreen(joystick, point);
        }
        else if (joystickMoved)
        {
            joystick.anchoredPosition = joystickHome;
            joystickMoved = false;
        }
        knob.anchoredPosition = direction * 32;
    }

    public void SetWarning(int index, Vector2 point)
    {
        if (index == warnings.Count) warnings.Add(Instantiate(warningTemplate, warningLayer));
        var warning = warnings[index];
        warning.gameObject.SetActive(true);
        PositionAtScreen(warning.rectTransform, point);
    }

    public void HideWarningsFrom(int index)
    {
        for (; index < warnings.Count; index++) warnings[index].gameObject.SetActive(false);
    }

    public void SetDodgeNotice(float left, Vector2 point, Color color)
    {
        dodgeNotice.gameObject.SetActive(left > 0);
        if (left <= 0) return;
        PositionAtScreen(dodgeNotice.rectTransform, point + Vector2.up * (35f + (1.15f - left) * 20f) * Scale);
        color.a *= Mathf.Clamp01(left * 3f);
        dodgeNotice.color = color;
    }
}
