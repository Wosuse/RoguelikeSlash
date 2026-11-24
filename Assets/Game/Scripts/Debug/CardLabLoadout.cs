using UnityEngine;
[CreateAssetMenu(menuName="割草/测试/卡牌组合")]
public sealed class CardLabLoadout:ScriptableObject
{
 [System.Serializable]public struct Entry {public V06CardKind card;[Min(0)]public int level;}
 [TextArea]public string description;
 public Entry[] cards=new Entry[0];
}
