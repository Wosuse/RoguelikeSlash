using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public sealed class ArenaHud : MonoBehaviour
{
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
    [SerializeField, InspectorName("生命颜色")] Color healthColor = new Color(0.35f, 0.94f, 0.69f);
    readonly List<Text> warnings = new List<Text>();
    Vector2 joystickHome;
    bool joystickMoved;
    public float Scale => Mathf.Max(0.01f, Mathf.Min(Screen.height / 450f, Screen.safeArea.width / 580f));

    void Update(){if(!Application.isPlaying&&safeArea)Layout();}

    public void Layout()
    {
        var canvas = GetComponent<Canvas>();
        if (Screen.width <= 0 || Screen.height <= 0) return;
        canvas.scaleFactor = Scale;
        Rect rect = Screen.safeArea;
        safeArea.anchorMin = new Vector2(rect.xMin / Screen.width, rect.yMin / Screen.height);
        safeArea.anchorMax = new Vector2(rect.xMax / Screen.width, rect.yMax / Screen.height);
    }

    public void PositionAtScreen(RectTransform target, Vector2 point)
    {
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)target.parent, point, null, out Vector3 world))
            target.position = world;
    }

    public void SetHealth(float health, float maxHealth)
    {
        float fraction = maxHealth > 0 ? Mathf.Clamp01(health / maxHealth) : 0;
        if (hpLabel && hpLabel.enabled) hpLabel.text = $"生命 {health:0.#} / {maxHealth:0.#}";
        if (hpFill.sprite && hpFill.type == Image.Type.Filled)
            hpFill.fillAmount = fraction;
        else
        {
            // A sprite-less Image draws a quad even in Filled mode.
            hpFill.type = Image.Type.Simple;
            var scale = hpFill.rectTransform.localScale;
            scale.x = fraction;
            hpFill.rectTransform.localScale = scale;
        }
        hpFill.color = fraction <= .25f ? new Color(1f, .38f, .39f) : healthColor;
    }

    public void SetScore(string time, int kills)
    {
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
