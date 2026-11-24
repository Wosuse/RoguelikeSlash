using System;
using UnityEngine;

[Serializable]
public sealed class RingSlashFlashSettings
{
    [Min(.01f), InspectorName("刀光宽度（世界单位）")] public float width = 1;
    [Min(.01f), InspectorName("环扫时间（秒）")] public float sweepTime = .12f;
    [Min(.01f), InspectorName("淡出时间（秒）")] public float fadeTime = .1f;
    [InspectorName("刀刃亮色")] public Color core = new Color(.88f,.98f,1,1);
    [InspectorName("刀光边缘色")] public Color edge = new Color(.25f,.65f,1,.8f);
    [Min(0), InspectorName("外侧黑色描边宽度（世界单位）")] public float outlineWidth = .045f;
    [InspectorName("描边颜色")] public Color outlineColor = new Color(.025f,.035f,.06f,1);
    [InspectorName("材质（空则使用默认透明材质）")] public Material material;
}

// Presentation only: a tapered blade with crisp bands and a dark silhouette.
public sealed class RingSlashFlash : MonoBehaviour
{
    const int Segments = 128, Rows = 8;
    readonly Vector3[] vertices = new Vector3[(Segments+1)*Rows];
    readonly Color[] colors = new Color[(Segments+1)*Rows];
    ArenaPrototype owner;
    RingSlashFlashSettings settings;
    HeartBladeSettings heart;
    Mesh mesh;
    Material ownedMaterial;
    float radius, startAngle, age;

    public void Initialize(ArenaPrototype arena, RingSlashFlashSettings config, float attackRadius, Vector2 direction, HeartBladeSettings heartStyle=null)
    {
        owner=arena;settings=config;radius=attackRadius;heart=heartStyle;
        transform.position=(Vector3)arena.PlayerPosition+Vector3.back*.15f;
        startAngle=Mathf.Atan2(direction.y,direction.x);
        mesh=new Mesh{name="环斩刀刃"};mesh.MarkDynamic();
        gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=gameObject.AddComponent<MeshRenderer>();renderer.sortingOrder=12;
        if(settings.material)renderer.sharedMaterial=settings.material;
        else{ownedMaterial=new Material(Shader.Find("Sprites/Default"));renderer.sharedMaterial=ownedMaterial;}
        var triangles=new int[Segments*(Rows-1)*6];int index=0;
        for(int i=0;i<Segments;i++)for(int row=0;row<Rows-1;row++)
        {
            int a=i*Rows+row,b=a+Rows;
            triangles[index++]=a;triangles[index++]=b;triangles[index++]=a+1;
            triangles[index++]=a+1;triangles[index++]=b;triangles[index++]=b+1;
        }
        mesh.vertices=vertices;mesh.triangles=triangles;Advance(0);
    }

    void Update()
    {
        if(!owner||!owner.Alive){Destroy(gameObject);return;}
        if(!owner.v06Skills.clock.Paused)Advance(Time.deltaTime);
    }

    public void Advance(float dt)
    {
        age+=Mathf.Max(0,dt);
        float sweep=Mathf.Clamp01(age/Mathf.Max(.01f,settings.sweepTime));
        float fade=1-Mathf.Clamp01((age-settings.sweepTime)/Mathf.Max(.01f,settings.fadeTime));
        for(int i=0;i<=Segments;i++)
        {
            float t=i/(float)Segments,angle=startAngle-t*sweep*Mathf.PI*2;
            // The leading tip narrows to a point; the completed ring closes without a gap.
            float taper=sweep<1?Mathf.Clamp01((1-t)*12):1;
            float width=Mathf.Min(radius,settings.width)*taper*(.8f+.2f*Mathf.Sin(t*Mathf.PI*6));
            float border=Mathf.Min(width*.2f,Mathf.Max(0,settings.outlineWidth)*taper);
            for(int row=0;row<Rows;row++)
            {
                // Duplicate boundaries keep the outline and white edge crisp rather than blurred.
                float offset=row==0?0:row<=2?border:row<=4?width*.73f:row<=6?width-border:width;
                float r=radius-width+offset;int n=i*Rows+row;
                vertices[n]=new Vector3(Mathf.Cos(angle)*r,Mathf.Sin(angle)*r,0);
                Color color=row>=6?settings.outlineColor:row<=3?(heart!=null?heart.dark:settings.edge):(heart!=null?heart.red:settings.core);
                color.a*=fade;
                colors[n]=color;
            }
        }
        mesh.vertices=vertices;mesh.colors=colors;mesh.RecalculateBounds();
        if(fade<=0)Destroy(gameObject);
    }

    void OnDestroy(){if(mesh)Destroy(mesh);if(ownedMaterial)Destroy(ownedMaterial);}
}
