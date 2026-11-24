using System.Collections.Generic;
using UnityEngine;
public sealed class V09Elements:MonoBehaviour
{
 public ArenaPrototype arena;public V06Attack attack;
 [Header("试玩参数：基础效果")]
 public float wetDuration=3;
 public float wetRadius=2;
 public float lightningRadius=2;
 public float fireBaseBonus=10;
 [Tooltip("尚未确定：无雷劈时采用一次雷击")] public int defaultLightningSegments=1;
 [Tooltip("0表示暂不启用后续雷电眩晕特性")] public int stunHitThreshold=0;
 public float stunDuration=.1f;
 [Header("中毒外观")]
 public Color poisonTint=new Color(.82f,.65f,1,1);
 [Min(.1f)]public float poisonBlinkHz=2;
 [Range(0,1)]public float poisonTintStrength=.65f;
 public bool HasPoison(int id)=>skillPoisons.Exists(p=>p.id==id&&p.remaining>0);
 readonly Dictionary<int,float> ice=new Dictionary<int,float>();readonly Dictionary<int,float> slows=new Dictionary<int,float>();
 readonly Dictionary<int,float> wet=new Dictionary<int,float>();readonly Dictionary<int,int> lightningHits=new Dictionary<int,int>();readonly Dictionary<int,float> stuns=new Dictionary<int,float>();
 readonly List<Storm> storms=new List<Storm>();readonly List<ArenaPrototype.Target> targets=new List<ArenaPrototype.Target>();
 sealed class Storm {public Vector2 center;public float delay=1,age,next,amount;public int done,count;}
 public float Vulnerability(int id)=>wet.ContainsKey(id)?1+(attack.Level(V06CardKind.浪)>0?attack.Value(V06CardKind.浪):10)/100:1;
 public float EnemyRate(int id)=>stuns.ContainsKey(id)?0:ice.ContainsKey(id)?1-slows[id]/100:1;
 public void ResetRun(){wet.Clear();stuns.Clear();ice.Clear();slows.Clear();skillPoisons.Clear();storms.Clear();lightningHits.Clear();}
 void Update(){if(!arena.Alive||attack.clock.Paused)return;float dt=Time.deltaTime*arena.WorldRate;TickSkillPoison(dt);Decay(ice,dt);Decay(wet,dt);Decay(stuns,dt);for(int i=storms.Count-1;i>=0;i--){var s=storms[i];s.delay-=dt;if(s.delay>0)continue;s.age+=dt;while(s.done<s.count&&s.age>=s.next){arena.ReadTargets(targets);foreach(var t in targets)if(Vector2.Distance(t.position,s.center)<=lightningRadius){arena.V09FinalHit(t.id,s.amount/s.count,DamageSource.Weapon);lightningHits.TryGetValue(t.id,out int hits);hits++;if(stunHitThreshold>0&&hits>=stunHitThreshold){stuns[t.id]=stunDuration;hits=0;}lightningHits[t.id]=hits;}s.done++;s.next=s.count==1?3:3f*s.done/(s.count-1);}if(s.done==s.count)storms.RemoveAt(i);}}
 void Decay(Dictionary<int,float> map,float dt){var keys=new List<int>(map.Keys);foreach(int key in keys){map[key]-=dt;if(map[key]<=0||!arena.TargetPosition(key,out _))map.Remove(key);}}
 public void ApplyWet(Vector2 position,float radius){arena.ReadTargets(targets);foreach(var target in targets)if(Vector2.Distance(target.position,position)<=radius)wet[target.id]=wetDuration;}
 public bool Hit(int id,Vector2 position,float attackPower,ref float direct){if(attack.Level(V06CardKind.冰寒)>0&&Random.value*100<attack.Value(V06CardKind.冰寒)){ice[id]=attack.build.iceDuration;slows[id]=attack.Secondary(V06CardKind.冰寒);}if(attack.Level(V06CardKind.水涌)>0&&Random.value*100<attack.Value(V06CardKind.水涌)){arena.ReadTargets(targets);foreach(var t in targets)if(Vector2.Distance(t.position,position)<=wetRadius)wet[t.id]=wetDuration;}
 if(attack.Level(V06CardKind.引雷)>0&&Random.value*100<attack.Value(V06CardKind.引雷)){storms.Add(new Storm{center=position,amount=attackPower*attack.Secondary(V06CardKind.引雷)/100,count=attack.Level(V06CardKind.雷劈)>0?Mathf.RoundToInt(attack.Value(V06CardKind.雷劈)):defaultLightningSegments});return true;}
 if(attack.Level(V06CardKind.火羽)>0&&Random.value*100<attack.Value(V06CardKind.火羽))direct*=1+(fireBaseBonus+attack.Value(V06CardKind.炎))/100;return false;}
 sealed class SkillDot {public int id;public float remaining,tick;public List<float> layers=new List<float>();}
 readonly List<SkillDot> skillPoisons=new List<SkillDot>();
 public void SkillPoison(int id,float amount){int level=attack.Level(V06CardKind.淬毒之刃);if(level==0||Random.value*100>=attack.Value(V06CardKind.淬毒之刃))return;var dot=skillPoisons.Find(d=>d.id==id);if(dot==null){dot=new SkillDot{id=id};skillPoisons.Add(dot);}if(level<4)dot.layers.Clear();if(dot.layers.Count<5)dot.layers.Add(amount*attack.Secondary(V06CardKind.淬毒之刃)/100);dot.remaining=3;}
 void TickSkillPoison(float dt){for(int i=skillPoisons.Count-1;i>=0;i--){var dot=skillPoisons[i];if(!arena.TargetPosition(dot.id,out _)){skillPoisons.RemoveAt(i);continue;}float advance=Mathf.Min(dt,dot.remaining);dot.remaining-=advance;dot.tick+=advance;while(dot.tick>=.5f){dot.tick-=.5f;float sum=0;foreach(float damage in dot.layers)sum+=damage;arena.V09FinalHit(dot.id,sum/6,DamageSource.Skill);}if(dot.remaining<=0)skillPoisons.RemoveAt(i);}}
}
