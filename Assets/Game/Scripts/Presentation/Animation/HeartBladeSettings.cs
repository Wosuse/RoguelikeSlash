using System;
using UnityEngine;

[Serializable]
public sealed class HeartBladeSettings
{
    [InspectorName("暗部颜色")] public Color dark=new Color(.09f,.015f,.025f,1);
    [InspectorName("红色刃口")] public Color red=new Color(1,.08f,.12f,1);
    [InspectorName("拖尾颜色")] public Color trailColor=new Color(1,.04f,.08f,.8f);
    [Min(.01f),InspectorName("涟漪拖尾宽度")] public float trailWidth=.2f;
    [Min(.01f),InspectorName("月牙残影时长（秒）")] public float trailTime=.2f;
    [Range(1,8),InspectorName("月牙残影数量")] public int moonTrailCopies=4;
    [Range(0,1),InspectorName("月牙残影不透明度")] public float moonTrailOpacity=.3f;
    [Min(0),InspectorName("涟漪拖尾间距")] public float ringTrailDistance=.6f;
}
