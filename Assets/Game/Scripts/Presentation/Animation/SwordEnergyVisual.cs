using UnityEngine;
using System.Collections.Generic;

// Presentation only. Damage, charge consumption and travel stay in V06Skills.
public sealed class SwordEnergyVisual : MonoBehaviour
{
    SpriteRenderer visual;
    SkillAnimationSet set;
    SkillAnimationClip clip;
    bool ring;
    float age;
    HeartBladeSettings heart;
    SpriteRenderer[] echoes;
    struct TrailSample {public float time;public Vector3 position;}
    readonly List<TrailSample> history=new List<TrailSample>();
    LineRenderer ringTrail;
    Material bladeMaterial,trailMaterial;

    public void Initialize(SkillAnimationSet configuration, bool isRing, HeartBladeSettings heartStyle=null)
    {
        set = configuration;
        ring = isRing;
        heart = heartStyle;
        clip = ring ? set.ring : set.crescent;
        visual = gameObject.AddComponent<SpriteRenderer>();
        visual.sortingOrder = 8;
        if(heart!=null)
        {
            var shader=Resources.Load<Shader>("BladeOutline");
            if(shader){bladeMaterial=new Material(shader);bladeMaterial.SetColor("_OutlineColor",heart.dark);bladeMaterial.SetFloat("_OutlinePixels",2);visual.sharedMaterial=bladeMaterial;}
            if(ring)
            {
                trailMaterial=new Material(Shader.Find("Sprites/Default"));
                ringTrail=gameObject.AddComponent<LineRenderer>();ringTrail.sharedMaterial=trailMaterial;
                ringTrail.useWorldSpace=true;ringTrail.loop=true;ringTrail.positionCount=64;
                ringTrail.widthMultiplier=heart.trailWidth;ringTrail.startColor=ringTrail.endColor=heart.trailColor;ringTrail.sortingOrder=5;
            }
            else
            {
                echoes=new SpriteRenderer[Mathf.Clamp(heart.moonTrailCopies,1,8)];
                for(int i=0;i<echoes.Length;i++)
                {
                    var echo=new GameObject("月牙红色残影"+(i+1));echo.transform.SetParent(transform,false);
                    echoes[i]=echo.AddComponent<SpriteRenderer>();echoes[i].sortingOrder=5;echoes[i].enabled=false;
                }
            }
        }
    }

    public void Draw(Vector2 origin, Vector2 direction, float distance, float dt)
    {
        age += dt;
        if (!visual || !clip.Valid) return;
        visual.sprite = clip.Sample(age, true);
        visual.color = heart!=null?heart.red:set.swordTint;
        transform.position = (ring ? origin : origin + direction * distance) + clip.offset;
        if(ringTrail)
        {
            float radius=Mathf.Max(0,distance-heart.ringTrailDistance);
            ringTrail.enabled=radius>0;
            for(int i=0;i<64;i++){float angle=i*Mathf.PI*2/64;ringTrail.SetPosition(i,origin+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius+clip.offset);}
        }
        transform.rotation = ring ? Quaternion.identity : Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        Vector2 bounds = visual.sprite.bounds.size;
        Vector2 size = ring ? Vector2.one * (2 * distance * set.ringDiameterScale * clip.scale) : set.CrescentDisplaySize;
        transform.localScale = new Vector3(size.x / Mathf.Max(.001f, bounds.x), size.y / Mathf.Max(.001f, bounds.y), 1);
        DrawEchoes();
    }
    void DrawEchoes()
    {
        if(echoes==null)return;
        var sample=new TrailSample{time=age,position=transform.position};
        if(history.Count>0&&history[history.Count-1].time==age)history[history.Count-1]=sample;
        else history.Add(sample);
        float duration=Mathf.Max(.01f,heart.trailTime);
        while(history.Count>2&&history[1].time<age-duration)history.RemoveAt(0);
        for(int i=0;i<echoes.Length;i++)
        {
            float fraction=(i+1f)/echoes.Length,target=age-duration*fraction;
            var echo=echoes[i];echo.enabled=history.Count>1&&target>=history[0].time;
            if(!echo.enabled)continue;
            int n=1;while(n<history.Count-1&&history[n].time<target)n++;
            var a=history[n-1];var b=history[n];
            echo.transform.position=Vector3.Lerp(a.position,b.position,Mathf.InverseLerp(a.time,b.time,target));
            echo.transform.localScale=Vector3.one*Mathf.Lerp(.98f,.9f,fraction);
            echo.sprite=visual.sprite;
            Color color=heart.trailColor;color.a*=heart.moonTrailOpacity*(1-fraction*.85f);echo.color=color;
        }
    }
    void OnDestroy(){if(bladeMaterial)Destroy(bladeMaterial);if(trailMaterial)Destroy(trailMaterial);}
}
