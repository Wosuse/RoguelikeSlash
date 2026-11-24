using System.Collections.Generic;
using UnityEngine;
public sealed class V06Attack:MonoBehaviour
{
 [System.NonSerialized] public bool suspendFiring;
 public ArenaPrototype arena;public RunStats stats;public RunClock clock;public V06Skills skills;
 public V06Card[] cards;public V09Elements elements;public V09Build build;
 [Header("统一普攻")] public float interval=2.5f;
 public float speed=10;
 public float damage=100;
 public float size=.5f;
 public float burstInterval=.1f;
 public float searchRadius=8;
 [Min(.1f),InspectorName("基础射程")] public float range=6;
 public Sprite projectileSprite;public Color projectileColor=Color.white;public AudioClip fireSound,hitSound;public int budget=512;
 [Header("会心必暴弹外观")]
 [InspectorName("必暴弓箭图像")] public Sprite focusArrowSprite;
 [InspectorName("弓箭朝向修正（度，朝右素材填0）")] public float focusArrowAngleOffset=0;
 [InspectorName("必暴弓箭染色")] public Color focusArrowTint=new Color(1,.45f,.42f,1);
 [InspectorName("红色拖尾颜色")] public Color focusTrailColor=new Color(1,.13f,.1f,1);
 [Min(.01f),InspectorName("红色拖尾宽度")] public float focusTrailWidth=.22f;
 [Min(.01f),InspectorName("红色拖尾持续时间（秒）")] public float focusTrailTime=.28f;
 [Header("锈刃")] public float poisonDuration=3;
 public float poisonTick=.5f;
 // Keep serialized card IDs stable: the removed card occupied index 8.
 [SerializeField] int[] levels=new int[(int)V06CardKind.离别赠礼+1];float timer,burstTimer;int burstIndex,burstCount;Vector2 aim;
 readonly List<KeyValuePair<float,ArenaPrototype.Target>> contacts=new List<KeyValuePair<float,ArenaPrototype.Target>>();
 readonly List<ArenaPrototype.Target> targets=new List<ArenaPrototype.Target>();readonly List<Shot> shots=new List<Shot>();readonly Stack<SpriteRenderer> pool=new Stack<SpriteRenderer>();readonly List<Poison> poisons=new List<Poison>();AudioSource audioSource;
 sealed class Round {public bool danced;}
 Round currentRound;
 sealed class Shot {public Round round;public SpriteRenderer renderer;public Vector2 pos,dir;public float left,factor,attackPower,criticalChance,criticalDamage,baseDamage;public int generation,index,hits;public bool split,focusArrow;public HashSet<int> touched=new HashSet<int>();}
 sealed class Poison {public int id;public float left,tick;public List<float> layers=new List<float>();}
 public int Level(V06CardKind kind)=>(int)kind<levels.Length?levels[(int)kind]:0;
 public V06Card Config(V06CardKind kind)=>System.Array.Find(cards,c=>c&&c.kind==kind);
 public float Value(V06CardKind k)=>Level(k)>0&&Config(k)?Config(k).Value(Level(k)):0;
 public float Secondary(V06CardKind k)=>Level(k)>0&&Config(k)?Config(k).Secondary(Level(k)):0;
 public float MoveMultiplier=>(build?build.MoveFactor:1)+(Secondary(V06CardKind.灵巧射手)+(Level(V06CardKind.剑客之道)>0?10:0))/100;
 public string DebugAttackStatus(){if(!enabled||!gameObject.activeInHierarchy)return "普攻组件已关闭";if(suspendFiring)return "普攻开关已关闭";if(!arena.Alive)return "角色已死亡";if(clock.Paused)return "游戏暂停中";if(Level(V06CardKind.亮剑)>0)return "亮剑已禁用普通攻击";if(!arena.CanAutoAttack)return "技能释放中，暂缓普攻";arena.ReadTargets(targets);if(targets.Count==0)return "没有目标：请摆放木桩或开启生怪";float nearest=float.PositiveInfinity;foreach(var target in targets)nearest=Mathf.Min(nearest,Vector2.Distance(target.position,arena.PlayerPosition));if(nearest>EffectiveRange)return "目标超出射程";return "普攻运行中 · 已发射 "+DebugShotsFired+" 发";}
 public float EffectiveRange=>Mathf.Max(0,range*(1+Value(V06CardKind.隼之眼)/100));
 public float NormalCritChance=>Mathf.Clamp(stats.Value(RunStat.CritChance)+Level(V06CardKind.狙击天赋)*Value(V06CardKind.狙击天赋),0,100);
 public float NormalCritDamage=>stats.Value(RunStat.CritDamage)+Level(V06CardKind.狙击天赋)*Secondary(V06CardKind.狙击天赋);
 public bool HasPoison(int id)=>poisons.Exists(p=>p.id==id&&p.left>0);
 public int ProjectileCount=>1+Mathf.RoundToInt(Value(V06CardKind.连击弹));
 public bool Available(V06Card card)=>Level(card.kind)<card.maxPicks&&(card.kind!=V06CardKind.分裂||Level(card.kind)<2||ProjectileCount>=Level(card.kind));
 public void Grant(V06Card card){if(!Available(card))return;if(levels.Length< (int)V06CardKind.离别赠礼+1)System.Array.Resize(ref levels,(int)V06CardKind.离别赠礼+1);levels[(int)card.kind]++;if(build)build.CardGranted(card.kind);if(skills)skills.CardGranted(card.kind);if(card.kind==V06CardKind.血狂)stats.ClampHealth();if(card.sound)audioSource.PlayOneShot(card.sound);}
 public bool Remove(V06CardKind kind){int index=(int)kind;if(index<0||index>=levels.Length||levels[index]<=0)return false;levels[index]--;if(build)build.CardRemoved(kind,levels[index]);if(skills)skills.CardRemoved(kind,levels[index]);return true;}
 public void ResetRun(){DebugShotsFired=0;if(elements)elements.ResetRun();if(build)build.ResetRun();System.Array.Clear(levels,0,levels.Length);foreach(var s in shots)Recycle(s);shots.Clear();poisons.Clear();timer=burstTimer=0;burstCount=burstIndex=0;}
 public float ResolveNormal(float amount,bool crit){bool critical=crit&&Random.value*100<NormalCritChance;return amount*stats.Value(RunStat.Attack)/100*(critical?(NormalCritDamage)/100:1);}
 void Awake(){audioSource=gameObject.AddComponent<AudioSource>();var progression=arena.GetComponentInChildren<RunProgression>();var definitions=new AbilityCardDefinition[cards.Length];for(int i=0;i<cards.Length;i++){var d=ScriptableObject.CreateInstance<AbilityCardDefinition>();d.v06=cards[i];d.title=cards[i].name;d.description=cards[i].details;d.icon=cards[i].icon;definitions[i]=d;}progression.cards=definitions;}
 void Update(){if(!arena.Alive||clock.Paused)return;float dt=Time.deltaTime*arena.WorldRate;arena.ReadTargets(targets);if(build)build.Tick(dt);timer-=dt;
 if(!suspendFiring && arena.CanAutoAttack && Level(V06CardKind.亮剑)==0){if(burstIndex<burstCount){burstTimer-=dt;if(burstTimer<=0)Fire();}else if(timer<=0&&Reserved()+ProjectileCount*10<=budget){float nearest=EffectiveRange;int id=-1;foreach(var t in targets){float d=Vector2.Distance(t.position,arena.PlayerPosition);if(d<nearest){nearest=d;id=t.id;aim=(t.position-arena.PlayerPosition).normalized;}}if(id>=0){burstCount=ProjectileCount;burstIndex=0;currentRound=new Round();Fire();float rate=(1+(stats.Value(RunStat.AttackSpeed)+Value(V06CardKind.灵巧射手)+(build?build.AttackSpeedBonus:0)+(skills.Writing?skills.writingAttackSpeed:0))/100)*(Level(V06CardKind.连击弹)>0?.75f:1)*(Level(V06CardKind.穿透之箭)>0?.75f:1);timer=interval/Mathf.Max(.01f,rate);}}}
 int initial=shots.Count;for(int i=initial-1;i>=0;i--)if(!Tick(shots[i],dt)){Recycle(shots[i]);shots.RemoveAt(i);}TickPoison(dt);}
 int Reserved(){int n=0;foreach(var shot in shots)n+=shot.split?1:shot.generation==0?10:shot.generation==1?3:1;return n;}
 public int DebugShotsFired {get;private set;}
 void Fire(){DebugShotsFired++;Spawn(arena.PlayerPosition,aim,skills.Writing?.5f:1,0,burstIndex++);if(burstIndex==1&&build&&build.BeginFocusRound()){var shot=shots[shots.Count-1];shot.baseDamage*=Mathf.Max(1,Secondary(V06CardKind.会心)/100);shot.criticalChance=100;SetFocusArrow(shot);}burstTimer=burstInterval;if(fireSound)audioSource.PlayOneShot(fireSound);}
 void Spawn(Vector2 pos,Vector2 dir,float factor,int generation,int index){if(shots.Count>=budget)return;SpriteRenderer r;if(pool.Count>0)r=pool.Pop();else{var obj=new GameObject("普通攻击弹丸");obj.transform.SetParent(transform);r=obj.AddComponent<SpriteRenderer>();}r.sprite=projectileSprite;r.color=projectileColor;r.sortingOrder=6;r.transform.localScale=Vector3.one*size;r.transform.position=pos;r.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(dir.y,dir.x)*Mathf.Rad2Deg-90);var oldTrail=r.GetComponent<TrailRenderer>();if(oldTrail){oldTrail.enabled=false;oldTrail.Clear();}r.gameObject.SetActive(true);shots.Add(new Shot{renderer=r,pos=pos,dir=dir,left=EffectiveRange,round=currentRound,factor=factor,attackPower=(build?build.AttackPower:stats.Value(RunStat.Attack)),criticalChance=NormalCritChance,criticalDamage=NormalCritDamage,baseDamage=damage*(Level(V06CardKind.分裂)>0?.75f:1)*(1+(Value(V06CardKind.射手天赋)+(build?build.AutoBonus:0))/100),generation=generation,index=index});}
 void SetFocusArrow(Shot shot){shot.focusArrow=true;shot.renderer.sprite=focusArrowSprite?focusArrowSprite:projectileSprite;shot.renderer.color=focusArrowTint;shot.renderer.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(shot.dir.y,shot.dir.x)*Mathf.Rad2Deg+focusArrowAngleOffset);var trail=shot.renderer.GetComponent<TrailRenderer>();if(!trail)trail=shot.renderer.gameObject.AddComponent<TrailRenderer>();trail.enabled=false;trail.time=focusTrailTime;trail.startWidth=focusTrailWidth;trail.endWidth=0;trail.startColor=focusTrailColor;trail.endColor=new Color(focusTrailColor.r,focusTrailColor.g,focusTrailColor.b,0);trail.minVertexDistance=.05f;trail.alignment=LineAlignment.View;trail.sortingLayerID=shot.renderer.sortingLayerID;trail.sortingOrder=shot.renderer.sortingOrder-1;trail.material=focusTrailMaterial?focusTrailMaterial:(focusTrailMaterial=new Material(Shader.Find("Sprites/Default")));trail.Clear();trail.enabled=true;}
 Material focusTrailMaterial;
 bool Tick(Shot s,float dt){float travel=Mathf.Min(s.left,speed*dt);Vector2 step=s.dir*travel;contacts.Clear();foreach(var t in targets){if(s.touched.Contains(t.id))continue;float u=step.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(t.position-s.pos,step)/step.sqrMagnitude):0;if(Vector2.Distance(s.pos+step*u,t.position)<.48f)contacts.Add(new KeyValuePair<float,ArenaPrototype.Target>(u,t));}contacts.Sort((a,b)=>a.Key.CompareTo(b.Key));foreach(var pair in contacts){var target=pair.Value;s.touched.Add(target.id);float pierce=Level(V06CardKind.穿透之箭)>0?Mathf.Max(Value(V06CardKind.穿透之箭)/100,1-s.hits*.1f):1;float amount=s.baseDamage*s.factor*pierce*s.attackPower/100;bool lightning=elements&&elements.Hit(target.id,target.position,s.attackPower,ref amount);if(!lightning)arena.V09FinalHit(target.id,amount*(Random.value*100<s.criticalChance?s.criticalDamage/100:1),DamageSource.Weapon);if(s.round!=null){if(build)build.Dance(!s.round.danced);s.round.danced=true;}skills.Charge();PoisonHit(target.id,amount);if(hitSound)audioSource.PlayOneShot(hitSound);foreach(var card in cards)if(card.hitEffect&&Level(card.kind)>0){var effect=Instantiate(card.hitEffect,target.position,Quaternion.identity);Destroy(effect,2);}
 if(!s.split){s.split=true;int lvl=Level(V06CardKind.分裂);bool allowed=s.generation==0&&s.index<Mathf.Max(1,lvl-1)||s.generation==1&&lvl>=2;if(lvl>0&&allowed)Split(s,target);}
 s.hits++;if(Level(V06CardKind.穿透之箭)==0||s.generation>0)return false;}
 s.pos+=step;s.left-=travel;s.renderer.transform.position=s.pos;s.renderer.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(s.dir.y,s.dir.x)*Mathf.Rad2Deg+(s.focusArrow?focusArrowAngleOffset:-90));return s.left>0;}
 void Split(Shot s,ArenaPrototype.Target hit){int count=s.generation==0?3:2;var used=new HashSet<int>{hit.id};for(int i=0;i<count;i++){int id=-1;float nearest=searchRadius;Vector2 pos=hit.position;foreach(var t in targets){if(used.Contains(t.id))continue;float d=Vector2.Distance(hit.position,t.position);if(d<nearest){nearest=d;id=t.id;pos=t.position;}}if(id<0)break;used.Add(id);Spawn(hit.position,(pos-hit.position).normalized,s.factor*.5f,s.generation+1,s.index);var child=shots[shots.Count-1];child.round=s.round;child.touched.Add(hit.id);child.attackPower=s.attackPower;child.criticalChance=s.criticalChance;child.criticalDamage=s.criticalDamage;child.baseDamage=s.baseDamage;if(s.focusArrow)SetFocusArrow(child);}}
 void PoisonHit(int id,float amount){int lvl=Level(V06CardKind.锈刃);if(lvl==0||Random.value*100>Value(V06CardKind.锈刃))return;var poison=poisons.Find(p=>p.id==id);if(poison==null){poison=new Poison{id=id};poisons.Add(poison);}if(lvl<4)poison.layers.Clear();if(poison.layers.Count<5)poison.layers.Add(amount*Secondary(V06CardKind.锈刃)/100);poison.left=poisonDuration;}
 void TickPoison(float dt){for(int i=poisons.Count-1;i>=0;i--){var p=poisons[i];if(!arena.TargetPosition(p.id,out _)){poisons.RemoveAt(i);continue;}float advance=Mathf.Min(dt,p.left);p.left-=advance;p.tick+=advance;while(p.tick+1e-5f>=poisonTick){p.tick-=poisonTick;float amount=0;foreach(float x in p.layers)amount+=x;arena.V09FinalHit(p.id,amount*poisonTick/poisonDuration,DamageSource.Bleed);}if(p.left<=0)poisons.RemoveAt(i);}}
 void Recycle(Shot s){var trail=s.renderer.GetComponent<TrailRenderer>();if(trail){trail.enabled=false;trail.Clear();}s.renderer.gameObject.SetActive(false);pool.Push(s.renderer);}
 void OnDestroy(){if(focusTrailMaterial)Destroy(focusTrailMaterial);}
}
