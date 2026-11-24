using System.Collections.Generic;
using UnityEngine;
public sealed partial class CardLab
{
 enum CardGroup{全部,普攻,斩与剑气,闪与机动,元素,生存}
 CardGroup cardGroup;
 static CardGroup GroupFor(V06CardKind kind)
 {
  switch(kind){
   case V06CardKind.浪:case V06CardKind.炎:case V06CardKind.火羽:case V06CardKind.水涌:case V06CardKind.引雷:case V06CardKind.雷劈:case V06CardKind.冰寒:case V06CardKind.炽刃:case V06CardKind.珠帘:return CardGroup.元素;
   case V06CardKind.写意:case V06CardKind.离别赠礼:return CardGroup.闪与机动;
   case V06CardKind.力劈:case V06CardKind.战士天赋:case V06CardKind.剑客天赋:case V06CardKind.剑客之道:case V06CardKind.亮剑:case V06CardKind.月牙:case V06CardKind.涟漪:case V06CardKind.剑息:case V06CardKind.快攻势:case V06CardKind.剑气:case V06CardKind.剑心:case V06CardKind.浪客:case V06CardKind.淬毒之刃:case V06CardKind.蓄势:return CardGroup.斩与剑气;
   case V06CardKind.血袭:case V06CardKind.血狂:case V06CardKind.临危亢奋:case V06CardKind.不屈:case V06CardKind.背水:return CardGroup.生存;
   default:return CardGroup.普攻;
  }
 }
 readonly HashSet<V06CardKind> expandedCardDetails=new HashSet<V06CardKind>();
 public bool CardDetailsExpanded(V06CardKind kind)=>expandedCardDetails.Contains(kind);
 public void ToggleCardDetails(V06Card card){if(!card)return;if(!expandedCardDetails.Add(card.kind))expandedCardDetails.Remove(card.kind);
#if UNITY_EDITOR
 UnityEditor.Selection.activeGameObject=card.gameObject;
 UnityEditor.EditorGUIUtility.PingObject(card.gameObject);
#endif
 }
 public bool RemoveCard(V06CardKind kind)=>arena&&arena.v06Attack&&arena.v06Attack.Remove(kind);
 void DrawCardEntry(V06Card card){GUILayout.BeginHorizontal();var label=(CardDetailsExpanded(card.kind)?"▼ ":"▶ ")+card.name+"  "+arena.v06Attack.Level(card.kind)+"/"+card.maxPicks;
 var header=new GUIStyle(labButton){alignment=TextAnchor.MiddleLeft};if(GUILayout.Button(new GUIContent(label,"展开说明并在Inspector中选中配置"),header,GUILayout.ExpandWidth(true)))ToggleCardDetails(card);
 GUI.enabled=arena.v06Attack.Level(card.kind)>0;if(GUILayout.Button("−1",labButton,GUILayout.Width(38)))RemoveCard(card.kind);GUI.enabled=arena.v06Attack.Available(card);if(GUILayout.Button("+1",labButton,GUILayout.Width(38)))Grant(card.kind);GUI.enabled=true;GUILayout.EndHorizontal();
 if(CardDetailsExpanded(card.kind)){var detail=new GUIStyle(labLabel){wordWrap=true,fixedHeight=0,alignment=TextAnchor.UpperLeft,fontSize=16,padding=new RectOffset(12,8,6,8)};GUILayout.Label(card.Description(),detail);}
 }
}
