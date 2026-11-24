using System.Collections.Generic;
using UnityEngine;

public sealed class EnemyRewards : MonoBehaviour
{
    [System.Serializable] public sealed class Tier
    {
        public string name;
        [Min(0)] public int xp, coins;
        [Min(0)] public float weight;
        [Min(.1f)] public float healthMultiplier = 1;
    }
    [InspectorName("独立奖励档位")] public Tier[] tiers = {
        new Tier {name="普通",xp=3,coins=1,weight=85}, new Tier {name="精英",xp=10,coins=4,weight=10,healthMultiplier=2},
        new Tier {name="困难",xp=25,coins=10,weight=4,healthMultiplier=4}, new Tier {name="极难",xp=60,coins=25,weight=1,healthMultiplier=8} };
    public ArenaPrototype arena;
    public RunProgression progression;
    public RunClock clock;
    [Header("掉落物与拾取")]
    [Min(.1f), InspectorName("磁吸范围")] public float magnetRange = 2.5f;
    [Min(.1f), InspectorName("磁吸速度")] public float magnetSpeed = 9;
    [Min(1), InspectorName("最大显示掉落数（超出合并）")] public int maxDrops = 256;
    [InspectorName("材质")] public Material material;
    [InspectorName("经验纹理")] public Texture2D xpTexture;
    [InspectorName("金币纹理")] public Texture2D coinTexture;
    [InspectorName("经验颜色")] public Color xpColor = Color.cyan;
    [InspectorName("金币颜色")] public Color coinColor = Color.yellow;
    [InspectorName("拾取音效")] public AudioClip pickupSound;
    public Transform container;
    sealed class Drop { public GameObject obj; public Renderer renderer; public int xp, coins; }
    readonly List<Drop> drops = new List<Drop>();
    readonly Stack<Drop> pool = new Stack<Drop>();
    public int LiveDrops => drops.Count;
    public int RollTier()
    {
        float sum=0; foreach (var tier in tiers) sum += tier.weight;
        float r=Random.value*sum;
        for(int i=0;i<tiers.Length;i++) { r-=tiers[i].weight; if(r<=0)return i; }
        return 0;
    }
    public void DropRewards(Vector2 position,int tier)
    {
        var value=tiers[tier]; Spawn(position+Vector2.left*.15f,value.xp,0); Spawn(position+Vector2.right*.15f,0,value.coins);
    }
    void Spawn(Vector2 position,int xp,int coins)
    {
        if (drops.Count >= Mathf.Max(2,maxDrops))
        {
            Drop nearest=drops[0]; float distance=float.MaxValue;
            foreach(var item in drops) { float d=((Vector2)item.obj.transform.position-position).sqrMagnitude; if(d<distance){distance=d;nearest=item;} }
            nearest.xp+=xp; nearest.coins+=coins; return;
        }
        Drop drop;
        if(pool.Count>0) drop=pool.Pop();
        else {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(obj.GetComponent<Collider>());
            obj.transform.SetParent(container ? container : transform); var renderer=obj.GetComponent<Renderer>();
            renderer.sharedMaterial=new Material(material); drop=new Drop {obj=obj,renderer=renderer};
        }
        drop.xp=xp; drop.coins=coins; drop.obj.name=xp>0?"经验球":"金币";
        drop.renderer.sharedMaterial.color=xp>0?xpColor:coinColor;
        drop.renderer.sharedMaterial.mainTexture=xp>0?xpTexture:coinTexture;
        drop.obj.transform.position=new Vector3(position.x,position.y,-.15f); drop.obj.transform.localScale=Vector3.one*.23f;
        drop.obj.SetActive(true); drops.Add(drop);
    }
    void Update()
    {
        if(!arena || !arena.Alive || clock.Paused)return;
        for(int i=drops.Count-1;i>=0;i--)
        {
            var drop=drops[i]; Vector2 p=drop.obj.transform.position;
            if(Vector2.Distance(p,arena.PlayerPosition)>magnetRange)continue;
            p=Vector2.MoveTowards(p,arena.PlayerPosition,magnetSpeed*Time.deltaTime*arena.WorldRate);
            drop.obj.transform.position=new Vector3(p.x,p.y,-.15f);
            if(Vector2.Distance(p,arena.PlayerPosition)>.4f)continue;
            int xp=drop.xp,coins=drop.coins; drop.obj.SetActive(false);pool.Push(drop);drops.RemoveAt(i);
            progression.AddCoins(coins); progression.AddXP(xp);
            if(pickupSound) AudioSource.PlayClipAtPoint(pickupSound,Camera.main.transform.position,.3f);
            if(clock.Paused)break;
        }
    }
    public void ResetRun() { foreach(var drop in drops){drop.obj.SetActive(false);pool.Push(drop);}drops.Clear(); }
}
