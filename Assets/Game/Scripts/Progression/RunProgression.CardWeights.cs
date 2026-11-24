using UnityEngine;

public sealed partial class RunProgression
{
    [Header("卡池：前置与构筑关系")]
    [Range(.01f,1), InspectorName("缺少前置时权重倍率")]
    public float missingPrerequisiteWeight=.2f;
    [Range(.01f,1), InspectorName("构筑冲突时权重倍率")]
    public float conflictingBuildWeight=.2f;

    public float CardOfferWeight(V06Card card,V06Attack attack)
    {
        if(!card||!attack)return 0;
        float multiplier=1;
        if(MissingPrerequisite(card.kind,attack))multiplier=Mathf.Min(multiplier,Mathf.Clamp(missingPrerequisiteWeight,.01f,1));
        if(Conflicts(card.kind,attack))multiplier=Mathf.Min(multiplier,Mathf.Clamp(conflictingBuildWeight,.01f,1));
        return Mathf.Max(0,card.weight)*multiplier;
    }

    static bool Own(V06Attack attack,V06CardKind kind)=>attack.Level(kind)>0;
    static bool MissingPrerequisite(V06CardKind kind,V06Attack attack)
    {
        switch(kind)
        {
            case V06CardKind.剑息:case V06CardKind.剑气:case V06CardKind.浪客:case V06CardKind.快攻势:
                return !Own(attack,V06CardKind.月牙)&&!Own(attack,V06CardKind.涟漪);
            case V06CardKind.雷劈:return !Own(attack,V06CardKind.引雷);
            case V06CardKind.炎:return !Own(attack,V06CardKind.火羽)&&!Own(attack,V06CardKind.炽刃);
            case V06CardKind.浪:return !Own(attack,V06CardKind.水涌)&&!Own(attack,V06CardKind.珠帘);
            default:return false;
        }
    }

    static bool NormalCard(V06CardKind kind)
    {
        switch(kind)
        {
            case V06CardKind.连击弹:case V06CardKind.穿透之箭:case V06CardKind.分裂:case V06CardKind.锈刃:
            case V06CardKind.隼之眼:case V06CardKind.射手天赋:case V06CardKind.狙击天赋:case V06CardKind.灵巧射手:
            case V06CardKind.火羽:case V06CardKind.水涌:case V06CardKind.引雷:case V06CardKind.雷劈:
            case V06CardKind.自信走位:case V06CardKind.纯粹射手:case V06CardKind.烈射手:case V06CardKind.狂射手:
            case V06CardKind.踢踏舞:case V06CardKind.嗡嗡攻势:case V06CardKind.会心:case V06CardKind.冰寒:return true;
            default:return false;
        }
    }

    static bool SlashCard(V06CardKind kind)
    {
        switch(kind)
        {
            case V06CardKind.力劈:case V06CardKind.战士天赋:case V06CardKind.剑客天赋:case V06CardKind.剑客之道:
            case V06CardKind.亮剑:case V06CardKind.月牙:case V06CardKind.涟漪:case V06CardKind.剑息:
            case V06CardKind.剑气:case V06CardKind.剑心:case V06CardKind.浪客:case V06CardKind.快攻势:
            case V06CardKind.血袭:case V06CardKind.血狂:case V06CardKind.临危亢奋:case V06CardKind.淬毒之刃:
            case V06CardKind.蓄势:case V06CardKind.炽刃:case V06CardKind.珠帘:return true;
            default:return false;
        }
    }

    static bool DodgeCard(V06CardKind kind)=>kind==V06CardKind.写意||kind==V06CardKind.离别赠礼||kind==V06CardKind.快攻势;
    static bool Conflicts(V06CardKind kind,V06Attack attack)
    {
        bool normalOff=Own(attack,V06CardKind.亮剑);
        bool slashOff=Own(attack,V06CardKind.自信走位)||Own(attack,V06CardKind.烈射手);
        bool dodgeOff=Own(attack,V06CardKind.自信走位)||Own(attack,V06CardKind.狂射手);
        if(normalOff&&NormalCard(kind)||slashOff&&SlashCard(kind)||dodgeOff&&DodgeCard(kind))return true;
        // Also lower the cards that would disable a branch the player has already built.
        foreach(var owned in attack.cards)
        {
            if(!owned||!Own(attack,owned.kind))continue;
            if(kind==V06CardKind.亮剑&&NormalCard(owned.kind))return true;
            if((kind==V06CardKind.自信走位||kind==V06CardKind.烈射手)&&SlashCard(owned.kind))return true;
            if((kind==V06CardKind.自信走位||kind==V06CardKind.狂射手)&&DodgeCard(owned.kind))return true;
        }
        return false;
    }

    float OfferWeight(AbilityCardDefinition card,V06Attack attack)=>card.v06?CardOfferWeight(card.v06,attack):1;
}
