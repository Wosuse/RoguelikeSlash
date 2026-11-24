using UnityEngine;
public sealed class PlayerShieldVisual:MonoBehaviour
{
    public ArenaPrototype arena;
    public RunStats stats;
    [InspectorName("护盾材质")] public Material material;
    [InspectorName("护盾颜色")] public Color color=new Color(.4f,.75f,1,.7f);
    [Min(.01f),InspectorName("线宽")] public float width=.06f;
    [Min(.1f),InspectorName("半径")] public float radius=.65f;
    [InspectorName("生成音效")] public AudioClip generatedSound;
    LineRenderer ring;
    float previousShield;
    void LateUpdate()
    {
        if(!ring){ring=gameObject.AddComponent<LineRenderer>();ring.loop=true;ring.positionCount=48;ring.useWorldSpace=true;}
        ring.enabled=stats.Shield>0&&arena.Alive;
        if(!ring.enabled){previousShield=stats.Shield;return;}
        if(stats.Shield>previousShield&&generatedSound)AudioSource.PlayClipAtPoint(generatedSound,Camera.main.transform.position,.5f);
        previousShield=stats.Shield;ring.sharedMaterial=material;ring.startColor=ring.endColor=color;ring.startWidth=ring.endWidth=width;
        for(int i=0;i<48;i++){float a=i*Mathf.PI/24;Vector2 p=arena.PlayerPosition+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;ring.SetPosition(i,new Vector3(p.x,p.y,-.3f));}
    }
}
