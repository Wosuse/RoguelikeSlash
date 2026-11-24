using UnityEngine;
using UnityEngine.UI;

public sealed class RunHud : MonoBehaviour
{
    [Header("系统")]
    public ArenaPrototype arena;
    public RunStats stats;
    public RunClock clock;
    public RunProgression progression;
    public WeaponInventory inventory;
    [Header("场景界面引用")]
    public Text progressLabel, shieldLabel, weaponLabel, panelTitle, detailLabel;
    public Image xpFill, shieldFill;
    public Text coinsLabel, levelLabel;
    public GameObject modal, choices, detail;
    public Button inventoryButton, statsButton, closeButton;
    public Button[] optionButtons;
    public Text[] optionLabels;
    public Image[] optionIcons;
    [Header("品质颜色")]
    public Color silverColor = new Color(.8f,.85f,.9f);
    public Color goldColor = new Color(1,.8f,.25f);
    public Color coloredColor = new Color(.85f,.5f,1);
    [SerializeField, InspectorName("当前面板")] string panel;
    float acceptAfter;
    int lastStage = -1;
    AudioSource audioSource;
    CharacterPanel characterPanel;
    void Awake()
    {
        audioSource=gameObject.AddComponent<AudioSource>();
        var characterRoot = new GameObject("角色属性与内容栏", typeof(RectTransform));
        characterRoot.transform.SetParent(detail.transform.parent, false);
        characterPanel = characterRoot.AddComponent<CharacterPanel>();
        characterPanel.Initialize(this);
        // Disable only the text components: their objects also hold the bar fills.
        if (progressLabel) progressLabel.enabled = false;
        if (shieldLabel) shieldLabel.enabled = false;
        inventoryButton.onClick.AddListener(()=>Open("角色"));
        statsButton.onClick.AddListener(()=>Open("角色"));
        closeButton.onClick.AddListener(Close);
        AddShardOption();
        for(int i=0;i<optionButtons.Length;i++){int index=i;optionButtons[i].onClick.AddListener(()=>Choose(index));}
        progression.Changed+=Refresh;
        Refresh();
    }
    void AddShardOption()
    {
        int index = optionButtons.Length;
        if (index >= 4) return;
        var template = optionButtons[0];
        var button = Instantiate(template, template.transform.parent);
        button.name = "随机成长碎片";
        System.Array.Resize(ref optionButtons, index + 1);
        System.Array.Resize(ref optionLabels, index + 1);
        System.Array.Resize(ref optionIcons, index + 1);
        optionButtons[index] = button;
        optionLabels[index] = button.GetComponentInChildren<Text>(true);
        optionIcons[index] = button.transform.Find(optionIcons[0].name).GetComponent<Image>();
    }
    void OnDestroy(){if(progression)progression.Changed-=Refresh;}
    void Open(string mode)
    {
        if(!arena.Alive||progression.Stage!=RewardStage.None)return;
        panel=mode;clock.SetPause("查看面板",true);Refresh();
    }
    void Close(){panel="";clock.SetPause("查看面板",false);Refresh();}
    void LateUpdate()
    {
        if (levelLabel) levelLabel.text = $"等级：{progression.Level}";
        if (coinsLabel) coinsLabel.text = $"金币 {progression.Coins}";
        xpFill.fillAmount=(float)progression.XP/progression.NeededXP;
        xpFill.rectTransform.localScale=new Vector3(xpFill.fillAmount,1,1);
        shieldFill.fillAmount=stats.Capacity>0?stats.Shield/stats.Capacity:0;
        shieldFill.rectTransform.localScale=new Vector3(shieldFill.fillAmount,1,1);
        weaponLabel.gameObject.SetActive(!arena.v06Attack);
        weaponLabel.text=$"{inventory.Current.displayName} +{inventory.Level}   熟练 {inventory.Mastery}/6";
        inventoryButton.interactable=statsButton.interactable=arena.Alive&&progression.Stage==RewardStage.None;
        if(lastStage!=(int)progression.Stage)Refresh();
        if(panel=="角色")detailLabel.text=stats.Summary();
    }
    void Refresh()
    {
        lastStage=(int)progression.Stage;acceptAfter=Time.unscaledTime+.15f;
        bool reward=progression.Stage!=RewardStage.None;
        modal.SetActive(reward||panel!="");closeButton.gameObject.SetActive(!reward);
        choices.SetActive(reward);
        detail.SetActive(false);
        characterPanel.Show(panel == "角色" && !reward);
        for(int i=0;i<optionButtons.Length;i++){optionButtons[i].gameObject.SetActive(false);optionIcons[i].gameObject.SetActive(false);optionLabels[i].color=Color.white;}
        if(!reward)
        {
            panelTitle.text=arena.v06Attack?"角色 · 属性与能力":"角色 · 属性与武器";
            return;
        }
        switch(progression.Stage)
        {
            case RewardStage.Choices:
                int choiceCount = progression.Offers.Count + 1;
                panelTitle.text = choiceCount == 1 ? "升级奖励" : $"升级奖励 · {"一二三四"[choiceCount - 1]}选一";
                for(int i=0;i<progression.Offers.Count;i++)
                {
                    var card=progression.Offers[i];string note = card.kind == AbilityCardKind.Projectile ? "弹丸数量 +1" : card.kind == AbilityCardKind.Direction ? "攻击方向 +1" : card.kind == AbilityCardKind.Mastery ? "共享熟练 +1" : "获得武器";
                    if(card.kind==AbilityCardKind.Weapon&&inventory.Owns(card.weapon))note=$"重复武器 → 共享熟练 +{inventory.duplicatePoints}";
                    Option(i,card.title+"\n"+(card.v06?card.v06.ShortDescription(arena.v06Attack.Level(card.v06.kind)+1):note),card.icon);
                }
                Option(progression.Offers.Count, $"随机成长碎片 ×{progression.CurrentRewardShardCount}", null);
                break;
            case RewardStage.Shards:
                panelTitle.text=$"随机成长碎片 · {progression.ShardIndex+1}/{progression.ShardTotal}";
                var shard=progression.CurrentShard;
                if(shard){Option(0,shard.Label+"\n点击领取",shard.icon);optionLabels[0].color=shard.quality==ShardQuality.Silver?silverColor:shard.quality==ShardQuality.Gold?goldColor:coloredColor;}
                break;
        }
        LayoutOptions();
    }
    void LayoutOptions()
    {
        bool character = panel == "角色" && progression.Stage == RewardStage.None;
        detailLabel.rectTransform.anchoredPosition = new Vector2(-175, 0);
        detailLabel.rectTransform.sizeDelta = new Vector2(340, 310);
        if (character)
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                var rect = (RectTransform)optionButtons[i].transform;
                rect.anchoredPosition = new Vector2(190, 100 - i * 100);
                rect.sizeDelta = new Vector2(290, 86);
                optionLabels[i].rectTransform.sizeDelta = new Vector2(270, 76);
                optionLabels[i].rectTransform.anchoredPosition = Vector2.zero;
                optionLabels[i].fontSize = 17;
                optionIcons[i].gameObject.SetActive(false);
            }
            return;
        }
        int count=0;foreach(var button in optionButtons)if(button.gameObject.activeSelf)count++;
        float width = count == 4 ? 174 : 220;
        int slot = 0;
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (!optionButtons[i].gameObject.activeSelf) continue;
            var rect = (RectTransform)optionButtons[i].transform;
            rect.sizeDelta = new Vector2(width, 250);
            rect.anchoredPosition = new Vector2((slot++ - (count - 1) * .5f) * (width + 15), 0);
            var labelRect = optionLabels[i].rectTransform;
            labelRect.sizeDelta = new Vector2(width - 12, labelRect.sizeDelta.y);
            optionLabels[i].fontSize = count == 4 ? 17 : 19;
        }
    }
    void Option(int index,string label,Sprite icon)
    {
        optionButtons[index].gameObject.SetActive(true);optionButtons[index].interactable=true;
        optionLabels[index].text=label;optionIcons[index].sprite=icon;optionIcons[index].gameObject.SetActive(icon);
        optionLabels[index].rectTransform.anchoredPosition=new Vector2(0,icon?-25:0);
        optionLabels[index].rectTransform.sizeDelta=new Vector2(208,icon?170:238);
    }
    void Choose(int index)
    {
        if(Time.unscaledTime<acceptAfter)return;
        if(progression.Stage==RewardStage.Choices)
        {
            if (index == progression.Offers.Count)
            {
                progression.ChooseShards();
                return;
            }
            if (index < 0 || index >= progression.Offers.Count) return;
            var card=progression.Offers[index];if(card.sound)audioSource.PlayOneShot(card.sound);progression.SelectCard(index);
        }
        else if(progression.Stage==RewardStage.Shards)
        {
            var shard=progression.CurrentShard;if(shard&&shard.sound)audioSource.PlayOneShot(shard.sound);progression.ClaimShard(progression.ShardIndex);
        }
        else if(panel=="角色"){inventory.Equip((WeaponKind)index);Refresh();}
    }
}

