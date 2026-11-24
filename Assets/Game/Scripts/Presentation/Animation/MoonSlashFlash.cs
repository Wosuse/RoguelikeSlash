using System;
using UnityEngine;

[Serializable]
public sealed class MoonSlashFlashSettings
{
    [InspectorName("启用月牙刀光")] public bool enabled = true;
    [Min(.01f), InspectorName("大小（相对斩半径）")] public float size = 1.15f;
    [Min(0), InspectorName("出现距离（斩半径倍数）"), Tooltip("刀光中心距释放位置的距离。0 在角色中心，1 为当前斩半径；例如半径 3.5、倍数 0.45 时距离为 1.575。沿本次释放方向偏移。")] public float distance = .45f;
    [Min(0), InspectorName("出现延迟（秒）"), Tooltip("从本次斩释放开始计时，0 为立即出现。仅调整刀光，不改变伤害结算时机。")] public float delay = .025f;
    [Min(.01f), InspectorName("显示时间（秒）")] public float sweepTime = .13f;
    [Min(.01f), InspectorName("淡出时间（秒）")] public float fadeTime = .07f;
    [InspectorName("蓝色起始染色")] public Color blue = new Color(.4f,.75f,1,.95f);
    [InspectorName("白色收尾染色")] public Color white = new Color(.85f,.96f,1,.95f);
    [InspectorName("独立贴图（空则使用现有月牙首帧）")] public Sprite sprite;
    [Range(0,6), InspectorName("黑色描边宽度（贴图像素）")] public float outlinePixels=1.5f;
    [Range(.01f,.9f), InspectorName("边缘收紧阈值（越大越锐利）")] public float alphaThreshold=.2f;
    [InspectorName("描边颜色")] public Color outlineColor=new Color(.025f,.035f,.06f,1);
    [InspectorName("材质（空则使用默认透明材质）")] public Material material;
}

// Visual only: the existing slash owns damage and movement locking.
public sealed class MoonSlashFlash : MonoBehaviour
{
    ArenaPrototype owner;
    MoonSlashFlashSettings settings;
    HeartBladeSettings heart;
    SpriteRenderer visual;
    Material ownedMaterial;
    float age;

    public void Initialize(ArenaPrototype arena, SkillAnimationClip animation, MoonSlashFlashSettings config, float attackRadius, Vector2 direction, HeartBladeSettings heartStyle=null)
    {
        owner=arena; settings=config;heart=heartStyle;
        // Capture the swing origin once; moving the player must not drag the slash.
        Vector2 origin=arena.PlayerPosition;
        direction=direction.sqrMagnitude>.0001f?direction.normalized:arena.VisualFacing;
        visual=gameObject.AddComponent<SpriteRenderer>(); visual.sortingOrder=12;
        if(settings.material)visual.sharedMaterial=settings.material;
        else
        {
            var shader=Resources.Load<Shader>("BladeOutline");
            if(shader)
            {
                ownedMaterial=new Material(shader);
                ownedMaterial.SetFloat("_OutlinePixels",settings.outlinePixels);
                ownedMaterial.SetFloat("_AlphaThreshold",settings.alphaThreshold);
                ownedMaterial.SetColor("_OutlineColor",settings.outlineColor);
                visual.sharedMaterial=ownedMaterial;
            }
        }
        visual.sprite=settings.sprite?settings.sprite:animation!=null?animation.Sample(0,true):null;
        transform.position=(Vector3)(origin+direction*attackRadius*Mathf.Max(0,settings.distance))+Vector3.back*.15f;
        transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
        if(visual.sprite)transform.localScale=Vector3.one*(attackRadius*Mathf.Max(.01f,settings.size)/Mathf.Max(.001f,visual.sprite.bounds.size.y));
        Advance(0);
    }

    void Update()
    {
        if(!owner||!owner.Alive){Destroy(gameObject);return;}
        if(owner.v06Skills.clock.Paused)return;
        Advance(Time.deltaTime);
    }

    public void Advance(float dt)
    {
        age+=Mathf.Max(0,dt);
        float active=age-settings.delay;
        visual.enabled=active>=0;
        if(active<0)return;
        float progress=Mathf.Clamp01(active/Mathf.Max(.01f,settings.sweepTime));
        float fade=Mathf.Clamp01((active-settings.sweepTime)/Mathf.Max(.01f,settings.fadeTime));
        if(!visual.sprite){visual.enabled=false;if(fade>=1)Destroy(gameObject);return;}
        Color color=Color.Lerp(heart!=null?heart.dark:settings.blue,heart!=null?heart.red:settings.white,progress);
        color.a*=1-fade; visual.color=color;
        if(fade>=1){visual.enabled=false;Destroy(gameObject);}
    }
    void OnDestroy(){if(ownedMaterial)Destroy(ownedMaterial);}
}
