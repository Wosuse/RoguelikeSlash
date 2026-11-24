using UnityEngine;
public sealed partial class ArenaPrototype
{
 [SerializeField, InspectorName("战斗界面")] ArenaHud hud;
 float UiScale => hud ? hud.Scale : 1;
 void UpdateHud()
 {
  if(!hud)return;
  int seconds=Mathf.FloorToInt(elapsed);string time=$"{seconds/60:00}:{seconds%60:00}";
  hud.SetScore(time,kills);hud.SetHealth(runStats?runStats.HP:hp,runStats?runStats.Value(RunStat.MaxHP):100);
  hud.SetJoystick(hp>0,finger>=0,touchOrigin,stick);hud.SetResults(hp<=0,time,kills);hud.retry.interactable=hp<=0;
  int count=0;if(hp>0)foreach(var enemy in enemies)if(IsMeleeWarning(enemy))hud.SetWarning(count++,view.WorldToScreenPoint((Vector3)enemy.Position+Vector3.up*.74f));
  hud.HideWarningsFrom(count);
 }
}
