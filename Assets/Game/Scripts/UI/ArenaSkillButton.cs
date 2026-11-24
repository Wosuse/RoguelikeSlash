using UnityEngine;
using UnityEngine.UI;

public sealed class ArenaSkillButton : MonoBehaviour
{
    [SerializeField, InspectorName("边框")] public Image border;
    [SerializeField, InspectorName("图标")] public RawImage icon;
    [SerializeField, InspectorName("主文字")] public Text title;
    [SerializeField, InspectorName("技能说明")] public Text caption;
    [SerializeField, InspectorName("冷却数字")] public Text cooldown;
    [SerializeField, InspectorName("冷却进度")] public Image progress;
    [SerializeField, InspectorName("持续时间环")] public Image duration;
    [SerializeField, InspectorName("反击次数")] public Text charges;
    [SerializeField, InspectorName("禁用X标记")] public Text disabledMark;
    internal RectTransform Rect => (RectTransform)transform;

    internal void ShowDisabled(bool disabled)
    {
        if(disabledMark){disabledMark.gameObject.SetActive(disabled);if(disabled)disabledMark.transform.SetAsLastSibling();}
        if(!disabled)return;
        border.color=new Color(.25f,.28f,.32f);
        cooldown.enabled=progress.enabled=duration.enabled=false;
    }

    internal void Show(Color accent, Texture2D texture, float remaining, float total, float remainingDuration)
    {
        bool cooling = remaining > 0;
        border.color = cooling ? new Color(0.24f, 0.31f, 0.4f) : accent;
        icon.texture = texture;
        icon.enabled = !cooling && texture;
        title.enabled = !cooling && !texture;
        if (caption) caption.enabled = !cooling;
        cooldown.enabled = progress.enabled = cooling;
        if (cooling)
        {
            cooldown.text = Mathf.CeilToInt(remaining) + "秒";
            progress.fillAmount = 1f - Mathf.Clamp01(remaining / Mathf.Max(0.01f, total));
            progress.color = accent;
        }
        duration.enabled = remainingDuration >= 0f;
        duration.fillAmount = Mathf.Clamp01(remainingDuration);
    }

}
