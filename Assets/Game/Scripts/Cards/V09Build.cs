using UnityEngine;
// Run-wide derived attributes and timed passives. Never writes card bonuses into shard counts.
public sealed class V09Build : MonoBehaviour
{
 public V06Attack attack;public RunStats stats;public V06Skills skills;
 [Header("待定规则：试玩配置")]
 public float iceDuration=2;
 public float afterimageRadius=3;
 [Header("踢踏舞：仅普攻命中叠层，受伤或脱战清空")]
 [Min(0),InspectorName("每层移动速度加成（%）")] public float danceMovePercentPerStack=1;
 [Min(0),InspectorName("每层攻击速度加成（%）")] public float danceAttackPercentPerStack=1;
 [Min(1),InspectorName("叠层上限")] public int danceStackLimit=100;
 [Min(.1f),InspectorName("未命中清层时间（秒）")] public float danceMissTimeout=5;
 [SerializeField] int danceStacks;
 [SerializeField] float danceIdleTime;
 public float DanceTimeRemaining=>DanceStacks>0?Mathf.Max(0,danceMissTimeout-danceIdleTime):0;
 [SerializeField] float heartTimer,undyingLeft;
 [SerializeField,InspectorName("会心已攻击轮数")] int focusRounds;
 [SerializeField] bool heartReady,focusReady,undyingUsed;
 Vector2 lastPosition;
 public bool Has(V06CardKind k)=>attack&&attack.Level(k)>0;
 public float Value(V06CardKind k)=>attack?attack.Value(k):0;
 public float Missing=>100*Mathf.Clamp01((stats.Value(RunStat.MaxHP)-stats.HP)/Mathf.Max(1,stats.Value(RunStat.MaxHP)));
 public float AttackBonus=>Has(V06CardKind.背水)?Missing*Value(V06CardKind.背水):0;
 public float CritBonus=>Has(V06CardKind.背水)?Missing*attack.Secondary(V06CardKind.背水):0;
 public float SkillHaste=>Has(V06CardKind.临危亢奋)?Missing*Value(V06CardKind.临危亢奋):0;
 public float CrisisLifeSteal=>Has(V06CardKind.临危亢奋)?Missing*attack.Secondary(V06CardKind.临危亢奋):0;
 public int DanceStacks=>Has(V06CardKind.踢踏舞)?Mathf.Clamp(danceStacks,0,Mathf.Max(1,danceStackLimit)):0;
 public float DanceAttackSpeedBonus=>DanceStacks*danceAttackPercentPerStack;
 public float MoveFactor=>1+DanceStacks*danceMovePercentPerStack/100;
 public float AutoBonus=>Value(V06CardKind.自信走位)+Value(V06CardKind.纯粹射手)+Value(V06CardKind.烈射手)+Value(V06CardKind.狂射手);
 public bool CanSlash=>!Has(V06CardKind.自信走位)&&!Has(V06CardKind.烈射手);
 public bool CanDodge=>!Has(V06CardKind.自信走位)&&!Has(V06CardKind.狂射手);
 public bool Immune=>undyingLeft>0;
 public bool HeartReady=>Has(V06CardKind.剑心)&&heartReady;
 public float AttackPower=>Has(V06CardKind.嗡嗡攻势)?stats.attack:stats.Value(RunStat.Attack);
 public float AttackSpeedBonus=>DanceAttackSpeedBonus+(Has(V06CardKind.嗡嗡攻势)?Mathf.Max(0,stats.RawAttack-stats.attack)*Value(V06CardKind.嗡嗡攻势):0);
 public void CardGranted(V06CardKind kind){if(attack.Level(kind)!=1)return;if(kind==V06CardKind.剑心){heartReady=true;heartTimer=0;}if(kind==V06CardKind.会心){focusReady=true;focusRounds=0;}}
 public void CardRemoved(V06CardKind kind,int remaining){if(remaining>0)return;switch(kind){case V06CardKind.踢踏舞:ClearDance();break;case V06CardKind.会心:focusReady=false;focusRounds=0;break;case V06CardKind.剑心:heartReady=false;heartTimer=0;break;case V06CardKind.不屈:undyingLeft=0;undyingUsed=false;break;}}
 public void ResetRun(){ClearDance();heartTimer=undyingLeft=0;focusRounds=0;heartReady=focusReady=undyingUsed=false;lastPosition=attack.arena.PlayerPosition;}
 public void Tick(float dt){if(DanceStacks>0){danceIdleTime+=Mathf.Max(0,dt);if(danceIdleTime>=Mathf.Max(.1f,danceMissTimeout))ClearDance();}undyingLeft=Mathf.Max(0,undyingLeft-dt);if(Has(V06CardKind.剑心)&&!heartReady){heartTimer+=dt*(1+skills.Haste(1)/100);if(heartTimer>=Value(V06CardKind.剑心)){heartReady=true;heartTimer=0;}}
 var position=attack.arena.PlayerPosition;if(Has(V06CardKind.浪客))skills.RecoverByDistance(Vector2.Distance(position,lastPosition));lastPosition=position;}
 public bool BeginFocusRound(){if(!Has(V06CardKind.会心))return false;bool critical=focusReady;focusReady=false;focusRounds++;if(focusRounds>=Mathf.Max(1,Mathf.RoundToInt(Value(V06CardKind.会心)))){focusRounds=0;focusReady=true;}return critical;}
 public bool ConsumeHeart(){if(!heartReady)return false;heartReady=false;return true;}
 // Every normal hit refreshes timeout, but only the first hit of a round grants a stack.
 public void Dance(bool firstInRound=true){if(!Has(V06CardKind.踢踏舞))return;danceIdleTime=0;if(firstInRound)danceStacks=Mathf.Min(DanceStacks+1,Mathf.Max(1,danceStackLimit));}
 void ClearDance(){danceStacks=0;danceIdleTime=0;}
 public void OnHealthDamaged(){ClearDance();}
 public void SkillHeal(float actual){float percent=(Has(V06CardKind.血狂)?Value(V06CardKind.血狂):0)+CrisisLifeSteal;if(Has(V06CardKind.血袭)&&Random.value*100<Value(V06CardKind.血袭))percent+=attack.Secondary(V06CardKind.血袭);stats.Heal(actual*percent/100);}
 public bool SaveFromDeath(){if(!Has(V06CardKind.不屈)||undyingUsed)return false;undyingUsed=true;undyingLeft=Value(V06CardKind.不屈);return true;}
}
