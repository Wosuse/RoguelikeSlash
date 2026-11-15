using UnityEngine;

public enum WeaponKind { Dart, Knife, Bow }
[CreateAssetMenu(menuName="割草/自动武器")]
public sealed class WeaponDefinition : ScriptableObject
{
    [InspectorName("种类")] public WeaponKind kind;
    [InspectorName("中文名称")] public string displayName;
    [Header("攻击与飞行")]
    [Min(.05f), InspectorName("基础间隔")] public float interval = 2.5f;
    [Min(.1f), InspectorName("飞行速度")] public float speed = 10;
    [Min(.1f), InspectorName("射程")] public float range = 9;
    [Header("飞镖")]
    [Min(.1f), InspectorName("旋转范围")] public float spinRadius = 1.4f;
    [Min(.01f), InspectorName("旋转伤害间隔")] public float spinTick = .25f;
    [Header("飞刀")]
    [Min(.01f), InspectorName("流血结算间隔")] public float bleedTick = .5f;
    [Header("弓箭")]
    [Min(0), InspectorName("每秒减速")] public float deceleration = 5;
    [Min(0), InspectorName("二级额外射程")] public float upgradedRange = 4;
    [Min(.1f), InspectorName("子箭射程")] public float childRange = 7;
    [Min(.1f), InspectorName("分裂索敌半径")] public float searchRadius = 6;
    [InspectorName("障碍物层（仅真实墙体）")] public LayerMask obstacleMask;
    [Header("外观")]
    [InspectorName("图标")] public Sprite icon;
    [InspectorName("弹丸图像")] public Texture2D texture;
    [InspectorName("弹丸材质")] public Material material;
    [InspectorName("颜色")] public Color color = Color.cyan;
    [Min(.02f), InspectorName("弹丸尺寸")] public float size = .3f;
    [Header("音效")]
    [InspectorName("发射音效")] public AudioClip fireSound;
    [InspectorName("命中音效")] public AudioClip hitSound;
    [Range(0,1), InspectorName("音量")] public float volume = .6f;
}
