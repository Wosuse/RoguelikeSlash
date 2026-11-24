using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public sealed partial class V06Skills : MonoBehaviour
{
 public ArenaPrototype arena; public RunClock clock; public RunStats stats;
 [Header("输入")] public float holdThreshold=.3f;
 [Header("闪")] [InspectorName("短按闪位移距离")] public float dodgeDistance=1;
 public float dodgeDuration=.18f,dodgeCD=2.5f,sprintDuration=3,sprintSpeed=1.6f,sprintCD=8;
 [Header("斩")] [InspectorName("短按斩扇形角度")] public float sectorAngle=60;
 [InspectorName("短按斩基础半径")] public float sectorRadius=3.5f;
 [InspectorName("长按斩基础半径")] public float ringRadius=3;
 public float sectorDamage=100,sectorCD=3,ringDamage=150,ringCD=8,slashDuration=.25f;
 [Header("无双：试玩参数")] public float energyRequired=100;
 public float energyPerHit=2;
 public float ultimateDuration=6;
 public float worldRate=.3f;
 public float comboInterval=.3f;
 public float comboDamage=100;
 [Header("短按斩：蓝白月牙刀光")] public MoonSlashFlashSettings tapBlade = new MoonSlashFlashSettings();
 [Header("环形斩：蓝白刀光环")] public RingSlashFlashSettings ringBlade=new RingSlashFlashSettings();
 [Header("剑心必暴：黑红刀光与剑气")] public HeartBladeSettings heartBlade=new HeartBladeSettings();
 public Color slashColor=Color.cyan;public Material slashMaterial;public AudioClip slashSound,dodgeSound,ultimateSound;
 [SerializeField] float energy,ultimateLeft,sprintLeft,dodgeLeft,slashLeft;
 readonly float[] cd=new float[2];readonly float[] uiCooldownTotal=new float[2];int pressed=-1,pointer,comboStep;float age,comboTimer;bool held,backwardDodge,slashAllowsMovement;Vector2 dashAim;
 AudioSource audioSource;
 public bool Busy=>pressed==1||slashLeft>0||dodgeLeft>0||Ultimate;
 public bool PreserveDodgeFacing=>backwardDodge&&(dodgeLeft>0||!arena.HasMovementInput);
 public bool Invulnerable=>dodgeLeft>0;
 public bool Ultimate=>ultimateLeft>0;
 public float Rate=>Ultimate?worldRate:1;
 public Vector2 Velocity(Vector2 normal)=>charging?Vector2.zero:dodgeLeft>0?dashAim*dodgeDistance/Mathf.Max(.01f,dodgeDuration):normal*(slashLeft>0&&!slashAllowsMovement?0:sprintLeft>0?sprintSpeed:1)*(Writing?Mathf.Max(0,1+writingMoveSpeed/100):1);
 public void DebugClearCooldowns(){System.Array.Clear(cd,0,cd.Length);System.Array.Clear(uiCooldownTotal,0,uiCooldownTotal.Length);}
 void Start(){audioSource=gameObject.AddComponent<AudioSource>();}
 [ContextMenu("调试/充满无双")] public void DebugCharge()=>energy=energyRequired;
 public void OnApplicationFocus(bool focus){if(!focus)CancelInput();}
 public void Charge()=>energy=Mathf.Min(energyRequired,energy+energyPerHit);
 public void ResetRun(){foreach(var effect in GetComponentsInChildren<RingSlashFlash>()){effect.gameObject.SetActive(false);Destroy(effect.gameObject);}foreach(var effect in GetComponentsInChildren<MoonSlashFlash>()){effect.gameObject.SetActive(false);Destroy(effect.gameObject);}backwardDodge=false;slashAllowsMovement=false;ResetAdvanced();ResetWriting();energy=ultimateLeft=sprintLeft=dodgeLeft=slashLeft=0;pressed=-1;cd[0]=cd[1]=0;}
 public void CancelInput(){pressed=-1;charging=false;}
 void Update(){if(!arena.Alive||clock.Paused){CancelInput();return;}float dt=Time.deltaTime;TickAdvanced(dt);TickWriting(dt);
 ultimateLeft=Mathf.Max(0,ultimateLeft-dt);sprintLeft=Mathf.Max(0,sprintLeft-dt);dodgeLeft=Mathf.Max(0,dodgeLeft-dt);slashLeft=Mathf.Max(0,slashLeft-dt);
 for(int i=0;i<2;i++)if(!(i==0&&(sprintLeft>0||dodgeLeft>0)||i==1&&slashLeft>0))cd[i]=Mathf.Max(0,cd[i]-dt*Rate);
 if(Input.GetKeyDown(KeyCode.Space))Down(0,-2);if(Input.GetKeyUp(KeyCode.Space))Up(0,-2);
 if(Input.GetKeyDown(KeyCode.J))Down(1,-3);if(Input.GetKeyUp(KeyCode.J))Up(1,-3);
 if(Input.GetKeyDown(KeyCode.K))Down(2,-4);if(Input.GetKeyUp(KeyCode.K))Up(2,-4);
 if(pressed>=0){age+=dt;if(pressed==2&&Ultimate){comboTimer-=dt;if(comboTimer<=0){Combo();comboTimer=comboInterval;}}
 else if(!held&&age>=holdThreshold){held=true;if(pressed==0){if(Has(V06CardKind.写意))StartWriting();else {sprintLeft=sprintDuration;Cooldown(0,sprintCD);}Sound(dodgeSound);}else if(pressed==1){if(Has(V06CardKind.蓄势)){charging=true;chargeTime=0;}else {Slash(true,ringDamage);Cooldown(1,ringCD);}}}}
 }
 public void Down(int i,int id){if(clock.Paused||!arena.Alive||ButtonDisabled(i))return;if(i<2&&Writing){if(i==0)TryAfterimage();EndWriting();CancelInput();if(i==1){Slash(false,sectorDamage);Cooldown(1,sectorCD);}return;}if(i<2&&arena.v06Attack.build&&!(i==0?arena.v06Attack.build.CanDodge:arena.v06Attack.build.CanSlash))return;if(pressed>=0)return;if(i<2&&(Ultimate||cd[i]>0||slashLeft>0||dodgeLeft>0))return;
 if(i==2&&!Ultimate){if(energy<energyRequired)return;energy=0;ultimateLeft=ultimateDuration;comboStep=0;Sound(ultimateSound);}if(i==0)TryAfterimage();pressed=i;pointer=id;age=0;held=false;comboTimer=comboInterval;if(i==2)Combo();}
 public void Up(int i,int id){if(pressed!=i||pointer!=id)return;if(charging&&i==1){FinishCharge();pressed=-1;return;}if(!held&&i<2){if(i==0){backwardDodge=!arena.HasMovementInput;dashAim=arena.DodgeDirection;dodgeLeft=dodgeDuration;Cooldown(0,dodgeCD);Sound(dodgeSound);}else{Slash(false,sectorDamage);Cooldown(1,sectorCD);}}pressed=-1;}
 void Combo(){int step=comboStep++%3;if(step==1){backwardDodge=false;dashAim=arena.V06Facing;dodgeLeft=dodgeDuration;}else Slash(step==2,comboDamage);}
 float SkillCooldownDuration(int i,float seconds)=>seconds*(Has(V06CardKind.纯粹射手)?1.5f:1)/(1+Haste(i)/100);
 void Cooldown(int i,float seconds)=>uiCooldownTotal[i]=cd[i]=SkillCooldownDuration(i,seconds);
 void Sound(AudioClip clip){if(clip&&audioSource)audioSource.PlayOneShot(clip);}
 void Slash(bool ring,float damage){SlashInternal(ring,damage,false);}
 void SlashInternal(bool ring,float damage,bool allowMovement){slashIsRing=ring;slashAllowsMovement=allowMovement;slashLeft=slashDuration;var direction=Writing?NearestDirection():arena.V06Facing;animationFacing=direction;float radius=ring?ringRadius:sectorRadius;float multiplier=1+(Card(V06CardKind.力劈)+Card(V06CardKind.亮剑))/100;bool heart=arena.v06Attack.build&&arena.v06Attack.build.ConsumeHeart();arena.V09Slash(radius,ring?360:sectorAngle,damage*multiplier,direction,heart);TriggerSkillExtras(ring,direction);ReleaseSword(ring,direction,Writing?.5f:1,heart);if(!ring&&tapBlade.enabled){var obj=new GameObject("短斩月牙刀光");obj.transform.SetParent(transform,false);obj.AddComponent<MoonSlashFlash>().Initialize(arena,animationSet?animationSet.crescent:null,tapBlade,radius,direction,heart?heartBlade:null);}if(ring){var obj=new GameObject("环斩蓝白刀光");obj.transform.SetParent(transform,false);obj.AddComponent<RingSlashFlash>().Initialize(arena,ringBlade,radius,direction,heart?heartBlade:null);}Sound(slashSound);}
}
