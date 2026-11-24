using System.Collections.Generic;
using UnityEngine;
public sealed partial class CardLab
{
 public enum PlacementMode{关闭,放置木桩,移除木桩}
 public bool autoSpawnEnemies=false;
 [Min(.1f)]public float spawnInterval=1;
 public bool spawnTargetsOnReset=false;
 public Vector2 singleTargetOffset=new Vector2(3,0);
 public Vector2[] layoutOffsets=new Vector2[0];
 public PlacementMode placement;
 public CardLabLoadout[] presets=new CardLabLoadout[0];
 public CardLabLoadout selectedLoadout;
 [TextArea]public string loadoutResult;
 readonly List<Vector2> dummyPositions=new List<Vector2>();
 Rect PanelRect(float scale)=>panelCollapsed?new Rect(12,12,140,36):new Rect(12,12,Mathf.Min(370,Screen.width/scale-24),Mathf.Max(150,Mathf.Min(700,Screen.height/scale-24)));
 void TickLabTools(){arena.labAutoSpawn=autoSpawnEnemies;arena.labSpawnInterval=spawnInterval;if(placement==PlacementMode.关闭||!Input.GetMouseButtonDown(0))return;float scale=PanelScale;Vector2 mouse=new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y)/scale;if(PanelRect(scale).Contains(mouse))return;if(UnityEngine.EventSystems.EventSystem.current&&UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())return;Vector2 world=Camera.main.ScreenToWorldPoint(Input.mousePosition);if(placement==PlacementMode.放置木桩)arena.LabSpawn(world-arena.PlayerPosition,targetHP);else arena.LabRemoveDummy(world,1);}
 float PanelScale=>Mathf.Max(.7f,Mathf.Min(Screen.height/750f,Screen.width/1200f));
 [ContextMenu("测试/摆放单个木桩")]public void AddTarget(){if(Application.isPlaying)arena.LabSpawn(singleTargetOffset,targetHP);}
 [ContextMenu("测试/移除全部木桩")]public void ClearTargets(){if(Application.isPlaying)arena.LabClearDummies();}
 [ContextMenu("测试/应用自定义木桩布局")]public void ApplyLayout(){if(!Application.isPlaying)return;arena.LabClearDummies();foreach(var offset in layoutOffsets)arena.LabSpawn(offset,targetHP);}
 [ContextMenu("测试/应用选中卡牌组合")]
 public void ApplyLoadout(){if(!Application.isPlaying||!selectedLoadout)return;arena.LabReadDummyPositions(dummyPositions);arena.DebugRestart();arena.labMode=true;var attack=arena.v06Attack;var goals=new Dictionary<V06CardKind,int>();foreach(var entry in selectedLoadout.cards){var config=attack.Config(entry.card);if(config)goals[entry.card]=Mathf.Clamp(entry.level,0,config.maxPicks);}
 bool progress=true;while(progress){progress=false;foreach(var pair in goals){var config=attack.Config(pair.Key);if(attack.Level(pair.Key)<pair.Value&&attack.Available(config)){attack.Grant(config);progress=true;}}}
 loadoutResult="已应用："+selectedLoadout.name;foreach(var goal in goals)if(attack.Level(goal.Key)<goal.Value)loadoutResult+="\n前置不足："+goal.Key+" "+attack.Level(goal.Key)+"/"+goal.Value;
 foreach(var position in dummyPositions)arena.LabSpawn(position-arena.PlayerPosition,targetHP);}
}
