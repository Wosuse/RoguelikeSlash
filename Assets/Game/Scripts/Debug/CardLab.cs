using UnityEngine;
[DefaultExecutionOrder(500)]
public sealed partial class CardLab:MonoBehaviour
{
 public ArenaPrototype arena;
 public bool freezeEnemies=true;
 public bool invincible=true;
 public bool autoAttack=true;
 [Min(1)] public float targetHP=10000;
 [Range(1,20)] public int targetCount=5;
 public float targetDistance=3,targetGap=1.5f;
 public V06CardKind[] startingCards=new V06CardKind[0];
 public V06CardKind selectedCard;
 [Min(0)]public int desiredLevel=1;
 public bool showPanel=true;
 Vector2 scroll;string search="";Font font;
 void Start(){arena.labMode=true;var hud=FindObjectOfType<ArenaHud>();font=hud?hud.timeLabel.font:null;ResetLab();}
 void Update(){arena.labFreezeEnemies=freezeEnemies;arena.labInvincible=invincible;arena.v06Attack.suspendFiring=!autoAttack;TickLabTools();}
 [ContextMenu("测试/重置实验")]
 public void ResetLab(){if(!Application.isPlaying)return;arena.DebugRestart();arena.labMode=true;foreach(var kind in startingCards)Grant(kind);if(spawnTargetsOnReset)SpawnTargets();}
 public void Grant(V06CardKind kind){var card=arena.v06Attack.Config(kind);if(card)arena.v06Attack.Grant(card);}
 [ContextMenu("测试/获取选中卡一次")]public void GrantSelected(){if(Application.isPlaying)Grant(selectedCard);}
 [ContextMenu("测试/选中卡提升到指定等级")]
 public void SetSelected(){if(!Application.isPlaying)return;var attack=arena.v06Attack;var card=attack.Config(selectedCard);if(!card)return;int limit=Mathf.Min(desiredLevel,card.maxPicks);for(int n=attack.Level(selectedCard);n<limit;n++){if(!attack.Available(card))break;attack.Grant(card);}}
 [ContextMenu("测试/生成目标")]
 public void SpawnTargets(){if(!Application.isPlaying)return;arena.LabClearDummies();for(int i=0;i<targetCount;i++)arena.LabSpawn(new Vector2(targetDistance,(i-(targetCount-1)*.5f)*targetGap),targetHP);}
 [ContextMenu("测试/充满无双")]public void Charge(){if(Application.isPlaying)arena.v06Skills.DebugCharge();}
 [ContextMenu("测试/玩家扣除一半最大生命")]
 public void HalfHealth(){if(Application.isPlaying)arena.LabDamagePlayer(arena.v06Attack.stats.Value(RunStat.MaxHP)*.5f);}
 [ContextMenu("测试/玩家回满生命")]
 public void Heal(){if(Application.isPlaying)arena.v06Attack.stats.Heal(999999);}
}
