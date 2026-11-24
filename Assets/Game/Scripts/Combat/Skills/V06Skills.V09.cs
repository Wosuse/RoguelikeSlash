using System.Collections.Generic;
using UnityEngine;
public sealed partial class V06Skills
{
 [Header("v0.9 写意（急速按点数加算）")]
 [InspectorName("写意期间攻速加成（%）")] public float writingAttackSpeed=70;
 [InspectorName("写意期间技能急速（点）")] public float writingHaste=60;
 [InspectorName("写意期间移速加成（%）"),Min(0)] public float writingMoveSpeed=20;
 float writingSlashTimer;
 [Header("剑气配置")]
 [Min(.01f),InspectorName("月牙基础恢复时间（秒）")] public float moonCooldown=10;
 [Min(.01f),InspectorName("涟漪基础恢复时间（秒）")] public float rippleCooldown=15;
 [Min(.01f),InspectorName("剑气飞行速度（单位/秒）")] public float swordSpeed=8;
 [Min(.01f),InspectorName("剑气飞行距离（世界单位）")] public float swordRange=10;
 [InspectorName("月牙判定跟随动画大小"),Tooltip("命中总宽度读取动画显示高度×显示倍率；关闭后在月牙卡牌配置中手动设置。")] public bool moonHitFollowsVisual=true;
 [HideInInspector,Min(.01f)] public float moonHitWidth=1;
 public float EffectiveMoonHitWidth=>moonHitFollowsVisual&&animationSet&&animationSet.crescent!=null&&animationSet.crescent.Valid?Mathf.Max(.01f,animationSet.CrescentDisplaySize.y):Mathf.Max(.01f,moonHitWidth);
 [SerializeField] float writingLeft;
 readonly List<ArenaPrototype.Target> swordTargets=new List<ArenaPrototype.Target>();
 readonly List<Sword> swords=new List<Sword>();
 sealed class Sword {public Vector2 origin,direction;public float distance,amount,critChance,critDamage;public bool ring;public HashSet<int> hit=new HashSet<int>();public List<SwordPush> pushes=new List<SwordPush>();public LineRenderer line;public SwordEnergyVisual visual;}
 sealed class SwordPush {public int id;public Vector2 direction;public float distance,lastSwordDistance;}
 public bool IsSwordPushing(int id){if(!Has(V06CardKind.剑气))return false;float limit=Configured(V06CardKind.剑气,2,6);foreach(var sword in swords)foreach(var push in sword.pushes)if(push.id==id&&push.distance<limit)return true;return false;}
 void PushWithSword(Sword sword){if(!Has(V06CardKind.剑气))return;float limit=Configured(V06CardKind.剑气,2,6);foreach(var push in sword.pushes){float step=Mathf.Min(Mathf.Max(0,sword.distance-push.lastSwordDistance),Mathf.Max(0,limit-push.distance));push.lastSwordDistance=sword.distance;if(step<=0)continue;arena.V09Push(push.id,push.direction,step);push.distance+=step;}}
 public bool Writing=>writingLeft>0;
 float Configured(V06CardKind kind,int field,float fallback){var c=arena.v06Attack.Config(kind);float value=c?(field==0?c.cooldown:field==1?c.duration:c.radius):0;return value>0?value:fallback;}
 bool Has(V06CardKind k)=>arena.v06Attack && arena.v06Attack.Level(k)>0;
 float Card(V06CardKind k)=>arena.v06Attack?arena.v06Attack.Value(k):0;
 public float Haste(int skillIndex)=>stats.Value(RunStat.Haste)+Card(V06CardKind.剑客之道)+(skillIndex==1?Card(V06CardKind.战士天赋)+(arena.v06Attack.build?arena.v06Attack.build.SkillHaste:0)+(Has(V06CardKind.亮剑)?100:0):Has(V06CardKind.亮剑)?60:0)+(Writing?writingHaste:0);
 public float SkillCritChance=>stats.Value(RunStat.CritChance)+Card(V06CardKind.剑客天赋);
 public float SkillCritDamage=>stats.Value(RunStat.CritDamage)+(arena.v06Attack?arena.v06Attack.Secondary(V06CardKind.剑客天赋):0);
 public void CardRemoved(V06CardKind kind,int remaining){if(kind==V06CardKind.浪客)swordChargeTimer=swordChargeDistance=0;if(kind==V06CardKind.剑息||kind==V06CardKind.月牙||kind==V06CardKind.涟漪)EnsureSwordQueue();if(remaining>0)return;if(kind==V06CardKind.写意&&Writing){EndWriting();cd[0]=0;CancelInput();}if(kind==V06CardKind.蓄势&&charging)CancelInput();if(kind==V06CardKind.珠帘){waterCharges=Mathf.Min(waterCharges,1);waterTimer=0;}}
 void StartWriting(){writingLeft=Card(V06CardKind.写意);writingSlashTimer=0;cd[1]=uiCooldownTotal[1]=0;}
 void EndWriting(){writingLeft=0;writingSlashTimer=0;Cooldown(0,sprintCD*1.5f);}
 void ResetWriting(){writingLeft=0;writingSlashTimer=0;ResetSwordPool();foreach(var s in swords)DestroySwordVisual(s);swords.Clear();}
 Vector2 NearestDirection(){arena.ReadTargets(swordTargets);float nearest=float.PositiveInfinity;Vector2 dir=arena.V06Facing;foreach(var t in swordTargets){float d=(t.position-arena.PlayerPosition).sqrMagnitude;if(d<nearest){nearest=d;dir=(t.position-arena.PlayerPosition).normalized;}}return dir;}
 void TickWriting(float dt){if(Writing){writingLeft-=dt;if(writingLeft<=0)EndWriting();else {if(slashLeft<=0)writingSlashTimer=Mathf.Max(0,writingSlashTimer-dt*Rate);if((!arena.v06Attack.build||arena.v06Attack.build.CanSlash)&&writingSlashTimer<=0&&slashLeft<=0&&dodgeLeft<=0){SlashInternal(false,sectorDamage*.5f,true);writingSlashTimer=SkillCooldownDuration(1,sectorCD);}}}
 float worldDt=dt*Rate;TickSwordPool(worldDt);
 for(int i=swords.Count-1;i>=0;i--){var s=swords[i];float old=s.distance;s.distance=Mathf.Min(Mathf.Max(.01f,swordRange),s.distance+worldDt*Mathf.Max(0,swordSpeed));arena.ReadTargets(swordTargets);foreach(var target in swordTargets){if(s.hit.Contains(target.id))continue;Vector2 delta=target.position-s.origin;bool hit=s.ring?delta.magnitude>=old-.5f&&delta.magnitude<=s.distance+.5f:Vector2.Distance(target.position,s.origin+s.direction*Mathf.Clamp(Vector2.Dot(delta,s.direction),old,s.distance))<EffectiveMoonHitWidth*.5f;if(!hit)continue;s.hit.Add(target.id);float amount=s.amount*(Random.value*100<s.critChance?s.critDamage/100:1);arena.V09FinalHit(target.id,amount,DamageSource.Skill);if(Has(V06CardKind.剑气))s.pushes.Add(new SwordPush{id=target.id,direction=s.ring?(target.position-s.origin).normalized:s.direction,lastSwordDistance=Mathf.Clamp(s.ring?delta.magnitude:Vector2.Dot(delta,s.direction),old,s.distance)});}
 PushWithSword(s);if(Has(V06CardKind.剑气))arena.V09ClearBullets(s.origin,s.direction,old,s.distance,s.ring,s.ring?.5f:EffectiveMoonHitWidth*.5f);if(s.visual)s.visual.Draw(s.origin,s.direction,s.distance,worldDt);if(s.line){int points=s.ring?64:2;s.line.positionCount=points;for(int n=0;n<points;n++){Vector2 pos=s.ring?s.origin+new Vector2(Mathf.Cos(n*Mathf.PI*2/(points-1)),Mathf.Sin(n*Mathf.PI*2/(points-1)))*s.distance:s.origin+s.direction*(s.distance-(n==0?.6f:0));s.line.SetPosition(n,pos);}}if(s.distance>=swordRange){DestroySwordVisual(s);swords.RemoveAt(i);}}
 }
 void DestroySwordVisual(Sword s){if(s.visual)Destroy(s.visual.gameObject);if(s.line){var material=s.line.sharedMaterial;Destroy(s.line.gameObject);if(material)Destroy(material);}}
 void Recover(ref int charges,ref float timer,int capacity,float baseCooldown,float dt){if(charges>=capacity){timer=0;return;}timer+=dt*(1+Haste(1)/100);if(timer>=baseCooldown){timer-=baseCooldown;charges++;}}
 void ReleaseSword(bool ring,Vector2 direction,float factor,bool? guaranteedHeart=null){if(!Has(ring?V06CardKind.涟漪:V06CardKind.月牙))return;if(!ConsumeSwordCharge(ring))return;
 bool heart=guaranteedHeart??(arena.v06Attack.build&&arena.v06Attack.build.ConsumeHeart());
 var obj=new GameObject(ring?"涟漪剑气":"月牙剑气");obj.transform.SetParent(transform);LineRenderer line=null;SwordEnergyVisual visual=null;
 var clip=animationSet?(ring?animationSet.ring:animationSet.crescent):null;
 if(clip!=null&&clip.Valid){visual=obj.AddComponent<SwordEnergyVisual>();visual.Initialize(animationSet,ring,heart?heartBlade:null);visual.Draw(arena.PlayerPosition,direction,0,0);}
 else{line=obj.AddComponent<LineRenderer>();line.material=new Material(Shader.Find("Sprites/Default"));line.startColor=line.endColor=heart?heartBlade.red:slashColor;line.widthMultiplier=.3f;}
 swords.Add(new Sword{origin=arena.PlayerPosition,direction=direction,ring=ring,amount=Card(ring?V06CardKind.涟漪:V06CardKind.月牙)*factor*stats.Value(RunStat.Attack)/100,critChance=heart?100:SkillCritChance,critDamage=SkillCritDamage+(heart?50:0),line=line,visual=visual});if(Has(V06CardKind.快攻势))cd[0]*=.5f;
 }
}
