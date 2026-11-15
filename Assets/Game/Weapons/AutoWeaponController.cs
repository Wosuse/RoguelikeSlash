using System.Collections.Generic;
using UnityEngine;

/// <summary>Owns weapon projectiles only. Hostile bullets and active skills stay in the arena.</summary>
[DefaultExecutionOrder(100)]
public sealed class AutoWeaponController : MonoBehaviour
{
    [Header("通用命中特效")]
    [SerializeField] Sprite[] hitFrames;
    [SerializeField, Min(.01f)] float hitFrameSeconds = .055f;
    [SerializeField, Min(.01f)] float hitSize = .65f;
    readonly List<HitFlash> flashes = new List<HitFlash>();
    readonly Stack<SpriteRenderer> flashPool = new Stack<SpriteRenderer>();
    sealed class HitFlash { public SpriteRenderer renderer; public float age; }
    public ArenaPrototype arena;
    public RunStats stats;
    public RunClock clock;
    public WeaponInventory inventory;
    public BleedStatusController bleed;
    [InspectorName("弹丸父物体")] public Transform container;
    [InspectorName("默认材质")] public Material fallbackMaterial;
    [Range(0,30), InspectorName("侧向角度")] public float sideAngle = 30;
    [Min(54), InspectorName("弹丸预算（整轮预留，满额延后攻击）")] public int projectileBudget = 512;
    [SerializeField, InspectorName("剩余攻击间隔")] float shotTimer;
    [SerializeField, InspectorName("活跃弹丸")] int activeCount;
    [SerializeField, InspectorName("预算阻塞次数")] int budgetWaits;
    readonly List<ArenaPrototype.Target> targets = new List<ArenaPrototype.Target>(100);
    readonly List<Shot> shots = new List<Shot>();
    readonly Stack<Shot> pool = new Stack<Shot>();
    readonly List<Contact> contacts = new List<Contact>(100);
    AudioSource audioSource;
    int volleyNumber;
    const float ProjectileInterval = .1f;
    Burst pendingBurst;
    sealed class Burst
    {
        public WeaponDefinition definition;
        public Volley volley = new Volley();
        public Vector2 direction;
        public int level, lanes, count, next;
        public float alternatingAngle, untilNext;
    }
    enum Phase { Fly, Spin, Return, HomeSpin }
    sealed class Volley
    {
        public int remaining;
        public readonly Dictionary<int,int> hits = new Dictionary<int,int>();
    }
    sealed class Shot
    {
        public GameObject obj; public Renderer renderer;
        public WeaponDefinition def; public Volley volley;
        public Phase phase; public int level;
        public bool child;
        public Vector2 position,direction;
        public float distance,speed,range,factor,age,spinAge,tick;
        public readonly HashSet<int> hit = new HashSet<int>();
    }
    public int ActiveCount => shots.Count;
    public static float BowRepeatMultiplier(int previousHits) => Mathf.Max(.1f,Mathf.Pow(.65f,previousHits));
    public static float ProjectileFactor(int index) => 1-.2f*index;
    void Awake(){audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;}
    void Update()
    {
        if (clock && !clock.Paused) UpdateHitFlashes(Time.deltaTime);
        if(!arena || !arena.Alive || clock.Paused)return;
        float dt=Time.deltaTime*arena.WorldRate;
        shotTimer=Mathf.Max(0,shotTimer-dt);
        arena.ReadTargets(targets);
        // Existing projectiles and DOT finish normally; skill use blocks new volleys.
        if (arena.CanAutoAttack)
        {
            if (pendingBurst != null) AdvanceBurst(dt);
            else if (shotTimer <= 0) TryFire();
        }
        for(int i=shots.Count-1;i>=0;i--) if(!Tick(shots[i],dt)) Recycle(i);
        activeCount=shots.Count;
    }
    void TryFire()
    {
        var def=inventory.Current; int level=inventory.Level;
        float range=def.range+(def.kind==WeaponKind.Bow && level>=2?def.upgradedRange:0);
        int target=-1;float closest=range*range;
        for(int i=0;i<targets.Count;i++){float d=(targets[i].position-arena.PlayerPosition).sqrMagnitude;if(d<closest){closest=d;target=i;}}
        if(target<0)return;
        int roots=inventory.ProjectileCount*inventory.DirectionCount;
        int reservation=def.kind==WeaponKind.Bow&&level>=1?roots*(level>=3?6:3):roots;
        // Reserve future splits too; do not silently swallow a portion of a volley.
        int reserved=shots.Count;
        foreach(var shot in shots)if(shot.def.kind==WeaponKind.Bow&&!shot.child&&shot.level>=1)reserved+=shot.level>=3?5:2;
        if(reserved+reservation>projectileBudget){budgetWaits++;return;}
        Vector2 direction=(targets[target].position-arena.PlayerPosition).normalized;
        volleyNumber++;
        pendingBurst = new Burst { definition = def, level = level, direction = direction,
            lanes = inventory.DirectionCount, count = inventory.ProjectileCount,
            alternatingAngle = volleyNumber % 2 == 0 ? sideAngle : -sideAngle };
        FireBurstWave();
        shotTimer=def.interval/(1+stats.Value(RunStat.AttackSpeed)/100);
    }
    void AdvanceBurst(float dt)
    {
        pendingBurst.untilNext -= dt;
        if (pendingBurst.untilNext <= .00001f) FireBurstWave();
    }
    void FireBurstWave()
    {
        var burst = pendingBurst;
        for (int lane = 0; lane < burst.lanes; lane++)
        {
            float angle = burst.lanes == 3 ? (lane - 1) * sideAngle : lane == 0 ? 0 : burst.alternatingAngle;
            Spawn(burst.definition, burst.level, burst.volley, arena.PlayerPosition,
                Rotate(burst.direction, angle), ProjectileFactor(burst.next), false);
        }
        if (burst.definition.fireSound) audioSource.PlayOneShot(burst.definition.fireSound, burst.definition.volume);
        burst.next++;
        burst.untilNext = ProjectileInterval;
        if (burst.next >= burst.count) pendingBurst = null;
    }
    void Spawn(WeaponDefinition def,int level,Volley volley,Vector2 position,Vector2 direction,float factor,bool child)
    {
        Shot shot;
        if(pool.Count>0)shot=pool.Pop();
        else{
            var obj=GameObject.CreatePrimitive(PrimitiveType.Quad);Destroy(obj.GetComponent<Collider>());
            obj.transform.SetParent(container?container:transform);
            var renderer=obj.GetComponent<Renderer>();renderer.sharedMaterial=new Material(fallbackMaterial);
            shot=new Shot{obj=obj,renderer=renderer};
        }
        shot.def=def;shot.level=level;shot.volley=volley;volley.remaining++;shot.child=child;
        shot.phase=Phase.Fly;shot.position=position;shot.direction=direction;shot.factor=factor;
        shot.speed=def.speed;shot.range=child?def.childRange:def.range+(def.kind==WeaponKind.Bow&&level>=2?def.upgradedRange:0);
        shot.distance=shot.age=shot.spinAge=shot.tick=0;shot.hit.Clear();
        var material=shot.renderer.sharedMaterial;material.CopyPropertiesFromMaterial(def.material?def.material:fallbackMaterial);
        material.color=def.color;if(def.texture)material.mainTexture=def.texture;
        shot.obj.name=def.displayName+(child?"·子箭":"");shot.obj.SetActive(true);shots.Add(shot);Paint(shot);
    }
    bool Tick(Shot shot,float dt)
    {
        shot.age+=dt;if(shot.age>30)return false;
        if(shot.phase==Phase.Spin||shot.phase==Phase.HomeSpin)
        {
            float duration=shot.level>=3?2:1;
            float advance=Mathf.Min(dt,Mathf.Max(0,duration-shot.spinAge));shot.spinAge+=advance;shot.tick+=advance;
            while(shot.tick+0.00001f>=shot.def.spinTick)
            {
                shot.tick-=shot.def.spinTick;
                arena.ReadTargets(targets);
                foreach(var target in targets)if(Vector2.Distance(target.position,shot.position)<=shot.def.spinRadius)
                    arena.WeaponHit(target.id,5*shot.factor,DamageSource.Spin);
            }
            Paint(shot);
            if(shot.spinAge+0.00001f<duration)return true;
            if(shot.phase==Phase.HomeSpin||shot.level<3)return false;
            shot.hit.Clear(); // Return hits are independent of outbound penetration.
            shot.phase=Phase.Return;return true;
        }
        if(shot.phase==Phase.Return)
        {
            Vector2 origin = shot.position;
            shot.position=Vector2.MoveTowards(origin,arena.PlayerPosition,shot.def.speed*dt);
            Vector2 returnStep = shot.position - origin;
            if (returnStep.sqrMagnitude > 0)
            {
                arena.ReadTargets(targets);
                foreach (var target in targets)
                {
                    if (shot.hit.Contains(target.id) || !Segment(origin, returnStep, target.position, .48f, out float t)) continue;
                    shot.hit.Add(target.id);
                    PlayProjectileHit(shot.def, origin + returnStep * t);
                    arena.WeaponHit(target.id, 20 * shot.factor, DamageSource.Weapon);
                }
            }
            if(Vector2.Distance(shot.position,arena.PlayerPosition)<.1f)StartSpin(shot,true);
            Paint(shot);return true;
        }
        if(shot.def.kind==WeaponKind.Bow)shot.speed=Mathf.Max(shot.def.speed*.5f,shot.speed-shot.def.deceleration*dt);
        float travel=Mathf.Min(shot.speed*dt,Mathf.Max(0,shot.range-shot.distance));
        Vector2 start=shot.position,step=shot.direction*travel;
        float wallT=2;
        if(shot.def.kind==WeaponKind.Bow && shot.def.obstacleMask.value!=0 && travel>0)
        {
            var wall=Physics2D.Raycast(start,shot.direction,travel,shot.def.obstacleMask);
            if(wall.collider)wallT=wall.distance/travel;
        }
        // Sort swept contacts by distance so penetration never depends on enemy list order.
        arena.ReadTargets(targets);
        contacts.Clear();
        foreach(var target in targets)
            if(!shot.hit.Contains(target.id)&&Segment(start,step,target.position,.48f,out float t)&&t<=wallT)
                contacts.Add(new Contact{id=target.id,t=t});
        contacts.Sort((a,b)=>a.t.CompareTo(b.t));
        foreach(var contact in contacts)
        {
            if(!arena.TargetPosition(contact.id,out _))continue;
            shot.position=start+step*contact.t;shot.hit.Add(contact.id);
            PlayProjectileHit(shot.def, shot.position);
            switch(shot.def.kind)
            {
                case WeaponKind.Dart:
                    arena.WeaponHit(contact.id,100*shot.factor*(1-.3f*(shot.hit.Count-1)),DamageSource.Weapon);
                    if(shot.hit.Count>=(shot.level>=1?3:1))
                    {if(shot.level>=2){StartSpin(shot,false);Paint(shot);return true;}return false;}
                    break;
                case WeaponKind.Knife:
                    bool full=bleed.IsFull(contact.id,shot.level);
                    arena.WeaponHit(contact.id,50*shot.factor,DamageSource.Weapon);
                    if (shot.level >= 2 && full)
                        arena.WeaponHit(contact.id,250*.15f,DamageSource.Weapon,false);
                    bleed.Apply(contact.id,shot.level,shot.def.bleedTick);
                    return false;
                case WeaponKind.Bow:
                    shot.volley.hits.TryGetValue(contact.id,out int previous);
                    shot.volley.hits[contact.id]=previous+1;
                    float damage=(shot.level>=2?250:150)*(shot.child?.75f:1)*(shot.speed/shot.def.speed)*shot.factor*BowRepeatMultiplier(previous);
                    arena.WeaponHit(contact.id,damage,DamageSource.Weapon);
                    Split(shot);return false;
            }
        }
        if(wallT<=1){shot.position=start+step*wallT;Split(shot);return false;}
        shot.position=start+step;shot.distance+=travel;Paint(shot);
        if(shot.distance+0.00001f<shot.range)return true;
        if(shot.def.kind==WeaponKind.Dart&&shot.level>=2){StartSpin(shot,false);return true;}
        return false;
    }
    struct Contact {public int id;public float t;}
    void Split(Shot parent)
    {
        if(parent.def.kind!=WeaponKind.Bow||parent.level<1||parent.child)return;
        int count=parent.level>=3?5:2;
        arena.ReadTargets(targets);
        targets.RemoveAll(t=>(t.position-parent.position).sqrMagnitude>parent.def.searchRadius*parent.def.searchRadius);
        targets.Sort((a,b)=>(a.position-parent.position).sqrMagnitude.CompareTo((b.position-parent.position).sqrMagnitude));
        for(int i=0;i<count;i++)
        {
            Vector2 direction=targets.Count>0?(targets[i%targets.Count].position-parent.position).normalized:Rotate(parent.direction,(i-(count-1)*.5f)*35);
            if(direction.sqrMagnitude<.01f)direction=Rotate(parent.direction,(i+1)*60);
            Spawn(parent.def,parent.level,parent.volley,parent.position+direction*.55f,direction,parent.factor,true);
            // A short spawn clearance prevents same-frame re-hits, without blacklisting the original enemy.
        }
    }
    void StartSpin(Shot shot,bool home){shot.phase=home?Phase.HomeSpin:Phase.Spin;shot.spinAge=shot.tick=0;}
    void Paint(Shot shot)
    {
        shot.obj.transform.position=new Vector3(shot.position.x,shot.position.y,-.25f);
        bool spin=shot.phase==Phase.Spin||shot.phase==Phase.HomeSpin;
        float size = spin ? shot.def.size * 1.5f : shot.def.size;
        shot.obj.transform.localScale = shot.def.texture ? new Vector3(size,size,1) : new Vector3(size,size*.45f,1);
        float angle = shot.def.kind == WeaponKind.Dart ? shot.age * 720 : Mathf.Atan2(shot.direction.y,shot.direction.x)*Mathf.Rad2Deg;
        shot.obj.transform.rotation = Quaternion.Euler(0,0,angle);
    }
    void PlayProjectileHit(WeaponDefinition definition, Vector2 position)
    {
        if (definition.hitSound) audioSource.PlayOneShot(definition.hitSound, definition.volume);
        ShowHitFlash(position);
    }
    void ShowHitFlash(Vector2 position)
    {
        if (hitFrames == null || hitFrames.Length == 0 || flashes.Count >= 96) return;
        var renderer = flashPool.Count > 0 ? flashPool.Pop() : new GameObject("武器命中火花").AddComponent<SpriteRenderer>();
        renderer.transform.SetParent(container ? container : transform, false);
        renderer.transform.position = new Vector3(position.x, position.y, -.4f);
        renderer.transform.localScale = Vector3.one * hitSize;
        renderer.sprite = hitFrames[0];
        renderer.gameObject.SetActive(true);
        flashes.Add(new HitFlash { renderer = renderer });
    }
    void UpdateHitFlashes(float dt)
    {
        for (int i = flashes.Count - 1; i >= 0; i--)
        {
            var flash = flashes[i];
            flash.age += dt;
            int frame = Mathf.FloorToInt(flash.age / Mathf.Max(.01f, hitFrameSeconds));
            if (frame < hitFrames.Length) flash.renderer.sprite = hitFrames[frame];
            else RecycleFlash(i);
        }
    }
    void RecycleFlash(int index)
    {
        var renderer = flashes[index].renderer;
        renderer.gameObject.SetActive(false);
        flashPool.Push(renderer);
        flashes.RemoveAt(index);
    }
    void Recycle(int index)
    {
        var shot=shots[index];shot.volley.remaining--;if(shot.volley.remaining==0 && pendingBurst?.volley != shot.volley)shot.volley.hits.Clear();
        shot.obj.SetActive(false);shots.RemoveAt(index);pool.Push(shot);
    }
    public void ResetRun(){pendingBurst=null;while(shots.Count>0)Recycle(shots.Count-1);while(flashes.Count>0)RecycleFlash(flashes.Count-1);shotTimer=0;volleyNumber=budgetWaits=0;activeCount=0;if(bleed)bleed.ResetRun();}
    static Vector2 Rotate(Vector2 v,float angle){float a=angle*Mathf.Deg2Rad;return new Vector2(v.x*Mathf.Cos(a)-v.y*Mathf.Sin(a),v.x*Mathf.Sin(a)+v.y*Mathf.Cos(a));}
    public static bool Segment(Vector2 start,Vector2 step,Vector2 center,float radius,out float time)
    {
        Vector2 d=start-center;float c=d.sqrMagnitude-radius*radius;time=0;if(c<=0)return true;
        float a=step.sqrMagnitude;if(a<=0)return false;float b=Vector2.Dot(d,step),disc=b*b-a*c;if(disc<0)return false;
        time=(-b-Mathf.Sqrt(disc))/a;return time>=0&&time<=1;
    }
}
